using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace ZipStudio.Core
{
    public class ZipEntryInfo
    {
        public string FileName { get; set; }
        public bool IsDirectory { get; set; }
    }

    public class Mod : IDisposable
    {
        public string Filename { get; protected set; }
        public IReadOnlyCollection<ZipEntryInfo> Entries { get; protected set; }
        public Manifest Manifest { get; protected set; }

        public Mod(string filename)
        {
            Filename = filename;
            LoadFromFile();
        }

        private void LoadFromFile()
        {
            var entries = new Dictionary<string, ZipEntryInfo>(StringComparer.OrdinalIgnoreCase);

            using (var archive = ZipFile.OpenRead(Filename))
            {
                foreach (var entry in archive.Entries)
                {
                    var entryName = NormalizeEntryName(entry.FullName);
                    var isDirectory = entryName.EndsWith("/");

                    AddEntry(entries, entryName, isDirectory);

                    if (!isDirectory)
                    {
                        AddParentDirectories(entries, entryName);
                    }
                }

                var manifestEntry = archive.Entries.FirstOrDefault(x => NormalizeEntryName(x.FullName).Equals("manifest.xml", StringComparison.OrdinalIgnoreCase));
                Manifest = manifestEntry == null ? new Manifest() : new Manifest(manifestEntry.ExtractToMemory());
            }

            Entries = entries.Values.ToArray();
        }

        private static string NormalizeEntryName(string entryName)
        {
            return (entryName ?? string.Empty).Replace('\\', '/').TrimStart('/');
        }

        private static void AddEntry(IDictionary<string, ZipEntryInfo> entries, string entryName, bool isDirectory)
        {
            if (string.IsNullOrWhiteSpace(entryName))
                return;

            if (isDirectory && !entryName.EndsWith("/"))
                entryName += "/";

            if (!entries.TryGetValue(entryName, out var existing))
            {
                entries[entryName] = new ZipEntryInfo
                {
                    FileName = entryName,
                    IsDirectory = isDirectory
                };
                return;
            }

            if (isDirectory && !existing.IsDirectory)
                existing.IsDirectory = true;
        }

        private static void AddParentDirectories(IDictionary<string, ZipEntryInfo> entries, string entryName)
        {
            var currentSlash = entryName.LastIndexOf('/');
            while (currentSlash > 0)
            {
                var dirName = entryName.Substring(0, currentSlash + 1);
                AddEntry(entries, dirName, true);
                currentSlash = entryName.LastIndexOf('/', currentSlash - 1);
            }
        }

        public void Save()
        {
            Save(Filename);
        }

        public void Save(string filename)
        {
            var tempPath = Path.GetTempFileName();

            try
            {
                using (var sourceArchive = ZipFile.OpenRead(Filename))
                using (var tempStream = File.Create(tempPath))
                using (var targetArchive = new ZipArchive(tempStream, ZipArchiveMode.Create, false, System.Text.Encoding.UTF8))
                {
                    foreach (var sourceEntry in sourceArchive.Entries)
                    {
                        if (NormalizeEntryName(sourceEntry.FullName).Equals("manifest.xml", StringComparison.OrdinalIgnoreCase))
                            continue;

                        var entryName = NormalizeEntryName(sourceEntry.FullName);
                        var isDirectory = entryName.EndsWith("/");

                        if (isDirectory)
                        {
                            targetArchive.CreateEntry(entryName);
                            continue;
                        }

                        var targetEntry = targetArchive.CreateEntry(entryName);
                        using (var sourceStream = sourceEntry.Open())
                        using (var targetStream = targetEntry.Open())
                        {
                            sourceStream.CopyTo(targetStream);
                        }
                    }

                    targetArchive.AddStringEntry("manifest.xml", Manifest.Export());
                }

                File.Copy(tempPath, filename, true);
                Filename = filename;
                LoadFromFile();
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }

        public void Dispose()
        {
        }
    }
}
