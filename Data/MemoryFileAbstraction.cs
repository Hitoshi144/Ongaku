using Microsoft.AspNetCore.WebUtilities;

namespace Ongaku.Data {
    public class MemoryFileAbstraction : TagLib.File.IFileAbstraction {
        public string Name { get; }
        public Stream ReadStream { get; }
        public Stream WriteStream { get; }

        public MemoryFileAbstraction(string name, Stream stream)
        {
            Name = name;
            ReadStream = stream;
            WriteStream = stream;
        }

        public void CloseStream(Stream stream) { }
    }
}
