using System.IO;

namespace P3___Networked_Producer.Models
{
    public class VideoFileItem
    {
        public required string FilePath { get; set; }
        public string DisplayName => "🎞 " + Path.GetFileName(FilePath);
    }
}
