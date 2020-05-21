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
    }
}
