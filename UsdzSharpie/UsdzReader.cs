using System;
using System.IO;
using System.IO.Compression;

namespace UsdzSharpie
{
    public class UsdzReader
    {
        public void ReadUsdz(string filename)
        {
            using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                ReadUsdz(stream);
            }
        }

        public void ReadUsdz(Stream stream)
        {
            using (var zipArchive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                ValidateUsdz(stream, zipArchive);

                foreach (var entry in zipArchive.Entries)
                {
                    if (Path.GetExtension(entry.Name).Equals(".usdc", StringComparison.CurrentCultureIgnoreCase))
                    {
                        var usdcReader = new UsdcReader();
                        {
                            using (var entryStream = entry.Open())
                            using (var memoryStream = new MemoryStream())
                            {
                                entryStream.CopyTo(memoryStream);
                                memoryStream.Position = 0;
                                usdcReader.ReadUsdc(memoryStream);
                            }
                        }
                    }
                    
                }
            }
        }

        private void ValidateUsdz(Stream stream, ZipArchive zipArchive)
        {
            foreach (var entry in zipArchive.Entries)
            {
                using (var entryStream = entry.Open())
                {
                    var offset = stream.Position;
                    if (offset % 64 != 0)
                    {
                        throw new Exception("Zip entry offset must be mulitple of 64 bytes");
                    }
                    Logger.LogLine($"offset = {offset}");
                }
            }
            for (int i = 0; i < zipArchive.Entries.Count; i++)
            {
                ZipArchiveEntry entry = zipArchive.Entries[i];
                using (var entryStream = entry.Open())
                {
                    Logger.LogLine($"[{i}] {entry.Name} : byte range ({stream.Position}, {stream.Position + entry.Length})");
                }
            }
        }
    }
}
