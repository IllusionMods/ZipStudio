using System.IO;
using System.IO.Compression;
using System.Text;

namespace ZipStudio.Core
{
    public static class Utility
    {
        public static MemoryStream ExtractToMemory(this ZipArchiveEntry entry)
        {
            var memStream = new MemoryStream();
            using (var sourceStream = entry.Open())
            {
                sourceStream.CopyTo(memStream);
            }

            memStream.Position = 0;
            return memStream;
        }

        public static void AddFileWithName(this ZipArchive archive, string fileName, string name)
        {
            var entry = archive.CreateEntry(name);

            using (var sourceStream = File.OpenRead(fileName))
            using (var entryStream = entry.Open())
            {
                sourceStream.CopyTo(entryStream);
            }
        }

        public static void AddStringEntry(this ZipArchive archive, string name, string contents)
        {
            var entry = archive.CreateEntry(name);

            using (var stream = entry.Open())
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                writer.Write(contents);
            }
        }
    }
}
