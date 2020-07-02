using K4os.Compression.LZ4;
using K4os.Compression.LZ4.Streams;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
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

            Logger.LogLine($"version = {version.Major}.{version.Minor}.{version.Patch}");

            return version;
        }

        private UsdcSection[] ReadTocSections(BinaryReader binaryReader)
        {
            var tocSections = new List<UsdcSection>();
            var tocOffset = binaryReader.ReadUInt64();

            Logger.LogLine($"toc offset = {tocOffset}");

            binaryReader.BaseStream.Position = (long)tocOffset;
            var tocCount = binaryReader.ReadUInt64();

            Logger.LogLine($"toc sections = {tocCount}");

            for (var i = (ulong)0; i < tocCount; i++)
            {
                var section = new UsdcSection
                {
                    Token = ReadString(binaryReader, 16),
                    Offset = binaryReader.ReadUInt64(),
                    Size = binaryReader.ReadUInt64()
                };

                Logger.LogLine($"section[{i}] name = {section.Token}, start = {section.Offset}, size = {section.Size}");

                tocSections.Add(section);
            }
            return tocSections.ToArray();
        }


        private string[] SplitTokenBufferIntoStrings(byte[] buffer)
        {
            var stringBuilder = new StringBuilder();
            var result = new List<string>();
            for (var i = 0; i < buffer.Length; i++)
            {
                if (buffer[i] == 0)
                {
                    var value = stringBuilder.ToString();

                    Logger.LogLine($"token[{result.Count}] = {value}");

                    result.Add(value);
                    stringBuilder.Clear();
                    continue;
                }
                stringBuilder.Append((char)buffer[i]);
            }
            return result.ToArray();
        }

        private string[] ReadTokens(BinaryReader binaryReader, ulong offset, ulong size)
        {
            Logger.LogLine($"sec.start = {offset}");

            binaryReader.BaseStream.Position = (long)offset;

            var tokenCount = binaryReader.ReadUInt64();
            var uncompressedSize = binaryReader.ReadUInt64();
            var compressedSize = binaryReader.ReadUInt64();

            Logger.LogLine($"# of tokens = {tokenCount}, uncompressedSize = {uncompressedSize}, compressedSize = {compressedSize}");

            if (compressedSize + 24 != size)
            {
                throw new Exception("Unexpected buffer size");
            }

            var compressedBuffer = ReadBytes(binaryReader, (int)compressedSize);
            var uncompressedBuffer = Decompressor.DecompressFromBuffer(compressedBuffer, uncompressedSize);

            var tokens = SplitTokenBufferIntoStrings(uncompressedBuffer);
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

            Logger.LogLine($"ReadIndices: n = {indexCount}");

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


        private UsdcField[] ReadFields(BinaryReader binaryReader, ulong offset, ulong size)
        {
            binaryReader.BaseStream.Position = (long)offset;

            var fieldCount = binaryReader.ReadUInt64();
            var fieldSize = binaryReader.ReadUInt64();

            Logger.LogLine($"fields_size = {fieldSize}, tmp.size = {fieldCount}, num_fields = {fieldCount}");
            Logger.LogLine($"num_fields = {fieldCount}");

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
                var field = new UsdcField
                {
                    Name = tokens[indices[i]],
                    Flags = BitConverter.ToUInt64(uncompressedBuffer, bufferOffset)
                };

                Logger.LogLine($"field[{i}] name = {field.Name}, value = ty: {(int)field.Type}, isArray: {field.IsArray.ToInt()}, isInlined: {field.IsInlined.ToInt()}, isCompressed: {field.IsCompressed.ToInt()}, payload: {field.Payload}");

                fields.Add(field);
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
            var workspaceSize = GetCompressedBufferSize(GetEncodedBufferSize(fieldSetCount));

            Logger.LogLine($"num_fieldsets = {fieldSetCount}, fsets_size = {fieldSetSize}, comp_buffer.size = {workspaceSize}");

            var uncompressedBuffer = Decompressor.DecompressFromBuffer(compressedBuffer, workspaceSize);
            var indices = IntegerDecoder.DecodeIntegers(uncompressedBuffer, fieldSetCount);
            if (indices.Length != (int)fieldSetCount)
            {
                throw new Exception("Unexpected field set count");
            }

            for (var i = 0; i < indices.Length; i++)
            {
                Logger.LogLine($"fieldset_index[{i}] = {(uint)indices[i]}");
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

                    Logger.LogLine($"paths[{pathIndices[thisIndex]}] is parent. name = {parentPath.full_path_name()}");

                    parentPath = UsdcPath.AbsoluteRootPath();
                    paths[pathIndices[thisIndex]] = parentPath;
                }
                else
                {
                    var tokenIndex = elementTokenIndices[thisIndex];
                    var isPrimPropertyPath = tokenIndex < 0;
                    tokenIndex = Math.Abs(tokenIndex);

                    Logger.LogLine($"tokenIndex = {tokenIndex}");

                    if (tokenIndex >= tokens.Length)
                    {
                        throw new Exception("Unexpected token index");
                    }

                    var elemToken = tokens[tokenIndex];
                    Logger.LogLine($"elemToken = {elemToken}");
                    Logger.LogLine($"[{pathIndices[thisIndex]}].append = {elemToken}");

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
                Logger.LogLine($"thisIndex = {thisIndex}, curIndex = {currentIndex}");
                
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


                    Logger.LogLine($"Hierarhy. parent[{pathIndices[parentNodeIndex]}].add_child = {pathIndices[thisIndex]}");

                    var node = new UsdcNode(parentNodeIndex, paths[pathIndices[thisIndex]]);

                    nodes[pathIndices[thisIndex]] = node;

                    var name = paths[pathIndices[thisIndex]].local_path_name();

                    Logger.LogLine($"childName = {name}");

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

                    Logger.LogLine($"parentNodeIndex = {parentNodeIndex}");
                }
                // If we had only a sibling, we just continue since the parent path is
                // unchanged and the next thing in the reader stream is the sibling's
                // header.
            } while (hasChild || hasSibling);

        }

        private void ReadPaths(BinaryReader binaryReader, ulong offset, ulong size, out UsdcPath[] paths, out UsdcNode[] nodes)
        {
            binaryReader.BaseStream.Position = (long)offset;

            var pathCount = binaryReader.ReadUInt64();

            Logger.LogLine($"numPaths : {pathCount}");

            if (pathCount != binaryReader.ReadUInt64())
            {
                throw new Exception("Unexpected path count");
            }

            var pathIndexSize = binaryReader.ReadUInt64();

            var compressedBuffer = binaryReader.ReadBytes((int)pathIndexSize);
            var workspaceSize = GetCompressedBufferSize(GetEncodedBufferSize(pathCount));

            Logger.LogLine($"comBuffer.size = {workspaceSize}");
            Logger.LogLine($"pathIndexesSize = {pathIndexSize}");

            var uncompressedBuffer = Decompressor.DecompressFromBuffer(compressedBuffer, workspaceSize);
            var pathIndices = IntegerDecoder.DecodeIntegers(uncompressedBuffer, pathCount);
            if (pathIndices.Length != (int)pathCount)
            {
                throw new Exception("Unexpected field set count");
            }

            var elementTokenIndexSize = binaryReader.ReadUInt64();
            compressedBuffer = binaryReader.ReadBytes((int)elementTokenIndexSize);
            workspaceSize = GetCompressedBufferSize(GetEncodedBufferSize(pathCount));
            uncompressedBuffer = Decompressor.DecompressFromBuffer(compressedBuffer, workspaceSize);
            var elementTokenIndices = IntegerDecoder.DecodeIntegers(uncompressedBuffer, pathCount);
            if (elementTokenIndices.Length != (int)pathCount)
            {
                throw new Exception("Unexpected field set count");
            }

            var jumpSize = binaryReader.ReadUInt64();
            compressedBuffer = binaryReader.ReadBytes((int)jumpSize);
            workspaceSize = GetCompressedBufferSize(GetEncodedBufferSize(pathCount));
            uncompressedBuffer = Decompressor.DecompressFromBuffer(compressedBuffer, workspaceSize);
            var jumpIndices = IntegerDecoder.DecodeIntegers(uncompressedBuffer, pathCount);
            if (jumpIndices.Length != (int)pathCount)
            {
                throw new Exception("Unexpected field set count");
            }

            paths = new UsdcPath[pathCount];

            BuildDecompressedPaths(pathIndices, elementTokenIndices, jumpIndices, 0, new UsdcPath(), ref paths);

            nodes = new UsdcNode[pathCount];
            BuildNodeHierarchy(pathIndices, elementTokenIndices, jumpIndices, 0, -1, ref nodes, ref paths);

            for (var i = 0; i < pathIndices.Length; i++)
            {
                Logger.LogLine($"pathIndexes[{i}] = {pathIndices[i]}");
            }

            for (var i = 0; i < elementTokenIndices.Length; i++)
            {
                Logger.LogLine($"elementTokenIndexes {elementTokenIndices[i]}");
            }

            for (var i = 0; i < jumpIndices.Length; i++)
            {
                Logger.LogLine($"jumps {jumpIndices[i]}");
            }

            Logger.LogLine($"# of paths {pathCount}");
            for (var i = 0; i < paths.Length; i++)
            {
                Logger.LogLine($"path[{i}] = {paths[i].full_path_name()}");
            }
        }

        private UsdcSpec[] ReadSpecs(BinaryReader binaryReader, ulong offset, ulong size)
        {
            binaryReader.BaseStream.Position = (long)offset;

            var specCount = binaryReader.ReadUInt64(); 

            Logger.LogLine($"num_specs {specCount}");

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

            for (var i = 0; i < fieldSetIndices.Length; i++)
            {
                Logger.LogLine($"specs[{i}].fieldset_index = {fieldSetIndices[i]}");
            }

            if (fieldSetIndices.Length != (int)specCount)
            {
                throw new Exception("Unexpected field set count");
            }

            var spectypeSize = binaryReader.ReadUInt64();

            compressedBuffer = binaryReader.ReadBytes((int)spectypeSize);
            uncompressedBuffer = Decompressor.DecompressFromBuffer(compressedBuffer, workspaceSize);
            var specTypes = IntegerDecoder.DecodeIntegers(uncompressedBuffer, specCount);

            for (var i = 0; i < specTypes.Length; i++)
            {
                Logger.LogLine($"spectype = {specTypes[i]}");
            }

            if (specTypes.Length != (int)specCount)
            {
                throw new Exception("Unexpected field set count");
            }

            var specs = new List<UsdcSpec>();
            for (var i = 0; i < (int)specCount; i++)
            {
                var spec = new UsdcSpec
                {
                    PathIndex = pathIndices[i],
                    FieldSetIndex = fieldSetIndices[i],
                    SpecType = ((ulong)specTypes[i]).ToEnum<UsdcField.SpecType>()
                };

                Logger.LogLine($"spec[{i}].pathIndex  = {spec.PathIndex}, fieldset_index = {spec.FieldSetIndex}, spec_type = {(int)spec.SpecType}");
                if (i >= fieldSetIndices.Length)
                {
                    Logger.LogLine($"spec[{i}] string_repr = #INVALID spec index#");
                }
                else
                {
                    Logger.LogLine($"spec[{i}] string_repr = [Spec] path: {paths[spec.PathIndex].full_path_name()}, fieldset id: {spec.FieldSetIndex}, spec_type: {spec.SpecType}");
                }
                specs.Add(spec);
            }



            return specs.ToArray();
        }

        private UsdcVersion version;

        private string[] tokens;

        private int[] stringIdices;

        private UsdcField[] fields;

        private int[] fieldSetIndices;

        private UsdcSpec[] specs;

        private UsdcPath[] paths;

        private UsdcNode[] nodes;


        private bool SupportsArray(UsdcField.ValueTypeId valueType)
        {
            switch (valueType)
            {
                case UsdcField.ValueTypeId.ValueTypeBool:
                case UsdcField.ValueTypeId.ValueTypeUChar:
                case UsdcField.ValueTypeId.ValueTypeInt:
                case UsdcField.ValueTypeId.ValueTypeUInt:
                case UsdcField.ValueTypeId.ValueTypeInt64:
                case UsdcField.ValueTypeId.ValueTypeUInt64:
                case UsdcField.ValueTypeId.ValueTypeHalf:
                case UsdcField.ValueTypeId.ValueTypeFloat:
                case UsdcField.ValueTypeId.ValueTypeDouble:
                case UsdcField.ValueTypeId.ValueTypeString:
                case UsdcField.ValueTypeId.ValueTypeToken:
                case UsdcField.ValueTypeId.ValueTypeAssetPath:
                case UsdcField.ValueTypeId.ValueTypeQuatd:
                case UsdcField.ValueTypeId.ValueTypeQuatf:
                case UsdcField.ValueTypeId.ValueTypeQuath:
                case UsdcField.ValueTypeId.ValueTypeVec2d:
                case UsdcField.ValueTypeId.ValueTypeVec2f:
                case UsdcField.ValueTypeId.ValueTypeVec2h:
                case UsdcField.ValueTypeId.ValueTypeVec2i:
                case UsdcField.ValueTypeId.ValueTypeVec3d:
                case UsdcField.ValueTypeId.ValueTypeVec3f:
                case UsdcField.ValueTypeId.ValueTypeVec3h:
                case UsdcField.ValueTypeId.ValueTypeVec3i:
                case UsdcField.ValueTypeId.ValueTypeVec4d:
                case UsdcField.ValueTypeId.ValueTypeVec4f:
                case UsdcField.ValueTypeId.ValueTypeVec4h:
                case UsdcField.ValueTypeId.ValueTypeVec4i:
                case UsdcField.ValueTypeId.ValueTypeMatrix2d:
                case UsdcField.ValueTypeId.ValueTypeMatrix3d:
                case UsdcField.ValueTypeId.ValueTypeMatrix4d:
                case UsdcField.ValueTypeId.ValueTypeTimeCode:
                    return true;
                default:
                    return false;
            }
        }


        private object UnpackInlined(UsdcField field)
        {
            Logger.LogLine($"d = {field.Payload}");
            Logger.LogLine($"ty.id = {(ulong)field.Type}");

            if (field.Type == UsdcField.ValueTypeId.ValueTypeBool)
            {
                if (field.IsCompressed)
                {
                    throw new Exception($"Compression is not supported for type {field.Type}");
                }

                if (field.IsArray)
                {
                    throw new Exception($"Array is not accepted for type {field.Type}");
                }

                Logger.LogLine($"Bool: {field.Payload}");

                //value->SetBool(d ? true : false);

                return null;
            }
            else if (field.Type == UsdcField.ValueTypeId.ValueTypeAssetPath)
            {
                if (field.IsCompressed)
                {
                    throw new Exception($"Compression is not supported for type {field.Type}");
                }

                if (field.IsArray)
                {
                    throw new Exception($"Array is not accepted for type {field.Type}");
                }

                var token = tokens[field.Payload];

                //value->SetAssetPath(str);

                return null;
            }
            else if (field.Type == UsdcField.ValueTypeId.ValueTypeSpecifier)
            {
                if (field.IsCompressed)
                {
                    throw new Exception($"Compression is not supported for type {field.Type}");
                }

                if (field.IsArray)
                {
                    throw new Exception($"Array is not accepted for type {field.Type}");
                }

                var specifier = field.Payload.ToEnum<UsdcField.Specifier>();

                Logger.LogLine($"Specifier: {specifier}");

                //value->SetSpecifier(d);

                return null;
            }
            else if (field.Type == UsdcField.ValueTypeId.ValueTypePermission)
            {
                if (field.IsCompressed)
                {
                    throw new Exception($"Compression is not supported for type {field.Type}");
                }

                if (field.IsArray)
                {
                    throw new Exception($"Array is not accepted for type {field.Type}");
                }

                var permission = field.Payload.ToEnum<UsdcField.Permission>();

                Logger.LogLine($"Permission: {permission}");

                //value->SetPermission(d);

                return null;
            }
            else if (field.Type == UsdcField.ValueTypeId.ValueTypeVariability)
            {
                if (field.IsCompressed)
                {
                    throw new Exception($"Compression is not supported for type {field.Type}");
                }

                if (field.IsArray)
                {
                    throw new Exception($"Array is not accepted for type {field.Type}");
                }

                var variability = field.Payload.ToEnum<UsdcField.Variability>();

                Logger.LogLine($"Variability: {variability}");

                //value->SetVariability(d);

                return null;
            }
            else if (field.Type == UsdcField.ValueTypeId.ValueTypeToken)
            {
                if (field.IsCompressed)
                {
                    throw new Exception($"Compression is not supported for type {field.Type}");
                }

                if (field.IsArray)
                {
                    throw new Exception($"Array is not accepted for type {field.Type}");
                }

                var token = tokens[field.Payload];

                Logger.LogLine($"value.token = {token}");

                //value->SetToken(token);

                return null;
            }
            else if (field.Type == UsdcField.ValueTypeId.ValueTypeString)
            {
                if (field.IsCompressed)
                {
                    throw new Exception($"Compression is not supported for type {field.Type}");
                }

                if (field.IsArray)
                {
                    throw new Exception($"Array is not accepted for type {field.Type}");
                }

                var stringValue = tokens[stringIdices[field.Payload]];

                Logger.LogLine($"value.string = {stringValue}");

                //value->SetString(stringValue);

                return null;
            }
            else if (field.Type == UsdcField.ValueTypeId.ValueTypeFloat)
            {
                if (field.IsCompressed)
                {
                    throw new Exception($"Compression is not supported for type {field.Type}");
                }

                if (field.IsArray)
                {
                    throw new Exception($"Array is not accepted for type {field.Type}");
                }

                var floatValue = field.Payload.UnpackFloat32();

                Logger.LogLine($"value.float = {floatValue.ToCoutFormat()}");

                //value->SetFloat(floatValue);

                return null;
            }
            else if (field.Type == UsdcField.ValueTypeId.ValueTypeDouble)
            {
                if (field.IsCompressed)
                {
                    throw new Exception($"Compression is not supported for type {field.Type}");
                }

                if (field.IsArray)
                {
                    throw new Exception($"Array is not accepted for type {field.Type}");
                }

                var doubleValue = (double)field.Payload.UnpackFloat32();

                Logger.LogLine($"value.double = {doubleValue.ToCoutFormat()}");

                //value->SetDouble(doubleValue);

                return null;
            }
            else if (field.Type == UsdcField.ValueTypeId.ValueTypeVec3i)
            {
                if (field.IsCompressed)
                {
                    throw new Exception($"Compression is not supported for type {field.Type}");
                }

                if (field.IsArray)
                {
                    throw new Exception($"Array is not accepted for type {field.Type}");
                }

                var x = (int)(field.Payload & 0xff);
                var y = (int)((field.Payload >> 8) & 0xff);
                var z = (int)((field.Payload >> 16) & 0xff);

                Logger.LogLine($"value.vec3i = {x}, {y}, {z}");

                //value->SetVec3i(x, y, z);

                return null;
            }
            else if (field.Type == UsdcField.ValueTypeId.ValueTypeVec3f)
            {
                if (field.IsCompressed)
                {
                    throw new Exception($"Compression is not supported for type {field.Type}");
                }

                if (field.IsArray)
                {
                    throw new Exception($"Array is not accepted for type {field.Type}");
                }

                var x = (float)(field.Payload & 0xff);
                var y = (float)((field.Payload >> 8) & 0xff);
                var z = (float)((field.Payload >> 16) & 0xff);

                Logger.LogLine($"value.vec3f = {x.ToCoutFormat()}, {y.ToCoutFormat()}, {z.ToCoutFormat()}");

                //value->SetVec3f(x, y, z);

                return null;
            }
            else if (field.Type == UsdcField.ValueTypeId.ValueTypeMatrix2d)
            {
                if (field.IsCompressed)
                {
                    throw new Exception($"Compression is not supported for type {field.Type}");
                }

                if (field.IsArray)
                {
                    throw new Exception($"Array is not accepted for type {field.Type}");
                }

                var m00 = (double)(field.Payload & 0xff);
                var m11 = (double)((field.Payload >> 8) & 0xff);

                Logger.LogLine($"value.matrix(diag) = {m00.ToCoutFormat()}, {m11.ToCoutFormat()}");

                //value->SetMatrix2d(m00, m11);

                return null;
            }
            else if (field.Type == UsdcField.ValueTypeId.ValueTypeMatrix3d)
            {
                if (field.IsCompressed)
                {
                    throw new Exception($"Compression is not supported for type {field.Type}");
                }

                if (field.IsArray)
                {
                    throw new Exception($"Array is not accepted for type {field.Type}");
                }

                var m00 = (double)(field.Payload & 0xff);
                var m11 = (double)((field.Payload >> 8) & 0xff);
                var m22 = (double)((field.Payload >> 16) & 0xff);

                Logger.LogLine($"value.matrix(diag) = {m00.ToCoutFormat()}, {m11.ToCoutFormat()}, {m22.ToCoutFormat()}");

                //value->SetMatrix3d(m00, m11, m22);

                return null;
            }
            else if (field.Type == UsdcField.ValueTypeId.ValueTypeMatrix4d)
            {
                if (field.IsCompressed)
                {
                    throw new Exception($"Compression is not supported for type {field.Type}");
                }

                if (field.IsArray)
                {
                    throw new Exception($"Array is not accepted for type {field.Type}");
                }

                var m00 = (double)(field.Payload & 0xff);
                var m11 = (double)((field.Payload >> 8) & 0xff);
                var m22 = (double)((field.Payload >> 16) & 0xff);
                var m33 = (double)((field.Payload >> 24) & 0xff);

                Logger.LogLine($"value.matrix(diag) = {m00.ToCoutFormat()}, {m11.ToCoutFormat()}, {m22.ToCoutFormat()}, {m33.ToCoutFormat()}");

                //value->SetMatrix3d(m00, m11, m22);

                return null;
            }

            return null;
        }

        private int[] ReadIntArray(BinaryReader binaryReader, bool compressed)
        {
            var kMinCompressedArraySize = 16;
            var result = new List<int>();

            if (!compressed)
            {
                var length = version.Is32Bit() ? binaryReader.ReadInt32() : (int)binaryReader.ReadInt64();
                for (var i = 0; i < length; i++)
                {
                    result.Add(version.Is32Bit() ? binaryReader.ReadInt32() : (int)binaryReader.ReadInt64());                
                }
                return result.ToArray();
            }

            var elemCount = version.Is32Bit() ? binaryReader.ReadInt32() : (int)binaryReader.ReadInt64();
            Logger.LogLine($"array.len = {elemCount}");

            if (elemCount < kMinCompressedArraySize)
            {
                for (var i = 0; i < elemCount; i++)
                {
                    result.Add(version.Is32Bit() ? binaryReader.ReadInt32() : (int)binaryReader.ReadInt64());
                }

                return result.ToArray();
            }

            var compSize = binaryReader.ReadUInt64();
            var compressedBuffer = binaryReader.ReadBytes((int)compSize);

            var workspaceSize = GetCompressedBufferSize(GetEncodedBufferSize((ulong)elemCount));
            var uncompressedBuffer = Decompressor.DecompressFromBuffer(compressedBuffer, workspaceSize);
            var intValues = IntegerDecoder.DecodeIntegers(uncompressedBuffer, (ulong)elemCount);
            if (intValues.Length != (int)elemCount)
            {
                throw new Exception("Unexpected count");
            }

            return intValues;
        }

        private short[] ReadHalfArray(BinaryReader binaryReader, bool compressed)
        {
            var kMinCompressedArraySize = 16;
            var result = new List<short>();

            if (!compressed)
            {
                var length = version.Is32Bit() ? binaryReader.ReadInt32() : (int)binaryReader.ReadInt64();
                for (var i = 0; i < length; i++)
                {
                    result.Add(binaryReader.ReadInt16());
                }
                return result.ToArray();
            }

            var elemCount = version.Is32Bit() ? binaryReader.ReadInt32() : (int)binaryReader.ReadInt64();
            Logger.LogLine($"array.len = {elemCount}");

            if (elemCount < kMinCompressedArraySize)
            {
                for (var i = 0; i < elemCount; i++)
                {
                    result.Add(binaryReader.ReadInt16());
                }

                return result.ToArray();
            }

            var halfResult = new List<short>();
            var code = (char)binaryReader.ReadByte();
            if (code == 'i')
            {

                var compSize = binaryReader.ReadUInt64();
                var compressedBuffer = binaryReader.ReadBytes((int)compSize);

                var workspaceSize = GetCompressedBufferSize(GetEncodedBufferSize((ulong)elemCount));
                var uncompressedBuffer = Decompressor.DecompressFromBuffer(compressedBuffer, workspaceSize);
                var intValues = IntegerDecoder.DecodeIntegers(uncompressedBuffer, (ulong)elemCount);
                if (intValues.Length != (int)elemCount)
                {
                    throw new Exception("Unexpected count");
                }

                //for (size_t i = 0; i < length; i++)
                //{
                //    float f = float(ints[i]);
                //    float16 h = float_to_half_full(f);
                //    (*d)[i] = h.u;
                //}

                for (var i = 0; i < intValues.Length; i++)
                {
                    halfResult.Add((short)intValues[i]);
                }

            }
            else if (code == 't')
            {
                var lutSize = binaryReader.ReadUInt32();
                var lutResult = new List<short>();
                for (var i = 0; i < lutSize; i++)
                {
                    lutResult.Add(binaryReader.ReadInt16());
                }

                var compSize = binaryReader.ReadUInt64();
                var compressedBuffer = binaryReader.ReadBytes((int)compSize);

                var workspaceSize = GetCompressedBufferSize(GetEncodedBufferSize((ulong)elemCount));
                var uncompressedBuffer = Decompressor.DecompressFromBuffer(compressedBuffer, workspaceSize);
                var intValues = IntegerDecoder.DecodeIntegers(uncompressedBuffer, (ulong)elemCount);
                if (intValues.Length != (int)elemCount)
                {
                    throw new Exception("Unexpected count");
                }
                for (var i = 0; i < intValues.Length; i++)
                {
                    halfResult.Add(lutResult[intValues[i]]);
                }
            }

            return halfResult.ToArray();
        }

        private float[] ReadFloatArray(BinaryReader binaryReader, bool compressed)
        {
            var kMinCompressedArraySize = 16;
            var result = new List<float>();

            if (!compressed)
            {
                var length = version.Is32Bit() ? binaryReader.ReadInt32() : (int)binaryReader.ReadInt64();
                for (var i = 0; i < length; i++)
                {
                    result.Add(binaryReader.ReadSingle());
                }
                return result.ToArray();
            }

            var elemCount = version.Is32Bit() ? binaryReader.ReadInt32() : (int)binaryReader.ReadInt64();
            Logger.LogLine($"array.len = {elemCount}");

            if (elemCount < kMinCompressedArraySize)
            {
                for (var i = 0; i < elemCount; i++)
                {
                    result.Add(binaryReader.ReadSingle());
                }

                return result.ToArray();
            }

            var floatResult = new List<float>();
            var code = (char)binaryReader.ReadByte();
            if (code == 'i')
            {

                var compSize = binaryReader.ReadUInt64();
                var compressedBuffer = binaryReader.ReadBytes((int)compSize);

                var workspaceSize = GetCompressedBufferSize(GetEncodedBufferSize((ulong)elemCount));
                var uncompressedBuffer = Decompressor.DecompressFromBuffer(compressedBuffer, workspaceSize);
                var intValues = IntegerDecoder.DecodeIntegers(uncompressedBuffer, (ulong)elemCount);
                if (intValues.Length != (int)elemCount)
                {
                    throw new Exception("Unexpected count");
                }

                //for (size_t i = 0; i < length; i++)
                //{
                //    float f = float(ints[i]);
                //    float16 h = float_to_half_full(f);
                //    (*d)[i] = h.u;
                //}

                for (var i = 0; i < intValues.Length; i++)
                {
                    floatResult.Add((float)intValues[i]);
                }

            }
            else if (code == 't')
            {
                var lutSize = binaryReader.ReadUInt32();
                var lutResult = new List<float>();
                for (var i = 0; i < lutSize; i++)
                {
                    lutResult.Add(binaryReader.ReadSingle());
                }

                var compSize = binaryReader.ReadUInt64();
                var compressedBuffer = binaryReader.ReadBytes((int)compSize);

                var workspaceSize = GetCompressedBufferSize(GetEncodedBufferSize((ulong)elemCount));
                var uncompressedBuffer = Decompressor.DecompressFromBuffer(compressedBuffer, workspaceSize);
                var intValues = IntegerDecoder.DecodeIntegers(uncompressedBuffer, (ulong)elemCount);
                if (intValues.Length != (int)elemCount)
                {
                    throw new Exception("Unexpected count");
                }
                for (var i = 0; i < intValues.Length; i++)
                {
                    floatResult.Add(lutResult[intValues[i]]);
                }
            }

            return floatResult.ToArray();
        }

        private double[] ReadDoubleArray(BinaryReader binaryReader, bool compressed)
        {
            var kMinCompressedArraySize = 16;
            var result = new List<double>();

            if (!compressed)
            {
                var length = version.Is32Bit() ? binaryReader.ReadInt32() : (int)binaryReader.ReadInt64();
                for (var i = 0; i < length; i++)
                {
                    result.Add(binaryReader.ReadDouble());
                }
                return result.ToArray();
            }

            var elemCount = version.Is32Bit() ? binaryReader.ReadInt32() : (int)binaryReader.ReadInt64();
            Logger.LogLine($"array.len = {elemCount}");

            if (elemCount < kMinCompressedArraySize)
            {
                for (var i = 0; i < elemCount; i++)
                {
                    result.Add(binaryReader.ReadDouble());
                }

                return result.ToArray();
            }

            var doubleResult = new List<double>();
            var code = (char)binaryReader.ReadByte();
            if (code == 'i')
            {

                var compSize = binaryReader.ReadUInt64();
                var compressedBuffer = binaryReader.ReadBytes((int)compSize);

                var workspaceSize = GetCompressedBufferSize(GetEncodedBufferSize((ulong)elemCount));
                var uncompressedBuffer = Decompressor.DecompressFromBuffer(compressedBuffer, workspaceSize);
                var intValues = IntegerDecoder.DecodeIntegers(uncompressedBuffer, (ulong)elemCount);
                if (intValues.Length != (int)elemCount)
                {
                    throw new Exception("Unexpected count");
                }

                //for (size_t i = 0; i < length; i++)
                //{
                //    float f = float(ints[i]);
                //    float16 h = float_to_half_full(f);
                //    (*d)[i] = h.u;
                //}

                for (var i = 0; i < intValues.Length; i++)
                {
                    doubleResult.Add((float)intValues[i]);
                }

            }
            else if (code == 't')
            {
                var lutSize = binaryReader.ReadUInt32();
                var lutResult = new List<double>();
                for (var i = 0; i < lutSize; i++)
                {
                    lutResult.Add(binaryReader.ReadInt16());
                }

                var compSize = binaryReader.ReadUInt64();
                var compressedBuffer = binaryReader.ReadBytes((int)compSize);

                var workspaceSize = GetCompressedBufferSize(GetEncodedBufferSize((ulong)elemCount));
                var uncompressedBuffer = Decompressor.DecompressFromBuffer(compressedBuffer, workspaceSize);
                var intValues = IntegerDecoder.DecodeIntegers(uncompressedBuffer, (ulong)elemCount);
                if (intValues.Length != (int)elemCount)
                {
                    throw new Exception("Unexpected count");
                }
                for (var i = 0; i < intValues.Length; i++)
                {
                    doubleResult.Add(lutResult[intValues[i]]);
                }
            }

            return doubleResult.ToArray();
        }

        private object UnpackNotInlined(BinaryReader binaryReader, UsdcField field)
        {
            var offset = field.Payload;
            binaryReader.BaseStream.Position = (long)offset;

            if (field.Type == UsdcField.ValueTypeId.ValueTypeToken)
            {
                if (field.IsCompressed)
                {
                    throw new Exception($"Compression is not supported for type {field.Type}");
                }

                if (!field.IsArray)
                {
                    throw new Exception($"Non array is not accepted for type {field.Type}");
                }

                var result = new List<string>();
                var count = binaryReader.ReadUInt64();
                for (var i = 0; i < (int)count; i++)
                {
                    var index = binaryReader.ReadInt32();
                    var token = tokens[index];

                    Logger.LogLine($"Token[{i}] = {token} ({index})");

                    result.Add(token);
                }

                //value->SetTokenArray(tokens);

                return null;
            }
            else if (field.Type == UsdcField.ValueTypeId.ValueTypeInt)
            {
                if (!field.IsArray)
                {
                    throw new Exception($"Non array is not accepted for type {field.Type}");
                }

                var values = ReadIntArray(binaryReader, field.IsCompressed);
                for (var i = 0; i < values.Length; i++)
                {
                    Logger.LogLine($"Int[{i}] = {values[i]}");
                }

                //if (rep.IsArray())
                //{
                //    value->SetIntArray(v.data(), v.size());
                //}
                //else
                //{
                //    value->SetInt(v[0]);
                //}

                return null;
            }
            else if (field.Type == UsdcField.ValueTypeId.ValueTypeVec2f)
            {
                if (field.IsCompressed)
                {
                    throw new Exception($"Compression is not supported for type {field.Type}");
                }

                if (field.IsArray)
                {
                    var count = binaryReader.ReadUInt64();
                    for (var i = 0; i < (int)count; i++)
                    {
                        var x = binaryReader.ReadSingle();
                        var y = binaryReader.ReadSingle();
                        Logger.LogLine($"Vec2f[{i}] = {x}, {y}");
                    }

                    //value->SetVec2fArray(v.data(), v.size());
                }
                else
                {
                    var x = binaryReader.ReadSingle();
                    var y = binaryReader.ReadSingle();
                    Logger.LogLine($"Vec2f = {x}, {y}");

                    //value->SetVec2f(v);
                }

                return null;
            }
            else if (field.Type == UsdcField.ValueTypeId.ValueTypeVec3f)
            {
                if (field.IsCompressed)
                {
                    throw new Exception($"Compression is not supported for type {field.Type}");
                }

                if (field.IsArray)
                {
                    var count = binaryReader.ReadUInt64();
                    for (var i = 0; i < (int)count; i++)
                    {
                        var x = binaryReader.ReadSingle();
                        var y = binaryReader.ReadSingle();
                        var z = binaryReader.ReadSingle();
                        Logger.LogLine($"Vec3f[{i}] = {x.ToCoutFormat()}, {y.ToCoutFormat()}, {z.ToCoutFormat()}");
                    }

                    //value->SetVec2fArray(v.data(), v.size());
                }
                else
                {
                    var x = binaryReader.ReadSingle();
                    var y = binaryReader.ReadSingle();
                    var z = binaryReader.ReadSingle();
                    Logger.LogLine($"Vec3f = {x.ToCoutFormat()}, {y.ToCoutFormat()}, {z.ToCoutFormat()}");

                    //value->SetVec3f(v);
                }

                return null;
            }
            else if (field.Type == UsdcField.ValueTypeId.ValueTypeVec4f)
            {
                if (field.IsCompressed)
                {
                    throw new Exception($"Compression is not supported for type {field.Type}");
                }

                if (field.IsArray)
                {
                    var count = binaryReader.ReadUInt64();
                    for (var i = 0; i < (int)count; i++)
                    {
                        var x = binaryReader.ReadSingle();
                        var y = binaryReader.ReadSingle();
                        var z = binaryReader.ReadSingle();
                        var w = binaryReader.ReadSingle();
                        Logger.LogLine($"Vec4f[{i}] = {x.ToCoutFormat()}, {y.ToCoutFormat()}, {z.ToCoutFormat()}, {w.ToCoutFormat()}");
                    }

                    //value->SetVec2fArray(v.data(), v.size());
                }
                else
                {
                    var x = binaryReader.ReadSingle();
                    var y = binaryReader.ReadSingle();
                    var z = binaryReader.ReadSingle();
                    var w = binaryReader.ReadSingle();
                    Logger.LogLine($"Vec3f = {x.ToCoutFormat()}, {y.ToCoutFormat()}, {z.ToCoutFormat()}, {w.ToCoutFormat()}");

                    //value->SetVec4f(v);
                }

                return null;
            }
            else if (field.Type == UsdcField.ValueTypeId.ValueTypeTokenVector)
            {
                if (field.IsCompressed)
                {
                    throw new Exception($"Compression is not supported for type {field.Type}");
                }

                var count = binaryReader.ReadUInt64();
                Logger.LogLine($"n = {count}");

                var indices = new List<int>();
                for (var i = 0; i < (int)count; i++)
                {
                    var index = binaryReader.ReadInt32();
                    indices.Add(index);
                    Logger.LogLine($"tokenIndex[{i}] = {index}");
                }

                var tokenResult = new List<string>();
                for (var i = 0; i < indices.Count; i++)
                {
                    var tokenIndex = indices[i];
                    var token = tokens[tokenIndex];
                    tokenResult.Add(token);
                    Logger.LogLine($"tokenVector[{i}] = {token}, ({tokenIndex})");
                }

                //value->SetTokenArray(tokens);

                return null;
            }
            else if (field.Type == UsdcField.ValueTypeId.ValueTypeHalf)
            {

                if (!field.IsArray)
                {
                    throw new Exception($"Non array is not accepted for type {field.Type}");
                }

                var values = ReadHalfArray(binaryReader, field.IsCompressed);
                for (var i = 0; i < values.Length; i++)
                {
                    Logger.LogLine($"Half[{i}] = {values[i]}");
                }

                return null;
            }
            else if (field.Type == UsdcField.ValueTypeId.ValueTypeFloat)
            {

                if (!field.IsArray)
                {
                    throw new Exception($"Non array is not accepted for type {field.Type}");
                }

                var values = ReadFloatArray(binaryReader, field.IsCompressed);
                for (var i = 0; i < values.Length; i++)
                {
                    Logger.LogLine($"Float[{i}] = {values[i].ToCoutFormat()}");
                }

                return null;
            }
            else if (field.Type == UsdcField.ValueTypeId.ValueTypeDouble)
            {

                if (!field.IsArray)
                {
                    throw new Exception($"Non array is not accepted for type {field.Type}");
                }

                var values = ReadDoubleArray(binaryReader, field.IsCompressed);
                for (var i = 0; i < values.Length; i++)
                {
                    Logger.LogLine($"Double[{i}] = {values[i].ToCoutFormat()}");
                }

                return null;
            }
            else if (field.Type == UsdcField.ValueTypeId.ValueTypeVec3i)
            {

                //TODO

                return null;
            }
            else if (field.Type == UsdcField.ValueTypeId.ValueTypeVec3f)
            {

                //TODO

                return null;
            }
            else if (field.Type == UsdcField.ValueTypeId.ValueTypeVec3d)
            {

                //TODO

                return null;
            }
            else if (field.Type == UsdcField.ValueTypeId.ValueTypeVec3h)
            {

                //TODO

                return null;
            }
            else if (field.Type == UsdcField.ValueTypeId.ValueTypeMatrix4d)
            {

                //TODO

                return null;
            }
            else if (field.Type == UsdcField.ValueTypeId.ValueTypeDictionary)
            {

                //TODO

                return null;
            }
            else if (field.Type == UsdcField.ValueTypeId.ValueTypePathListOp)
            {

                //TODO

                return null;
            }

            return null;
        }

        
        private object UnpackField(BinaryReader binaryReader, UsdcField field)
        {
            Logger.LogLine($"type_id = {(ulong)field.Type}");
            Logger.LogLine($"type_id = {(ulong)field.Type}");
            Logger.LogLine($"ValueType: {field.Type.ToString().Substring(9)}({(ulong)field.Type}), supports_array = {SupportsArray(field.Type).ToInt()}");

            return field.IsInlined ? UnpackInlined(field) :UnpackNotInlined(binaryReader, field);                    
        }


        public class FieldValuePair
        {
            public string Name { get; set; }
            public object Value { get; set; }
        }

        public class LiveFieldSet
        {
            public int Index { get; set; }

            public List<FieldValuePair> FieldValuePairs { get; set; } = new List<FieldValuePair>();
        }


        private void BuildLiveFieldSets(BinaryReader binaryReader)
        {
            var liveFieldSets = new List<LiveFieldSet>();

            var start = 0;
            for (var i = 0; i < fieldSetIndices.Length; i ++)
            {                
                if (fieldSetIndices[i] < 0)
                {
                    var count = i - start;

                    Logger.LogLine($"range size = {count}");

                    var liveFieldSet = new LiveFieldSet
                    {
                        Index = start
                    };

                    for (var j = start; j < start + count; j++)
                    {
                        Logger.LogLine($"fieldIndex = {fieldSetIndices[j]}");

                        var field = fields[fieldSetIndices[j]];
                        var first = field.Name;
                        var second = UnpackField(binaryReader, field);

                        var fieldValuePair = new FieldValuePair
                        {
                            Name = first,
                            Value = second
                        };
                        liveFieldSet.FieldValuePairs.Add(fieldValuePair);
                    }

                    liveFieldSets.Add(liveFieldSet);


                    start = i + 1;
          
                }
            }

            Logger.LogLine($"# of live fieldsets = {liveFieldSets.Count}");

            var sum = 0;
            for (var i = 0; i < liveFieldSets.Count; i++)
            {
                Logger.LogLine($"livefieldsets[{liveFieldSets[i]}].count = {liveFieldSets[i].FieldValuePairs.Count}");

                sum += liveFieldSets[i].FieldValuePairs.Count;
                for (var j = 0; j < liveFieldSets[i].FieldValuePairs.Count; j++)
                {
                    Logger.LogLine($"[{i}] name = {liveFieldSets[i].FieldValuePairs[j].Name}");
                }
            }

            Logger.LogLine($"Total fields used = {sum}");

            Logger.LogLine($"num_paths: {paths.Length}");

            for (var i = 0; i < paths.Length; i++)
            {
                Logger.LogLine($"path[{i}].name = {paths[i].full_path_name()}");
            }
        }

        public class Scene
        {
            //TODO:
        }

        public void ReconstructSceneRecursively(int parent, int level, ref Dictionary<int, int> pathToSpecLookup, ref Scene scene)
        {
            //TODO:
        }

        public void ReconstructScene()
        {
            Logger.LogLine($"reconstruct scene:");

            var pathToSpecLookup = new Dictionary<int, int>();
            for (var i = 0; i < specs.Length; i++)
            {
                pathToSpecLookup.Add(specs[i].PathIndex, i);
            }

            var scene = new Scene();
            ReconstructSceneRecursively(0, 0, ref pathToSpecLookup, ref scene);
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
                version = ReadVersion(binaryReader);
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
                        ReadPaths(binaryReader, section.Offset, section.Size, out paths, out nodes);
                    }
                    else if (section.Token.Equals("SPECS"))
                    {
                        specs = ReadSpecs(binaryReader, section.Offset, section.Size);
                    }
                }

                BuildLiveFieldSets(binaryReader);

                ReconstructScene();
            }
        }
    }
}