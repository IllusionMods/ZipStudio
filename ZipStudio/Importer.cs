using AssetStudio;
using MessagePack;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
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
                Filters = { new CommonFileDialogFilter("Compressed mod", "*.zipmod"), new CommonFileDialogFilter("Compressed folder", "*.zip") },
                Title = "Save new .zip file",
                AlwaysAppendDefaultExtension = true
            };

            if (saveDialog.ShowDialog() != CommonFileDialogResult.Ok)
                return false;

            string sourceDir = openDialog.FileName;
            string savePath = saveDialog.FileName;

            if (!savePath.ToLower().EndsWith(".zipmod") && (saveDialog.SelectedFileTypeIndex == 1))
                savePath += ".zipmod";

            if (!savePath.ToLower().EndsWith(".zip") && (saveDialog.SelectedFileTypeIndex == 2))
                savePath += ".zip";

            if (File.Exists(savePath))
                File.Delete(savePath);

            Manifest manifest = new Manifest
            {
                Guid = "<not set>"
            };

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
                    return false;
                }
            }

            string GetEntryName(string fullPath)
            {
                return fullPath.Remove(0, rootDir.Length).Trim('\\', '/').Replace('\\', '/');
            }

            using (var zipStream = File.Create(savePath))
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, false, Encoding.UTF8))
            {
                var addedDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                void AddDirectoryByName(string entryName)
                {
                    entryName = entryName.Trim('\\', '/').Replace('\\', '/');
                    if (string.IsNullOrWhiteSpace(entryName))
                        return;

                    entryName += "/";

                    if (addedDirectories.Add(entryName))
                        archive.CreateEntry(entryName);
                }

                void AddFile(string sourcePath, string entryName)
                {
                    entryName = entryName.TrimStart('/').Replace('\\', '/');
                    archive.AddFileWithName(sourcePath, entryName);
                }

                void AddTextFile(string entryName, string contents)
                {
                    entryName = entryName.TrimStart('/').Replace('\\', '/');
                    archive.AddStringEntry(entryName, contents);
                }

                void TryAddFolderFromRoot(string prefix)
                {
                    string totalPath = Path.Combine(rootDir, prefix);

                    if (Directory.Exists(totalPath))
                    {
                        AddDirectoryByName(prefix);

                        foreach (string subFilePath in Directory.GetFiles(totalPath, "*", SearchOption.AllDirectories))
                            AddFile(subFilePath, GetEntryName(subFilePath));

                        foreach (string subDirPath in Directory.GetDirectories(totalPath, "*", SearchOption.AllDirectories))
                            AddDirectoryByName(GetEntryName(subDirPath));
                    }
                }

                //need to add abdata with special care
                AddDirectoryByName("abdata");
                TryAddFolderFromRoot("UserData");

                //Export csv files
                if (Directory.Exists(Path.Combine(rootDir, "abdata/list/characustom/")))
                {
                    AssetsManager assetsManager = new AssetsManager();
                    assetsManager.LoadFolder(Path.Combine(rootDir, "abdata/list/characustom/"));
                    foreach (var assetFile in assetsManager.assetsFileList)
                    {
                        bool hasOtherAssets = false;

                        foreach (var asset in assetFile.Objects.Select(x => x.Value))
                        {
                            switch (asset)
                            {
                                case TextAsset textAsset:
                                    string fileName = $"abdata/list/characustom/{Path.GetFileNameWithoutExtension(assetFile.originalPath)}_{textAsset.m_Name}.csv";
                                    AddTextFile(fileName, ExportCSV(textAsset.m_Script));
                                    break;
                                case AssetBundle ab:
                                    break;
                                default:
                                    hasOtherAssets = true;
                                    break;
                            }
                        }

                        //If the list file has assets other than just text lists add it too. It may be necessary for the mod to work.
                        if (hasOtherAssets)
                            AddFile(assetFile.originalPath, GetEntryName(assetFile.originalPath));
                    }
                }

                //Export studio csv files
                if (Directory.Exists(Path.Combine(rootDir, "abdata/studio/info/")))
                {
                    AssetsManager assetsManager = new AssetsManager();
                    assetsManager.LoadFolder(Path.Combine(rootDir, "abdata/studio/info/"));
                    foreach (var assetFile in assetsManager.assetsFileList)
                    {
                        bool hasOtherAssets = false;
                        bool directoryAdded = false;

                        foreach (var asset in assetFile.Objects.Select(x => x.Value))
                        {
                            switch (asset)
                            {
                                case MonoBehaviour monoBehaviour:
                                    if (monoBehaviour.m_Script.TryGet(out var monoScript) && monoScript.m_Name == "ExcelData")
                                    {
                                        if (!directoryAdded)
                                        {
                                            AddDirectoryByName($"abdata/studio/info/{Path.GetFileNameWithoutExtension(assetFile.originalPath)}");
                                            directoryAdded = true;
                                        }
                                        string fileName = $"abdata/studio/info/{Path.GetFileNameWithoutExtension(assetFile.originalPath)}/{monoBehaviour.m_Name}.csv";

                                        monoBehaviour.reader.Reset();
                                        AddTextFile(fileName, ExportStudioCSV(monoBehaviour.serializedType.m_Nodes, monoBehaviour.reader));
                                    }
                                    break;
                                case AssetBundle ab:
                                case MonoScript ms:
                                    break;
                                default:
                                    hasOtherAssets = true;
                                    break;
                            }
                        }

                        if (hasOtherAssets)
                            AddFile(assetFile.originalPath, GetEntryName(assetFile.originalPath));
                    }
                }

                //Add the rest of the unity3d files
                foreach (string subFilePath in Directory.GetFiles(Path.Combine(rootDir, "abdata"), "*", SearchOption.AllDirectories))
                {
                    string newPath = GetEntryName(subFilePath);

                    //These are handled elsewhere
                    if (newPath.ToLower().StartsWith("abdata/list/characustom/") || newPath.ToLower().StartsWith("abdata/studio/info/"))
                        continue;

                    if (subFilePath.EndsWith(".unity3d"))
                        AddFile(subFilePath, newPath);
                }

                //Add all directories
                foreach (string subDirPath in Directory.GetDirectories(Path.Combine(rootDir, "abdata"), "*", SearchOption.AllDirectories))
                {
                    AddDirectoryByName(GetEntryName(subDirPath));
                }

                //Add manifest
                AddTextFile("manifest.xml", manifest.Export());
            }

            mod = new Mod(savePath);
            return true;
        }

        #region CSV
        private static string ExportStudioCSV(List<TypeTreeNode> members, BinaryReader reader)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < members.Count; i++)
            {
                ParseExcelData(sb, members, reader, ref i);
            }
            return sb.ToString();
        }

        private static string ParseExcelData(StringBuilder sb, List<TypeTreeNode> members, BinaryReader reader, ref int i)
        {
            var member = members[i];
            var level = member.m_Level;
            var varTypeStr = member.m_Type;
            var varNameStr = member.m_Name;
            var align = (member.m_MetaFlag & 0x4000) != 0;
            string output = "";
            switch (varTypeStr)
            {
                case "SInt8":
                    reader.ReadSByte();
                    break;
                case "UInt8":
                    reader.ReadByte();
                    break;
                case "short":
                case "SInt16":
                    reader.ReadInt16();
                    break;
                case "UInt16":
                case "unsigned short":
                    reader.ReadUInt16();
                    break;
                case "int":
                case "SInt32":
                    reader.ReadInt32();
                    break;
                case "UInt32":
                case "unsigned int":
                case "Type*":
                    reader.ReadUInt32();
                    break;
                case "long long":
                case "SInt64":
                    reader.ReadInt64();
                    break;
                case "UInt64":
                case "unsigned long long":
                    reader.ReadUInt64();
                    break;
                case "float":
                    reader.ReadSingle();
                    break;
                case "double":
                    reader.ReadDouble();
                    break;
                case "bool":
                    reader.ReadBoolean();
                    break;
                case "string":
                    var str = reader.ReadAlignedString();
                    if (i == 0) //came here from case "vector". don't write any strings from any other source.
                    {
                        output = str;
                        sb.Append(str);
                        sb.Append(',');
                    }
                    i += 3;
                    break;
                case "vector":
                    {
                        if ((members[i + 1].m_MetaFlag & 0x4000) != 0)
                            align = true;
                        var size = reader.ReadInt32();
                        var vector = GetMembers(members, level, i);
                        i += vector.Count - 1;
                        vector.RemoveRange(0, 3);

                        bool doLineEnd = false;
                        bool dataRow = i == 6;
                        for (int j = 0; j < size; j++)
                        {
                            int tmp = 0;
                            string returnValue = ParseExcelData(sb, vector, reader, ref tmp);
                            if (j == 0 && dataRow && returnValue == "")
                            {
                                //first cell of the row is empty
                                //remove the added comma and don't write the rest of the row
                                sb.Length--;
                                break;
                            }
                            else
                                doLineEnd = true;
                        }

                        //remove the comma at the end of the row and add a line break
                        if (doLineEnd && dataRow)
                        {
                            sb.Length--;
                            sb.Append(Environment.NewLine);
                        }
                        break;
                    }
                case "map":
                    {
                        if ((members[i + 1].m_MetaFlag & 0x4000) != 0)
                            align = true;
                        var size = reader.ReadInt32();
                        var map = GetMembers(members, level, i);
                        i += map.Count - 1;
                        map.RemoveRange(0, 4);
                        var first = GetMembers(map, map[0].m_Level, 0);
                        map.RemoveRange(0, first.Count);
                        var second = map;
                        for (int j = 0; j < size; j++)
                        {
                            int tmp1 = 0;
                            int tmp2 = 0;
                            ParseExcelData(sb, first, reader, ref tmp1);
                            ParseExcelData(sb, second, reader, ref tmp2);
                        }
                        break;
                    }
                case "TypelessData":
                    {
                        var size = reader.ReadInt32();
                        reader.ReadBytes(size);
                        i += 2;
                        break;
                    }
                default:
                    {
                        if (i != members.Count && members[i + 1].m_Type == "Array")
                        {
                            goto case "vector";
                        }
                        var @class = GetMembers(members, level, i);
                        @class.RemoveAt(0);
                        i += @class.Count;
                        for (int j = 0; j < @class.Count; j++)
                        {
                            ParseExcelData(sb, @class, reader, ref j);
                        }
                        break;
                    }
            }
            if (align)
                reader.AlignStream(4);
            return output;
        }

        private static List<TypeTreeNode> GetMembers(List<TypeTreeNode> members, int level, int index)
        {
            var member2 = new List<TypeTreeNode>
            {
                members[0]
            };
            for (int i = index + 1; i < members.Count; i++)
            {
                var member = members[i];
                var level2 = member.m_Level;
                if (level2 <= level)
                {
                    return member2;
                }
                member2.Add(member);
            }
            return member2;
        }

        private static string ExportCSV(byte[] data)
        {
            ChaListData chaListData = MessagePackSerializer.Deserialize<ChaListData>(data);

            StringBuilder bodyBuilder = new StringBuilder();

            bodyBuilder.AppendLine(chaListData.categoryNo.ToString());
            bodyBuilder.AppendLine(chaListData.distributionNo.ToString());
            bodyBuilder.AppendLine(chaListData.filePath);

            bodyBuilder.AppendLine(chaListData.lstKey.Aggregate((a, b) => $"{a},{b}"));

            foreach (var entry in chaListData.dictList.Select(x => x.Value))
            {
                bodyBuilder.AppendLine(entry.Aggregate((a, b) => $"{a},{b}"));
            }

            return bodyBuilder.ToString();
        }
        #endregion
    }
}
