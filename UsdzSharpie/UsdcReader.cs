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
                //https://github.com/PixarAnimationStudios/USD/blob/be1a80f8cb91133ac75e1fc2a2e1832cd10d91c8/pxr/base/tf/fastCompression.cpp (line 111)

                //size_t totalDecompressed = 0;
                //for (int i = 0; i != nChunks; ++i)
                //{
                //    int32_t chunkSize = 0;
                //    memcpy(&chunkSize, compressed, sizeof(chunkSize));
                //    compressed += sizeof(chunkSize);
                //    int nDecompressed = LZ4_decompress_safe(
                //        compressed, output, chunkSize,
                //        std::min<size_t>(LZ4_MAX_INPUT_SIZE, maxOutputSize));
                //    if (nDecompressed < 0)
                //    {
                //        TF_RUNTIME_ERROR("Failed to decompress data, possibly corrupt? "
        
                //                         "LZ4 error code: %d", nDecompressed);
                //        return 0;
                //    }
                //    compressed += chunkSize;
                //    output += nDecompressed;
                //    maxOutputSize -= nDecompressed;
                //    totalDecompressed += nDecompressed;
                //}
                //return totalDecompressed;

                throw new Exception("TODO not implemnted");
            }

            return uncompressedBuffer;
        }

        public void ReadTokens(BinaryReader binaryReader, ulong offset, ulong size)
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
        }


        public void ReadStrings(BinaryReader binaryReader, ulong offset, ulong size)
        {
            binaryReader.BaseStream.Position = (long)offset;
        }

        public void ReadFields(BinaryReader binaryReader, ulong offset, ulong size)
        {
            binaryReader.BaseStream.Position = (long)offset;
        }

        public void ReadFieldSets(BinaryReader binaryReader, ulong offset, ulong size)
        {
            binaryReader.BaseStream.Position = (long)offset;
        }

        public void ReadPaths(BinaryReader binaryReader, ulong offset, ulong size)
        {
            binaryReader.BaseStream.Position = (long)offset;
        }

        public void ReadSpecs(BinaryReader binaryReader, ulong offset, ulong size)
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
                        ReadTokens(binaryReader, section.Offset, section.Size);
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
