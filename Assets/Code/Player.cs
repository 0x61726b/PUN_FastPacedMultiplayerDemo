using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.Code.Network.Types;
using Assets.Code.Networking.Utils;
using Photon;
using UnityEngine;
using Random = UnityEngine.Random;

public class Player : PunBehaviour
{
    // Input Buffer to use in reconcialiton
    private List<Inputs> _inputBuffer;
    private List<Inputs> _localInputs;

    private float _simulationTime;

    // Local Tick Number
    private int _tickNumber;

    // Simulation Tick Number
    private int _simulationTickNumber;

    // Position Buffer for this player object (could be server/client(s))
    public NetworkCircularBuffer PositionBuffer;

    // Time Syncer
    public TimeSyncer Syncer;


    // Options
    public bool ClientPacketLossSimulation; // Simulate packet loss
    public float PacketLossChance; // Packet loss chance in percentage
    public bool Conciliation; // Whether to apply conciliation or not
    public bool Interpolation; // Whether to apply interpolation or not
    public int ClientSendRate; // Send Rate of the client (How many inputs will be sent in a second)
    //

    private BoxCollider2D _collider;
    private Rigidbody2D _rb;

    // Server Related
    private Queue<NetInputMessage> _inputMessageQueue; // Input Queue

    private List<ServerPlayer> _remotePlayers;


    void Awake()
    {
        _collider = GetComponent<BoxCollider2D>();
        _rb = GetComponent<Rigidbody2D>();

        GetComponent<SpriteRenderer>().color = Random.ColorHSV();
    }

    // Use this for initialization
    void Start()
    {
        _inputBuffer = new List<Inputs>();
        _localInputs = new List<Inputs>();
        _inputMessageQueue = new Queue<NetInputMessage>();
        _simulationTime = Time.time;

        PositionBuffer = new NetworkCircularBuffer();
        Syncer = new TimeSyncer();

        if (photonView.isMine)
        {
            PhotonNetwork.OnEventCall += OnEventRaised;
        }

        if (PhotonNetwork.isMasterClient)
            _remotePlayers = new List<ServerPlayer>();

        if (photonView.isMine)
        {
            Camera.main.GetComponent<FollowCamera>().Target = transform;
        }
    }

    void FixedUpdate()
    {
        // Server
        Server_InputLoop();
    }

    // Update is called once per frame
    void Update()
    {
        // Entity Interpolation
        Client_EntityInterpolation();

        // Process Inputs in current client tick
        Client_InputLoop();

        // Send inputs to server on an interval
        Client_SendInputs();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Collisions are processed only on the server
        // Then clients are made aware by RPCs
        if (!PhotonNetwork.isMasterClient)
            return;

        if (collision.collider.CompareTag("NetworkPlayer"))
        {
            Debug.LogFormat("Collision detected on server: {0} and {1}", gameObject.name, collision.collider.name);
        }
    }

    public void OnEventRaised(byte eventCode, object content, int senderID)
    {
        bool isMaster = PhotonNetwork.isMasterClient;
        bool isMine = photonView.isMine;

        switch (eventCode)
        {
            case (byte)EventCodes.CTOS_ClientInput:
                if (!isMaster)
                    break;

                var msg = (NetInputMessage)content;
                msg.PhotonPlayerId = senderID;
                msg.Time = Time.time;

                Server_ReceiveInput(msg, senderID);
                break;

            case (byte)EventCodes.STOC_ClientInputAcknowledge:
                var stateMsg = (NetStateMessage)content;

                var photonPlayer = PhotonUtils.GetPlayerById(stateMsg.PhotonPlayerId);
                if (photonPlayer == null)
                    break;

                var photonPlayerTransform = PhotonUtils.GetPhotonPlayerTransform(photonPlayer);
                if (photonPlayerTransform == null)
                    break;

                bool isMyAck = PhotonNetwork.player.ID == stateMsg.PhotonPlayerId;

                if (isMyAck)
                {
                    photonPlayerTransform.position = stateMsg.Position;

                    // Server Reconciliation
                    if (Conciliation)
                    {
                        int j = 0;
                        while (j < _inputBuffer.Count)
                        {
                            var input = _inputBuffer[j];

                            if (input.TickNr <= stateMsg.LastTick)
                            {
                                _inputBuffer.Remove(input);
                            }
                            else
                            {
                                ApplyInput(new List<Inputs> { input }, transform);
                                j++;
                            }
                        }
                    }

                    PositionBuffer.Push(stateMsg.Position.x, stateMsg.Position.y, Syncer.ServerDelta(Time.deltaTime));
                }
                else
                {
                    // Entity Interpolation
                    if (Interpolation)
                    {
                        Player p = photonPlayerTransform.GetComponent<Player>();

                        p.Client_SyncServer();
                        p.PositionBuffer.Push(stateMsg.Position.x, stateMsg.Position.y, p.Syncer.ServerDelta(Time.deltaTime));
                    }
                    else
                    {
                        photonPlayerTransform.position = stateMsg.Position;
                    }
                }
                break;
            default:
                Debug.LogErrorFormat("Unknown event code {0} from {1}", eventCode, senderID);
                break;
        }
    }

    #region Input & Interpolation & Reconciliation

    private void Server_InputLoop()
    {
        if (!PhotonNetwork.isMasterClient || !photonView.isMine) return;

        while (_inputMessageQueue.Count > 0)
        {
            ProcessInputMessages();
        }
    }

    private void Client_SendInputs()
    {
        // Simulate client send rate
        while (Time.time - _simulationTime > (1.0f / ClientSendRate) / 1000.0f)
        {
            _simulationTime += (1.0f / ClientSendRate) / 1000.0f;
            _simulationTickNumber++;

            if (_localInputs.Count == 0)
                return;

            // Check for queued inputs and send them to the server
            Client_SendInput(_localInputs);
            _localInputs.Clear();
        }
    }

    private void Client_InputLoop()
    {
        if (!photonView.isMine) return;

        Inputs input;
        input.Up = Input.GetKey(KeyCode.W);
        input.Down = Input.GetKey(KeyCode.S);
        input.Left = Input.GetKey(KeyCode.A);
        input.Right = Input.GetKey(KeyCode.D);
        input.TickNr = _tickNumber;
        input.JoystickDir = LeftJoystick.Instance.GetInputDirection();

        // Store Input
        _inputBuffer.Add(input);
        _localInputs.Add(input);

        ApplyInput(new List<Inputs>() { input }, transform);

        // Increment tick number
        _tickNumber++;
    }

    private void Client_EntityInterpolation()
    {
        if (!Interpolation || photonView.isMine) return;

        while (Syncer.Move())
        {
            PointAtTime interpPoint = PositionBuffer.Interpolate(Syncer.TimeSinceStart());
            transform.position = new Vector2(interpPoint.x, interpPoint.y);
        }
    }

    public void ProcessInputMessages()
    {
        // Get the message
        NetInputMessage msg = _inputMessageQueue.Dequeue();

        // Find out who sent it
        PhotonPlayer photonPlayer = PhotonUtils.GetPlayerById(msg.PhotonPlayerId);
        if (photonPlayer == null)
            return;

        // Get remote player instance
        var remotePlayer = _remotePlayers.Find(x => x.Player.ID == photonPlayer.ID);
        if (remotePlayer == null)
        {
            Transform target = null;
            foreach (var go in GameObject.FindGameObjectsWithTag("NetworkPlayer"))
            {
                if (go.GetComponent<PhotonView>().ownerId == photonPlayer.ID)
                {
                    target = go.transform;
                }
            }

            if (target == null)
                Debug.LogErrorFormat("Could not find Transform component for PhotonPlayer {0}", photonPlayer.ID);
            else
            {
                remotePlayer = new ServerPlayer() { Player = photonPlayer, Target = target };
                _remotePlayers.Add(remotePlayer);
            }
        }

        if (remotePlayer == null)
            return;

        Player player = remotePlayer.Target.GetComponent<Player>();
        player.Client_SyncServer();

        Server_ApplyInput(msg.Inputs, remotePlayer);

        player.PositionBuffer.Push(remotePlayer.Position.x, remotePlayer.Position.y, player.Syncer.ServerDelta(Time.deltaTime));

        NetStateMessage stateMsg = new NetStateMessage
        {
            Position = remotePlayer.Position,
            LastTick = msg.Identifier,
            PhotonPlayerId = remotePlayer.Player.ID
        };

        var options = new RaiseEventOptions {TargetActors = PhotonUtils.GetPlayerIdsInRoom()};


        // Broadcast new state of the client
        PhotonNetwork.RaiseEvent((byte)EventCodes.STOC_ClientInputAcknowledge, stateMsg, false, options);
    }

    void Server_ReceiveInput(NetInputMessage input, int clientId)
    {
        _inputMessageQueue.Enqueue(input);
    }

    void Client_SendInput(List<Inputs> inputs)
    {
        NetInputMessage msg = new NetInputMessage
        {
            Identifier = _tickNumber,
            Inputs = inputs
        };

        // If the current client is Master, send input to whole room
        // else send it to only Master (to be distributed later)
        var options = new RaiseEventOptions
        {
            TargetActors = PhotonNetwork.isMasterClient
                ? PhotonUtils.GetPlayerIdsInRoom()
                : new[] {PhotonNetwork.room.MasterClientId}
        };


        // If inputs are coming from the server (Master Server), they will be absolute.
        if (PhotonNetwork.isMasterClient)
        {
            var stateMsg = new NetStateMessage
            {
                Position = transform.position,
                LastTick = _tickNumber,
                PhotonPlayerId = PhotonNetwork.player.ID
            };

            PhotonNetwork.RaiseEvent((byte)EventCodes.STOC_ClientInputAcknowledge, stateMsg, false, options);
        }
        else
        {
            PhotonNetwork.RaiseEvent((byte)EventCodes.CTOS_ClientInput, msg, false, options);
        }
    }

    private void ApplyInput(List<Inputs> inputs, Transform target)
    {
        foreach (var input in inputs)
        {
            const float factor = 0.1f;
            if (input.JoystickDir == Vector2.zero)
            {
                if (input.Down)
                    target.Translate(0, -factor, 0);
                if (input.Up)
                    target.Translate(0, factor, 0);
                if (input.Left)
                    target.Translate(-factor, 0, 0);
                if (input.Right)
                    target.Translate(factor, 0, 0);
            }
            else
            {
                Vector2 v = input.JoystickDir * 5 * (1 / 60.0f);
                target.transform.Translate(v);
            }
        }
    }

    void Server_ApplyInput(List<Inputs> inputs, ServerPlayer player)
    {
        foreach (var input in inputs)
        {
            const float factor = 0.1f;
            if (input.JoystickDir == Vector2.zero)
            {
                if (input.Down)
                    player.Position = Server_Translate(Vector2.down * factor, player.Position);
                if (input.Up)
                    player.Position = Server_Translate(Vector2.up * factor, player.Position);
                if (input.Left)
                    player.Position = Server_Translate(Vector2.left * factor, player.Position);
                if (input.Right)
                    player.Position = Server_Translate(Vector2.right * factor, player.Position);
            }
            else
            {
                Vector2 v = input.JoystickDir * 5 * (1 / 60.0f);
                player.Position = Server_Translate(v, player.Position);
            }
        }
    }

    private Vector2 Server_Translate(Vector2 by, Vector2 to)
    {
        return to += by;
    }

    public void Client_SyncServer()
    {
        Syncer.OnServerUpdate();
    }
    #endregion
}
