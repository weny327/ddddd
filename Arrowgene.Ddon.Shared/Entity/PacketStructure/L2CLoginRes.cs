using Arrowgene.Buffers;
using Arrowgene.Ddon.Shared.Network;

namespace Arrowgene.Ddon.Shared.Entity.PacketStructure
{
    public class L2CLoginRes : ServerResponse
    {
        public string LoginToken { get; set; }

        public override PacketId Id => PacketId.L2C_LOGIN_RES;
    }

    public class L2CLoginResSerializer : EntitySerializer<L2CLoginRes>
    {
        public override void Write(IBuffer buffer, L2CLoginRes obj)
        {
            WriteServerResponse(buffer, obj);
            WriteMtString(buffer, obj.LoginToken);
        }

        public override L2CLoginRes Read(IBuffer buffer)
        {
            L2CLoginRes obj = new L2CLoginRes();
            ReadServerResponse(buffer, obj);
            obj.LoginToken = ReadMtString(buffer);
            return obj;
        }
    }
}
