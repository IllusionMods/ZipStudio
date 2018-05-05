using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;

namespace ZipStudio.Core
{
    public class Mod : IDisposable
    {
        public ZipFile ZipFile { get; protected set; }

        public Manifest Manifest { get; protected set; } 

        public Mod(string filename)
        {
            ZipFile = new ZipFile(filename);

            var manifestentry = ZipFile.GetEntry("manifest.xml");

            if (manifestentry != null)
            {
                Manifest = new Manifest(ZipFile.GetInputStream(manifestentry));
            }
            else
            {
                Manifest = new Manifest();
            }
        }

        public void Dispose()
        {
            ZipFile.Close();
        }
    }
}
