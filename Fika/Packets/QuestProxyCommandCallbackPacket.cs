

using Fika.Core.Networking.LiteNetLib.Utils;

namespace Astar.Vanguard.Fika.Packets
{
    public class QuestProxyCommandCallbackPacket : BasePacket
    {
        public string TargetId;

        public override void Deserialize(NetDataReader reader)
        {
            base.Deserialize(reader);
            TargetId = reader.GetString();
        }

        public override void Serialize(NetDataWriter writer)
        {
            base.Serialize(writer);
            writer.Put(TargetId, 0);
        }
    }
}