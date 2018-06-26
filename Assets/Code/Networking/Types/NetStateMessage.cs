using System.IO;
using ProtoBuf;
using UnityEngine;

namespace Assets.Code.Network.Types
{
    [ProtoContract]
    public struct NetStateMessage
    {
        [ProtoMember(1)]
        public Vector2 Position;

        [ProtoMember(2)] public int LastTick;

        [ProtoMember(3)] public int PhotonPlayerId;
    }
}