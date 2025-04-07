using System.IO;

namespace P3___Networked_Producer.Models
{
    public class FolderItem
    {
        public required string FolderPath { get; set; }
        public string DisplayName => "📂 " + Path.GetFileName(FolderPath);
    }
}
