using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ionic.Zip;

namespace ZipStudio.Core
{
    public static class Utility
    {
        public static MemoryStream ExtractToMemory(this ZipEntry entry)
        {
            MemoryStream memStream = new MemoryStream();
            entry.Extract(memStream);
            memStream.Position = 0;
            return memStream;
        }

        public static ZipEntry AddFileWithName(this ZipFile file, string fileName, string name)
        {
            string directory = "";

            if (name.Contains('/'))
                directory = name.Remove(name.LastIndexOf('/'));

            var entry = file.AddFile(fileName, directory);
            entry.FileName = name;

            return entry;
        }
    }
}
