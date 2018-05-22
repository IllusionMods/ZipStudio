using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ionic.Zip;

namespace ZipStudio.Core
{
    public class Mod : IDisposable
    {
        public string Filename { get; protected set; }

        public ZipFile ZipFile { get; protected set; }

        public Manifest Manifest { get; protected set; } 

        public Mod(string filename)
        {
            Filename = filename;
            ZipFile = new ZipFile(filename);

            var manifestentry = ZipFile.Entries.FirstOrDefault(x => x.FileName == "manifest.xml");

            if (manifestentry != null)
            {
                Manifest = new Manifest(manifestentry.ExtractToMemory());
            }
            else
            {
                Manifest = new Manifest();
            }
        }

        public void Save()
        {
            Save(Filename);
        }

        public void Save(string filename)
        {
            var manifestentry = ZipFile.Entries.FirstOrDefault(x => x.FileName == "manifest.xml");

            if (manifestentry != null)
            {
                ZipFile.RemoveEntry(manifestentry);
            }

            ZipFile.AddEntry("manifest.xml", Manifest.ExportAsBytes());

            ZipFile.Save(filename);
        }

        public void Dispose()
        {
            ZipFile.Dispose();
        }
    }
}
