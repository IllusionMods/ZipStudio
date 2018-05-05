using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using MessagePack;
using Microsoft.WindowsAPICodePack.Dialogs;
using Mozilla.NUniversalCharDet.Prober;
using Unity_Studio;
using ZipStudio.Core;

namespace ZipStudio
{
    public static class Importer
    {
        public static bool ImportFromDirectory(out Mod mod, out string message)
        {
            mod = null;
            message = "";

            CommonOpenFileDialog openDialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Open mod folder",
                Multiselect = false
            };

            if (openDialog.ShowDialog() != CommonFileDialogResult.Ok)
                return false;

            CommonSaveFileDialog saveDialog = new CommonSaveFileDialog
            {
                Filters = { new CommonFileDialogFilter("Compressed folder", "*.zip") },
                Title = "Save new .zip file",
                AlwaysAppendDefaultExtension = true
            };

            if (saveDialog.ShowDialog() != CommonFileDialogResult.Ok)
                return false;

            string sourceDir = openDialog.FileName;
            string savePath = saveDialog.FileName;

            if (!savePath.ToLower().EndsWith(".zip"))
                savePath += ".zip";

            if (File.Exists(savePath))
                File.Delete(savePath);

            ZipFile file = ZipFile.Create(savePath);

            Manifest manifest = new Manifest();
            manifest.Guid = "<not set>";

            //check for root abdata folder
            string rootDir = "";
            if (Path.GetFileName(sourceDir).ToLower() == "abdata")
                rootDir = Path.GetDirectoryName(sourceDir);
            else if (Directory.Exists(Path.Combine(sourceDir, "abdata")))
                rootDir = sourceDir;
            else
            {
                //we have to try and find abdata
                string potentialDirectory =
                    Directory.EnumerateDirectories(sourceDir, "abdata", SearchOption.AllDirectories).FirstOrDefault() ?? "";

                if (potentialDirectory != "")
                    rootDir = Path.GetDirectoryName(potentialDirectory);
                else
                {
                    //no abdata folder exists
                    message = "Cannot find \"abdata\" folder!";

                    file.CommitUpdate();
                    file.Close();
                    File.Delete(savePath);

                    return false;
                }
            }

            string textFilePath = Directory.EnumerateFiles(rootDir, "*.txt", SearchOption.TopDirectoryOnly).FirstOrDefault() ?? "";

            if (textFilePath != "")
            {
                byte[] data = File.ReadAllBytes(textFilePath);

                //try and detect shift-JIS encoding
                SJISProber sjisProber = new SJISProber();
                var result = sjisProber.handleData(data, 0, data.Length);

                var confidence = sjisProber.getConfidence();

                if (confidence > 0.95) //shift-jis text
                    manifest.Description = Encoding.GetEncoding(932).GetString(data);
                else //assume UTF-8 for ASCII compatiblity
                    manifest.Description = Encoding.UTF8.GetString(data);
            }

            void TryAddFolderFromRoot(string prefix)
            {
                string totalPath = Path.Combine(rootDir, prefix);

                if (Directory.Exists(totalPath))
                {
                    foreach (string subFilePath in Directory.GetFiles(Path.Combine(rootDir, prefix), "*", SearchOption.AllDirectories))
                    {
                        file.Add(subFilePath, subFilePath.Remove(0, rootDir.Length).Trim('\\', '/').Replace('\\', '/'));
                    }

                    foreach (string subDirPath in Directory.GetDirectories(Path.Combine(rootDir, prefix), "*", SearchOption.AllDirectories))
                    {
                        file.AddDirectory(subDirPath.Remove(0, rootDir.Length).Trim('\\', '/').Replace('\\', '/') + "/");
                    }
                }
            }
            
            file.BeginUpdate();

            //TryAddFolderFromRoot("abdata");
            TryAddFolderFromRoot("UserData");


            //need to add abdata with special care
            foreach (string subFilePath in Directory.GetFiles(Path.Combine(rootDir, "abdata"), "*", SearchOption.AllDirectories))
            {
                string newPath = subFilePath.Remove(0, rootDir.Length).Trim('\\', '/').Replace('\\', '/');

                if (newPath.ToLower().StartsWith("abdata/list/characustom/"))
                {
                    //extract the lists
                    var contents = GetListContents(subFilePath);

                    foreach (var kv in contents)
                    {
                        file.Add(__tempMakeFile(kv.Value), $"abdata/list/characustom/{kv.Key}");
                    }
                }
                else
                    file.Add(subFilePath, newPath);
            }

            foreach (string subDirPath in Directory.GetDirectories(Path.Combine(rootDir, "abdata"), "*", SearchOption.AllDirectories))
            {
                file.AddDirectory(subDirPath.Remove(0, rootDir.Length).Trim('\\', '/').Replace('\\', '/') + "/");
            }


            //add manifest
            file.Add(__tempMakeFile(manifest.Export()), "manifest.xml");

            file.CommitUpdate();
            file.Close();

            __deleteAllTempPaths();

            mod = new Mod(savePath);
            return true;
        }

        //this is temporary because i currently cannot be fucked making a memory source for the zip entries
        static string __tempMakeFile(string contents)
        {
            string manifestTempPath = Path.GetTempFileName();
            using (var writer = File.CreateText(manifestTempPath))
                writer.Write(contents);

            tempPaths.Add(manifestTempPath);

            return manifestTempPath;
        }

        static List<string> tempPaths = new List<string>();

        static void __deleteAllTempPaths()
        {
            foreach (string tempPath in tempPaths)
                if (File.Exists(tempPath))
                    File.Delete(tempPath);

            tempPaths.Clear();
        }

        #region CSV export
        static Dictionary<string, string> GetListContents(string unityFileName)
        {
            Dictionary<string, string> output = new Dictionary<string, string>();

            BundleFile bundle = new BundleFile(unityFileName);

            AssetsFile assetsFile = new AssetsFile(bundle.MemoryAssetsFileList[0].fileName, new EndianBinaryReader(bundle.MemoryAssetsFileList[0].memStream));

            AssetBundle assetBundle = new AssetBundle(assetsFile.preloadTable.First(x => x.Value.TypeString == "AssetBundle").Value);

            FixNames(assetsFile, assetBundle);

            string baseName = Path.GetFileNameWithoutExtension(unityFileName);

            foreach (var preload in assetsFile.preloadTable)
            {
                var asset = preload.Value;

                if (asset.TypeString == "TextAsset")
                {
                    string name = $"{baseName}_{asset.Text.Remove(0, asset.Text.LastIndexOf('/') + 1)}.csv";

                    output[name] = ExportCSV(ExportTextAsset(preload.Value));
                }
            }

            return output;
        }

        static byte[] ExportTextAsset(AssetPreloadData data)
        {
            return new TextAsset(data, true).m_Script;
        }

        static void FixNames(AssetsFile file, AssetBundle bundle)
        {
            foreach (var item in file.preloadTable)
            {
                var replacename = bundle?.m_Container.Find(y => y.second.asset.m_PathID == item.Value.m_PathID)?.first;
                if (!string.IsNullOrEmpty(replacename))
                {
                    var ex = Path.GetExtension(replacename);
                    item.Value.Text = !string.IsNullOrEmpty(ex) ? replacename.Replace(ex, "") : replacename;
                }
            }
        }

        static string ExportCSV(byte[] data)
        {
            ChaListData chaListData = MessagePackSerializer.Deserialize<ChaListData>(data);

            StringBuilder bodyBuilder = new StringBuilder();

            bodyBuilder.AppendLine(chaListData.categoryNo.ToString());
            bodyBuilder.AppendLine(chaListData.distributionNo.ToString());
            bodyBuilder.AppendLine(chaListData.filePath);

            bodyBuilder.AppendLine(chaListData.lstKey.Aggregate((a, b) => $"{a},{b}"));

            foreach (var entry in chaListData.dictList.OrderBy(x => x.Key).Select(x => x.Value))
            {
                bodyBuilder.AppendLine(entry.Aggregate((a, b) => $"{a},{b}"));
            }

            return bodyBuilder.ToString();
        }
        #endregion
    }
}
