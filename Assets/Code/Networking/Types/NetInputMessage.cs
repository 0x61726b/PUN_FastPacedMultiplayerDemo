using System.Collections.Generic;
using System.IO;
using ProtoBuf;
using UnityEngine;

namespace Assets.Code.Network.Types
{

    [ProtoContract]
    public struct Inputs
    {
        [ProtoMember(1)]
        public bool Up;

        [ProtoMember(2)]
        public bool Down;

        [ProtoMember(3)]
        public bool Left;

        [ProtoMember(4)]
        public bool Right;

        [ProtoMember(5)] public int TickNr;

        [ProtoMember(6)] public Vector2 JoystickDir;
    }

    [ProtoContract]
    public struct NetInputMessage
    {
        [ProtoMember(1)]
        public int Identifier;

        [ProtoMember(2)]
        public List<Inputs> Inputs;

        public int PhotonPlayerId;
        public float Time;
    }
}