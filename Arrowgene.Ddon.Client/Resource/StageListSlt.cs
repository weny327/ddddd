using System.Collections.Generic;
using Arrowgene.Buffers;

namespace Arrowgene.Ddon.Client.Resource
{
    public class StageListSlt : ResourceFile
    {
        public class Info
        {
            public uint StageNo { get; set; }
            public uint Type { get; set; }
            public byte RecommendLevel { get; set; }
            public uint MessageId { get; set; }
            public uint Version { get; set; }
        }

        public List<Info> AreaInfos { get; }

        public StageListSlt()
        {
            AreaInfos = new List<Info>();
        }

        protected override void ReadResource(IBuffer buffer)
        {
            AreaInfos.Clear();
            List<Info> infos = ReadMtArray(buffer, ReadEntry);
            AreaInfos.AddRange(infos);
        }

        private Info ReadEntry(IBuffer buffer)
        {
            Info entry = new Info();
            entry.StageNo = ReadUInt32(buffer);
            entry.Type = ReadUInt32(buffer);
            entry.RecommendLevel = buffer.ReadByte();
            entry.MessageId = ReadUInt32(buffer);
            entry.Version = ReadUInt32(buffer);
            return entry;
        }
    }
}
