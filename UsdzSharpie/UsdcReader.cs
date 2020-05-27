using K4os.Compression.LZ4;
using K4os.Compression.LZ4.Streams;
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
                    result.Add(stringBuilder.ToString());
                    stringBuilder.Clear();
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
                throw new Exception("Unexpected buffer size");
            }

            var compressedBuffer = ReadBytes(binaryReader, (int)compressedSize);
            var uncompressedBuffer = Decompressor.DecompressFromBuffer(compressedBuffer, uncompressedSize);

            var tokens = SplitBufferIntoStrings(uncompressedBuffer);
            if (tokens.Length != (int)tokenCount)
            {
                throw new Exception("Unexpected token count");
            }

            return tokens;
        }

        private int[] ReadStrings(BinaryReader binaryReader, ulong offset, ulong size)
        {
            binaryReader.BaseStream.Position = (long)offset;

            var indexCount = binaryReader.ReadUInt64();
            if ((indexCount * 4) + 8 != size)
            {
                throw new Exception("Unexpected buffer size");
            }

            var result = new List<int>();
            for (var i = 0; i < (int)indexCount; i++)
            {
                result.Add(binaryReader.ReadInt32());
            }
            return result.ToArray();
        }

        public const ulong LZ4_MAX_INPUT_SIZE = 0x7E000000;

        public ulong GetMaxInputSize()
        {
            return 127 * LZ4_MAX_INPUT_SIZE;
        }

        public ulong LZ4_compressBound(ulong size)
        {
            return size > LZ4_MAX_INPUT_SIZE ? 0 : size + (size / 255) + 16;
        }

        public ulong GetCompressedBufferSize(ulong inputSize)
        {
            if (inputSize > GetMaxInputSize())
            {
                return 0;
            }

            if (inputSize <= LZ4_MAX_INPUT_SIZE)
            {
                return LZ4_compressBound(inputSize) + 1;
            }
            ulong nWholeChunks = inputSize / LZ4_MAX_INPUT_SIZE;
            ulong partChunkSz = inputSize % LZ4_MAX_INPUT_SIZE;
            ulong sz = 1 + nWholeChunks * (LZ4_compressBound(LZ4_MAX_INPUT_SIZE) + sizeof(int));
            if (partChunkSz > 0)
            {
                sz += LZ4_compressBound(partChunkSz) + sizeof(int);
            }
            return sz;
        }

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
            var workspaceSize = GetCompressedBufferSize(GetEncodedBufferSize(fieldCount));
            var uncompressedBuffer = Decompressor.DecompressFromBuffer(compressedBuffer, workspaceSize);
            var indices = IntegerDecoder.DecodeIntegers(uncompressedBuffer, fieldCount);
            if (indices.Length != (int)fieldCount)
            {
                throw new Exception("Unexpected field count");
            }

            var flagSize = binaryReader.ReadUInt64();
            compressedBuffer = binaryReader.ReadBytes((int)flagSize);
            uncompressedBuffer = Decompressor.DecompressFromBuffer(compressedBuffer, fieldCount * 8);

            var bufferOffset = 0;
            var fields = new List<UsdcField>();
            for (var i = 0; i < (int)fieldCount; i++)
            {
                fields.Add(new UsdcField { 
                    Index = indices[i], 
                    Flags = BitConverter.ToUInt64(uncompressedBuffer, bufferOffset)
                });
                bufferOffset += sizeof(ulong);
            }
            return fields.ToArray();
        }

        private int[] ReadFieldSets(BinaryReader binaryReader, ulong offset, ulong size)
        {
            binaryReader.BaseStream.Position = (long)offset;

            var fieldSetCount = binaryReader.ReadUInt64();
            var fieldSetSize = binaryReader.ReadUInt64();

            var compressedBuffer = binaryReader.ReadBytes((int)fieldSetSize);
            var workspaceSize = GetEncodedBufferSize(fieldSetCount);
            var uncompressedBuffer = Decompressor.DecompressFromBuffer(compressedBuffer, workspaceSize);
            var indices = IntegerDecoder.DecodeIntegers(uncompressedBuffer, fieldSetCount);
            if (indices.Length != (int)fieldSetCount)
            {
                throw new Exception("Unexpected field set count");
            }

            return indices;
        }

        private void BuildDecompressedPaths(int[] pathIndices, int[] elementTokenIndices, int[] jumpIndices, int currentIndex, UsdcPath parentPath, ref UsdcPath[] paths)
        {
            var hasChild = false;
            var hasSibling = false;

            do
            {
                var thisIndex = currentIndex++;
                if (parentPath.IsEmpty)
                {
                    // root node.
                    // Assume single root node in the scene.
                    parentPath = UsdcPath.AbsoluteRootPath();
                    paths[pathIndices[thisIndex]] = parentPath;
                }
                else
                {
                    var tokenIndex = elementTokenIndices[thisIndex];
                    var isPrimPropertyPath = tokenIndex < 0;
                    tokenIndex = Math.Abs(tokenIndex);

                    if (tokenIndex >= tokens.Length)
                    {
                        throw new Exception("Unexpected token index");
                    }
                    var elemToken = tokens[tokenIndex];

                    // full path
                    paths[pathIndices[thisIndex]] = isPrimPropertyPath ? parentPath.AppendProperty(elemToken) : parentPath.AppendElement(elemToken);

                    // also set local path for 'primChildren' check
                    paths[pathIndices[thisIndex]].SetLocalPath(new UsdcPath(elemToken));
                }

                // If we have either a child or a sibling but not both, then just
                // continue to the neighbor.  If we have both then spawn a task for the
                // sibling and do the child ourself.  We think that our path trees tend
                // to be broader more often than deep.

                hasChild = (jumpIndices[thisIndex] > 0) || (jumpIndices[thisIndex] == -1);
                hasSibling = (jumpIndices[thisIndex] >= 0);

                if (hasChild)
                {
                    if (hasSibling)
                    {
                        // NOTE(syoyo): This recursive call can be parallelized
                        var siblingIndex = thisIndex + jumpIndices[thisIndex];
                        BuildDecompressedPaths(pathIndices, elementTokenIndices, jumpIndices, siblingIndex, parentPath, ref paths);
                    }
                    // Have a child (may have also had a sibling). Reset parent path.
                    parentPath = paths[pathIndices[thisIndex]];
                }
                // If we had only a sibling, we just continue since the parent path is
                // unchanged and the next thing in the reader stream is the sibling's
                // header.
            } while (hasChild || hasSibling);
        }

        private void BuildNodeHierarchy(int[] pathIndices, int[] elementTokenIndices, int[] jumpIndices, int currentIndex, int parentNodeIndex, ref UsdcNode[] nodes, ref UsdcPath[] paths)
        {
            var hasChild = false;
            var hasSibling = false;

            // NOTE: Need to indirectly lookup index through pathIndexes[] when accessing `_nodes`
            do
            {
                var thisIndex = currentIndex++;
                if (parentNodeIndex == -1)
                {
                    // root node.
                    // Assume single root node in the scene.
                    if (thisIndex != 0)
                    {
                        throw new Exception("Unexpected index");
                    }

                    var root = new UsdcNode(parentNodeIndex, paths[pathIndices[thisIndex]]);

                    nodes[pathIndices[thisIndex]] = root;

                    parentNodeIndex = thisIndex;

                }
                else
                {
                    if (parentNodeIndex >= nodes.Length)
                    {
                        throw new Exception("Unexpected node index");
                    }

                    var node = new UsdcNode(parentNodeIndex, paths[pathIndices[thisIndex]]);

                    nodes[pathIndices[thisIndex]] = node;

                    var name = paths[pathIndices[thisIndex]].local_path_name();
                    nodes[pathIndices[parentNodeIndex]].AddChildren(name, pathIndices[thisIndex]);
                }

                hasChild = (jumpIndices[thisIndex] > 0) || (jumpIndices[thisIndex] == -1);
                hasSibling = (jumpIndices[thisIndex] >= 0);

                if (hasChild)
                {
                    if (hasSibling)
                    {
                        var siblingIndex = thisIndex + jumpIndices[thisIndex];
                        BuildNodeHierarchy(pathIndices, elementTokenIndices, jumpIndices, siblingIndex, parentNodeIndex, ref nodes, ref paths);              
                    }
                    // Have a child (may have also had a sibling). Reset parent node index
                    parentNodeIndex = thisIndex;
                }
                // If we had only a sibling, we just continue since the parent path is
                // unchanged and the next thing in the reader stream is the sibling's
                // header.
            } while (hasChild || hasSibling);

        }

        private void ReadPaths(BinaryReader binaryReader, ulong offset, ulong size)
        {
            binaryReader.BaseStream.Position = (long)offset;

            var pathCount = binaryReader.ReadUInt64();
            if (pathCount != binaryReader.ReadUInt64())
            {
                throw new Exception("Unexpected path count");
            }

            var pathIndexSize = binaryReader.ReadUInt64();

            var compressedBuffer = binaryReader.ReadBytes((int)pathIndexSize);
            var workspaceSize = GetEncodedBufferSize(pathCount);
            var uncompressedBuffer = Decompressor.DecompressFromBuffer(compressedBuffer, workspaceSize);
            var pathIndices = IntegerDecoder.DecodeIntegers(uncompressedBuffer, pathCount);
            if (pathIndices.Length != (int)pathCount)
            {
                throw new Exception("Unexpected field set count");
            }

            var elementTokenIndexSize = binaryReader.ReadUInt64();
            compressedBuffer = binaryReader.ReadBytes((int)elementTokenIndexSize);
            workspaceSize = GetEncodedBufferSize(pathCount);
            uncompressedBuffer = Decompressor.DecompressFromBuffer(compressedBuffer, workspaceSize);
            var elementTokenIndices = IntegerDecoder.DecodeIntegers(uncompressedBuffer, pathCount);
            if (elementTokenIndices.Length != (int)pathCount)
            {
                throw new Exception("Unexpected field set count");
            }

            var jumpSize = binaryReader.ReadUInt64();
            compressedBuffer = binaryReader.ReadBytes((int)jumpSize);
            workspaceSize = GetEncodedBufferSize(pathCount);
            uncompressedBuffer = Decompressor.DecompressFromBuffer(compressedBuffer, workspaceSize);
            var jumpIndices = IntegerDecoder.DecodeIntegers(uncompressedBuffer, pathCount);
            if (jumpIndices.Length != (int)pathCount)
            {
                throw new Exception("Unexpected field set count");
            }

            var paths = new UsdcPath[pathCount];

            BuildDecompressedPaths(pathIndices, elementTokenIndices, jumpIndices, 0, new UsdcPath(), ref paths);

            var nodes = new UsdcNode[pathCount];
            BuildNodeHierarchy(pathIndices, elementTokenIndices, jumpIndices, 0, -1, ref nodes, ref paths);

            var x = 1;
        }




        private UsdcSpec[] ReadSpecs(BinaryReader binaryReader, ulong offset, ulong size)
        {
            binaryReader.BaseStream.Position = (long)offset;

            var specCount = binaryReader.ReadUInt64(); //1661

            var pathIndexSize = binaryReader.ReadUInt64(); //161

            var compressedBuffer = binaryReader.ReadBytes((int)pathIndexSize);
            var workspaceSize = GetEncodedBufferSize(specCount);
            var uncompressedBuffer = Decompressor.DecompressFromBuffer(compressedBuffer, workspaceSize);
            var pathIndices = IntegerDecoder.DecodeIntegers(uncompressedBuffer, specCount);
            if (pathIndices.Length != (int)specCount)
            {
                throw new Exception("Unexpected field count");
            }

            var fieldsetIndexSize = binaryReader.ReadUInt64();
            
            compressedBuffer = binaryReader.ReadBytes((int)fieldsetIndexSize);
            uncompressedBuffer = Decompressor.DecompressFromBuffer(compressedBuffer, workspaceSize);
            var fieldSetIndices = IntegerDecoder.DecodeIntegers(uncompressedBuffer, specCount);
            if (fieldSetIndices.Length != (int)specCount)
            {
                throw new Exception("Unexpected field set count");
            }

            var spectypeSize = binaryReader.ReadUInt64();

            compressedBuffer = binaryReader.ReadBytes((int)spectypeSize);
            uncompressedBuffer = Decompressor.DecompressFromBuffer(compressedBuffer, workspaceSize);
            var specTypes = IntegerDecoder.DecodeIntegers(uncompressedBuffer, specCount);
            if (specTypes.Length != (int)specCount)
            {
                throw new Exception("Unexpected field set count");
            }

            var specs = new List<UsdcSpec>();
            for (var i = 0; i < (int)specCount; i++)
            {
                specs.Add(new UsdcSpec
                {
                    PathIndex = pathIndices[i],
                    FieldSetIndex = fieldSetIndices[i],
                    SpecType = specTypes[i]
                });
            }
            return specs.ToArray();
        }

        private string[] tokens;

        private int[] stringIdices;

        private UsdcField[] fields;

        private int[] fieldSetIndices;

        private UsdcSpec[] specs;

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

                for (var i = 0; i < header.Length; i++)
                {
                    Logger.LogLine($"{(byte)header[i]}");
                }

                // Read version info
                var version = ReadVersion(binaryReader);
                if (version.Major == 0 && version.Minor < 4)
                {
                    throw new Exception("Version should be at least 0.4.0");
                }

                Logger.LogLine($"version = {version.Major}.{version.Minor}.{version.Patch}");
                
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
                        stringIdices = ReadStrings(binaryReader, section.Offset, section.Size);
                    }
                    else if (section.Token.Equals("FIELDS"))
                    {
                        fields = ReadFields(binaryReader, section.Offset, section.Size);
                    }
                    else if (section.Token.Equals("FIELDSETS"))
                    {
                        fieldSetIndices = ReadFieldSets(binaryReader, section.Offset, section.Size);
                    }
                    else if (section.Token.Equals("PATHS"))
                    {
                        ReadPaths(binaryReader, section.Offset, section.Size);
                    }
                    else if (section.Token.Equals("SPECS"))
                    {
                        specs = ReadSpecs(binaryReader, section.Offset, section.Size);
                    }
                }
            }
        }
    }
}