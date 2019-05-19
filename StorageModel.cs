using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmbedStorage
{
    [Serializable]
    public class StorageModel
    {
        public Guid FileId { get; set; }

        public string Path { get; set; }

        public string FileName { get; set; }

        public long FileLength { get; set; }

        public string Extention { get; set; }

        public DateTime UploadDate { get; set; }

        public DateTime DateExpires { get; set; }
    }
}
