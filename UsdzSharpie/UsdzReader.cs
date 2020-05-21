using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

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
                    var a = entry.Name;
                }
            }
        }
    }
}
