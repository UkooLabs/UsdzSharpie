using K4os.Compression.LZ4;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UsdzSharpie.Compression;

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
            var uncompressedBuffer = Decompressor.DecompressFromBuffer(compressedBuffer, uncompressedSize, true);

            var tokens = SplitBufferIntoStrings(uncompressedBuffer);
            if (tokens.Length != (int)tokenCount - 1)
            {
                throw new Exception("Unexpected token count");
            }

            return tokens;
        }


        private uint[] ReadStrings(BinaryReader binaryReader, ulong offset, ulong size)
        {
            binaryReader.BaseStream.Position = (long)offset;

            var indexCount = binaryReader.ReadUInt64();
            var result = new List<uint>();
            for (var i = 0; i < (int)indexCount; i++)
            {
                result.Add(binaryReader.ReadUInt32());
            }
            return result.ToArray();
        }

        //public const ulong LZ4_MAX_INPUT_SIZE = 0x7E000000;

        //public ulong GetMaxInputSize()
        //{
        //    return 127 * LZ4_MAX_INPUT_SIZE;
        //}

        //public ulong LZ4_compressBound(ulong size)
        //{
        //    return size > LZ4_MAX_INPUT_SIZE ? 0 : size + (size / 255) + 16;
        //}

        //public ulong GetCompressedBufferSize(ulong inputSize)
        //{
        //    if (inputSize > GetMaxInputSize())
        //    {
        //        return 0;
        //    }

        //    if (inputSize <= LZ4_MAX_INPUT_SIZE)
        //    {
        //        return LZ4_compressBound(inputSize) + 1;
        //    }
        //    ulong nWholeChunks = inputSize / LZ4_MAX_INPUT_SIZE;
        //    ulong partChunkSz = inputSize % LZ4_MAX_INPUT_SIZE;
        //    ulong sz = 1 + nWholeChunks * (LZ4_compressBound(LZ4_MAX_INPUT_SIZE) + sizeof(int));
        //    if (partChunkSz > 0)
        //    {
        //        sz += LZ4_compressBound(partChunkSz) + sizeof(int);
        //    }
        //    return sz;
        //}

        public ulong GetEncodedBufferSize(ulong count)
        {
            return count > 0 ? (sizeof(int)) + ((count * 2 + 7) / 8) + (count * sizeof(int)) : 0;
        }

        private string DebugString(int index,  ulong value)
        {
            var type = (value >> 48) & 0xff;
            var isArray = (value & ((ulong)1 << 63)) > 0;
            var isInlined = (value & ((ulong)1 << 62)) > 0;
            var isCompressed = (value & ((ulong)1 << 61)) > 0;
            var payload = (value & ((ulong)1 << 48) - 1);
            return $"name = {tokens[index]}, ty: {type}, isArray: {isArray}, isInlined: {isInlined}, isCompressed: {isCompressed}, payload: {payload}";
        }


        private UsdcField[] ReadFields(BinaryReader binaryReader, ulong offset, ulong size)
        {
            binaryReader.BaseStream.Position = (long)offset;

            var fieldCount = binaryReader.ReadUInt64();
            var fieldSize = binaryReader.ReadUInt64();
            
            var compressedBuffer = binaryReader.ReadBytes((int)fieldSize);

            var workSpaceSize = GetEncodedBufferSize(fieldCount);

            var uncompressedBuffer = Decompressor.DecompressFromBuffer(compressedBuffer, workSpaceSize, false);
            var results = IntegerDecoder.DecodeIntegers(uncompressedBuffer, fieldCount);
            if (results.Length != (int)fieldCount)
            {
                throw new Exception("Unexpected field count");
            }

            var repsSize = binaryReader.ReadUInt64();
            var repBuffer = binaryReader.ReadBytes((int)repsSize);
            var repuncompressedBuffer = Decompressor.DecompressFromBuffer(repBuffer, fieldCount * 8, true);

            var fields = new List<UsdcField>();
            for (var i = 0; i < (int)fieldCount; i++)
            {
                var index = results[i];
                var flags = BitConverter.ToUInt64(repuncompressedBuffer, i * 8);
                fields.Add(new UsdcField { 
                    Index = index, 
                    Flags = flags 
                });
            }
            return fields.ToArray();
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

        private string[] tokens;

        private uint[] indices;

        private UsdcField[] fields;

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
                        tokens = ReadTokens(binaryReader, section.Offset, section.Size);
                    }
                    else if (section.Token.Equals("STRINGS"))
                    {
                        indices = ReadStrings(binaryReader, section.Offset, section.Size);
                    }
                    else if (section.Token.Equals("FIELDS"))
                    {
                        fields = ReadFields(binaryReader, section.Offset, section.Size);
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