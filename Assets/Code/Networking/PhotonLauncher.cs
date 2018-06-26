using Assets.Code.Network.Types;
using Assets.Code.Networking.Serializers;
using ExitGames.Client.Photon;
using Photon;
using ProtoBuf.Meta;
using UnityEngine;

namespace Assets.Code.Networking
{
    public class PhotonLauncher : PunBehaviour
    {
        public bool OfflineMode;
        private bool _isConnecting;
        private string _roomName = "MyRoom";


        void Awake()
        {
            PhotonNetwork.autoJoinLobby = false;
            PhotonNetwork.automaticallySyncScene = true;
            PhotonNetwork.offlineMode = OfflineMode;
            PhotonNetwork.sendRate = 20;

            QualitySettings.vSyncCount = 0; // Turn off vsync
            Application.targetFrameRate = 60; // Set the frame rate to 60 to match TimeSyncer's interpolation frame rate


            // Register custom types
            PhotonPeer.RegisterType(typeof(NetInputMessage), 1, SerializerBase.Serialize, SerializerBase.Deserialize<NetInputMessage>);
            PhotonPeer.RegisterType(typeof(NetStateMessage), 2, SerializerBase.Serialize, SerializerBase.Deserialize<NetStateMessage>);

            // Register built-in types
            RuntimeTypeModel.Default.Add(typeof(Vector2), false).Add("x").Add("y");
            RuntimeTypeModel.Default.Add(typeof(Vector3), true).Add("x").Add("y").Add("z"); // Not used in this demo
            RuntimeTypeModel.Default.Add(typeof(Quaternion), true).Add("x").Add("y").Add("z").Add("w"); // Not used in this demo

            Connect();
        }

        void Start()
        {
            
        }

        public void Connect()
        {
            if (PhotonNetwork.offlineMode)
            {
                PhotonNetwork.CreateRoom(_roomName);
            }
            else
            {
                _isConnecting = true;
                PhotonNetwork.AuthValues = new AuthenticationValues
                {
                    UserId = Application.isEditor ? "EditorUser" : "StandaloneUser" + UnityEngine.Random.Range(0, 10000)
                };
                PhotonNetwork.ConnectUsingSettings("1.0");
            }
        }

        public override void OnConnectedToMaster()
        {
            Debug.Log("Region:" + PhotonNetwork.networkingPeer.CloudRegion);

            if (_isConnecting)
            {
                PhotonNetwork.CreateRoom(_roomName, new RoomOptions() { MaxPlayers = 10 }, null);
            }
        }

        public override void OnPhotonRandomJoinFailed(object[] codeAndMsg)
        {
            Debug.Log("Photon Random Join Failed");
        }

        public override void OnPhotonCreateRoomFailed(object[] codeAndMsg)
        {
            PhotonNetwork.JoinRoom(_roomName);
        }

        public override void OnDisconnectedFromPhoton()
        {
            Debug.Log("Disconnected From Photon");
        }

        public override void OnJoinedRoom()
        {
            Debug.Log("On Joined Room");

            if (PhotonNetwork.room.PlayerCount == 1)
            {
                Debug.Log("Master Client");

                PhotonNetwork.Instantiate("Prefabs/Player", new Vector3(0, 0, 1),Quaternion.identity, 0);
            }
            else
            {
                PhotonNetwork.Instantiate("Prefabs/Player", new Vector3(0, 0, 1), Quaternion.identity, 0);
            }
        }

        public override void OnPhotonPlayerConnected(PhotonPlayer newPlayer)
        {
            Debug.Log("New player connected");
        }

        void OnGUI()
        {
            // Draw information on top left
            int y = (int)(25.0f * (float)(Screen.width) / 1920.0f);
            int h = y + 2;
            int lineWidth = 400;
            DrawInfoText(new Rect(0, 0, lineWidth, h), "Game State: ");
            DrawInfoText(new Rect(0, y, lineWidth, h), "Is Offline mode? " + PhotonNetwork.offlineMode);
            DrawInfoText(new Rect(0, 2 * y, lineWidth, h), "Is Master? " + PhotonNetwork.isMasterClient);
            DrawInfoText(new Rect(0, 3 * y, lineWidth, h), "Connection: " + PhotonNetwork.connectionState);
            DrawInfoText(new Rect(0, 4 * y, lineWidth, h), "In Room? " + PhotonNetwork.inRoom);
            DrawInfoText(new Rect(0, 5 * y, lineWidth, h), "Send Rate: " + PhotonNetwork.sendRate);
            DrawInfoText(new Rect(0, 6 * y, lineWidth, h), "Cloud Region: " + PhotonNetwork.CloudRegion);
            DrawInfoText(new Rect(0, 7 * y, lineWidth, h),
                "Number of Players in room: " + PhotonNetwork.countOfPlayersInRooms);
            DrawInfoText(new Rect(0, 8 * y, lineWidth, h), "Server Address: " + PhotonNetwork.ServerAddress);
            DrawInfoText(new Rect(0, 9 * y, lineWidth, h),
                "Local User ID: " + PhotonNetwork.player.ID + " (" + PhotonNetwork.player.UserId + ")");
            DrawInfoText(new Rect(0, 10 * y, lineWidth, h), "Packet Loss: " + PhotonNetwork.PacketLossByCrcCheck);
            DrawInfoText(new Rect(0, 11 * y, lineWidth, h), "Ping: " + PhotonNetwork.GetPing());
            DrawInfoText(new Rect(0, 12 * y, lineWidth, h), "FPS: " + Mathf.CeilToInt(1 / Time.smoothDeltaTime));
        }

        private void DrawInfoText(Rect rect, string text)
        {
            GUIStyle infoTextStyle = new GUIStyle
            {
                fontSize = (int) (25.0f * (float) (Screen.width) / 1920.0f),
                normal = {textColor = Color.black}
            };
            GUI.Label(rect, text, infoTextStyle);
        }
    }
}