using System.IO;
using Assets.Code.Network.Types;
using ProtoBuf;

namespace Assets.Code.Networking.Serializers
{
    class SerializerBase
    {
        // Byte deserializer
        public static object Deserialize<T>(byte[] data)
        {
            using (var stream = new MemoryStream(data))
            {
                return Serializer.Deserialize<T>(stream);
            }
        }

        // Stream Serializer
        public static byte[] Serialize(object customobject)
        {
            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, customobject);
                return stream.ToArray();
            }

            return null;
        }
    }
}