using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P3___Networked_Consumer.Models
{
    public class VideoFileItem
    {
        public string FileName { get; set; } = string.Empty;
        public byte[] FileData { get; set; } = Array.Empty<byte>();
        public string Hash { get; set; } = string.Empty;
    }
}
