using K4os.Compression.LZ4;
using System;

namespace UsdzSharpie.Compression
{
    public static class Decompressor
    {
        private static void ProcessChunk(ref ChunkInfo chunkInfo)
        {
            var currentChunkSize = chunkInfo.ChunkSize;

            if (chunkInfo.Count != 0)
            {
                currentChunkSize = BitConverter.ToInt32(chunkInfo.CompressedBuffer, chunkInfo.Offset);
                chunkInfo.Offset += sizeof(int);
            }

            var decompressedSize = LZ4Codec.Decode(chunkInfo.CompressedBuffer, chunkInfo.Offset, currentChunkSize, chunkInfo.UncompressedBuffer, chunkInfo.TotalDecompressed, chunkInfo.UncompressedBuffer.Length);
            if (decompressedSize < 0)
            {
                throw new Exception("Unexpected decompressed chunk size");
            }

            chunkInfo.Offset += currentChunkSize;
            chunkInfo.TotalDecompressed += decompressedSize;
        }

        public static byte[] DecompressFromBuffer(byte[] compressedBuffer, ulong workspaceSize)
        {
            var workspaceBuffer = new byte[workspaceSize];

            var chunkInfo = new ChunkInfo(compressedBuffer[0], compressedBuffer.Length - 1, compressedBuffer, workspaceBuffer, 1);
            if (chunkInfo.Count == 0)
            {
                ProcessChunk(ref chunkInfo);
            }
            else
            {
                for (var i = 0; i < chunkInfo.Count; i++)
                {
                    ProcessChunk(ref chunkInfo);
                }
            }

            if (chunkInfo.TotalDecompressed == (int)workspaceSize)
            {
                return workspaceBuffer;
            }

            var result = new byte[chunkInfo.TotalDecompressed];
            Array.Copy(workspaceBuffer, result, chunkInfo.TotalDecompressed);
            return result;
        }
    }
}
