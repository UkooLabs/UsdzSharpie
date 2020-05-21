using K4os.Compression.LZ4;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace UsdzSharpie
{
    public class UsdcReader
    {
        const string usdcHeader = "PXR-USDC";

        public void ReadUsdc(string filename)
        {
            using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                ReadUsdc(stream);
            }
        }

        private string ReadString(BinaryReader binaryReader, int size)
        {
            var buffer = ReadBytes(binaryReader, size);
            for (var i = 0; i < buffer.Length; i++)
            {
                if (buffer[i] == 0)
                {
                    return Encoding.ASCII.GetString(buffer, 0, i);
                }
            }
            return Encoding.ASCII.GetString(buffer);
        }

        private byte[] ReadBytes(BinaryReader binaryReader, int size)
        {
            var buffer = binaryReader.ReadBytes(size);
            if (buffer.Length != size)
            {
                throw new Exception("Unexpected byte count read.");
            }
            return buffer;
        }

        private T ByteToType<T>(BinaryReader binaryReader)
        {
            var bytes = binaryReader.ReadBytes(Marshal.SizeOf(typeof(T)));
            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            T theStructure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();
            return theStructure;
        }

        private UsdcVersion ReadVersion(BinaryReader binaryReader)
        {
            var version = new UsdcVersion
            {
                Major = binaryReader.ReadByte(),
                Minor = binaryReader.ReadByte(),
                Patch = binaryReader.ReadByte()
            };
            _ = ReadBytes(binaryReader, 5);
            return version;
        }

        private UsdcSection[] ReadTocSections(BinaryReader binaryReader)
        {
            var tocSections = new List<UsdcSection>();
            var tocOffset = binaryReader.ReadUInt64();
            binaryReader.BaseStream.Position = (long)tocOffset;
            var tocCount = binaryReader.ReadUInt64();
            for (var i = (ulong)0; i < tocCount; i++)
            {
                tocSections.Add(new UsdcSection {
                    Token = ReadString(binaryReader, 16),
                    Offset = binaryReader.ReadUInt64(),
                    Size = binaryReader.ReadUInt64()
                });
            }
            return tocSections.ToArray();
        }

        private byte[] DecompressFromBuffer(byte[] compressedBuffer, ulong uncompressedSize)
        {
            var uncompressedBuffer = new byte[uncompressedSize];
            var chunks = compressedBuffer[0];
            if (chunks == 0)
            {
                if (LZ4Codec.Decode(compressedBuffer, 1, compressedBuffer.Length - 1, uncompressedBuffer, 0, (int)uncompressedSize) != (int)uncompressedSize)
                {
                    throw new Exception("Unexpected decompressed size");
                }
            }
            else
            {
                int offset = 1;
                int totalDecompressed = 0;
                for (var i = 0; i < chunks; i++)
                {
                    var chunkSize = BitConverter.ToInt32(compressedBuffer, offset);
                    offset += sizeof(int);
                    var decompressedSize = LZ4Codec.Decode(compressedBuffer, offset, chunkSize, uncompressedBuffer, totalDecompressed, (int)uncompressedSize);
                    if (decompressedSize < 0)
                    {
                        throw new Exception("Unexpected decompressed chunk size");
                    }
                    offset += chunkSize;
                    totalDecompressed += decompressedSize;
                }
                if (totalDecompressed != (int)uncompressedSize)
                {
                    throw new Exception("Unexpected decompressed total size");
                }
            }

            return uncompressedBuffer;
        }

        private string[] SplitBufferIntoStrings(byte[] buffer)
        {
            var stringBuilder = new StringBuilder();
            var result = new List<string>();
            for (var i = 0; i < buffer.Length; i++)
            {
                if (buffer[i] == 0)
                {
                    if (stringBuilder.Length > 0)
                    {
                        result.Add(stringBuilder.ToString());
                        stringBuilder.Clear();
                    }
                    continue;
                }
                stringBuilder.Append((char)buffer[i]);
            }
            return result.ToArray();
        }

        private string[] ReadTokens(BinaryReader binaryReader, ulong offset, ulong size)
        {
            binaryReader.BaseStream.Position = (long)offset;

            var tokenCount = binaryReader.ReadUInt64();
            var uncompressedSize = binaryReader.ReadUInt64();
            var compressedSize = binaryReader.ReadUInt64();
            if (compressedSize + 24 != size)
            {
                throw new Exception("Unexpected compressed size");
            }

            var compressedBuffer = ReadBytes(binaryReader, (int)compressedSize);
            var uncompressedBuffer = DecompressFromBuffer(compressedBuffer, uncompressedSize);

            var tokens = SplitBufferIntoStrings(uncompressedBuffer);
            if (tokens.Length != (int)tokenCount - 1)
            {
                throw new Exception("Unexpected token count");
            }

            return tokens;
        }


        private void ReadStrings(BinaryReader binaryReader, ulong offset, ulong size)
        {
            binaryReader.BaseStream.Position = (long)offset;
        }

        private void ReadFields(BinaryReader binaryReader, ulong offset, ulong size)
        {
            binaryReader.BaseStream.Position = (long)offset;
        }

        private void ReadFieldSets(BinaryReader binaryReader, ulong offset, ulong size)
        {
            binaryReader.BaseStream.Position = (long)offset;
        }

        private void ReadPaths(BinaryReader binaryReader, ulong offset, ulong size)
        {
            binaryReader.BaseStream.Position = (long)offset;
        }

        private void ReadSpecs(BinaryReader binaryReader, ulong offset, ulong size)
        {
            binaryReader.BaseStream.Position = (long)offset;
        }

        public void ReadUsdc(Stream stream)
        {
            using (var binaryReader = new BinaryReader(stream))
            {
                // Read header
                var header = ReadString(binaryReader, 8);
                if (!header.Equals(usdcHeader))
                {
                    throw new Exception("Unrecognised header");
                }

                // Read version info
                var version = ReadVersion(binaryReader);
                if (version.Major == 0 && version.Minor < 4)
                {
                    throw new Exception("Version should be at least 0.4.0");
                }
                
                // Read toc sections
                var tocSections = ReadTocSections(binaryReader);
                foreach (var section in tocSections)
                {
                    if (section.Token.Equals("TOKENS"))
                    {
                        var tokens = ReadTokens(binaryReader, section.Offset, section.Size);
                    }
                    else if (section.Token.Equals("STRINGS"))
                    {
                        ReadStrings(binaryReader, section.Offset, section.Size);
                    }
                    else if (section.Token.Equals("FIELDS"))
                    {
                        ReadFields(binaryReader, section.Offset, section.Size);
                    }
                    else if (section.Token.Equals("FIELDSETS"))
                    {
                        ReadFieldSets(binaryReader, section.Offset, section.Size);
                    }
                    else if (section.Token.Equals("PATHS"))
                    {
                        ReadPaths(binaryReader, section.Offset, section.Size);
                    }
                    else if (section.Token.Equals("SPECS"))
                    {
                        ReadSpecs(binaryReader, section.Offset, section.Size);
                    }
                }
            }
        }
    }
}