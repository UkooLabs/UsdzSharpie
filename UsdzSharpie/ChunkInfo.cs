namespace UsdzSharpie
{
    public struct ChunkInfo
    {
        public int Count { get; }

        public int ChunkSize { get; }

        public byte[] CompressedBuffer { get; }

        public byte[] UncompressedBuffer { get; }

        public int Offset { get; set; }

        public int TotalDecompressed { get; set; }

        public ChunkInfo(int count, int chunkSize, byte[] compressedBuffer, byte[] uncompressedBuffer, int offset)
        {
            Count = count;
            ChunkSize = chunkSize;
            CompressedBuffer = compressedBuffer;
            UncompressedBuffer = uncompressedBuffer;
            Offset = offset;
            TotalDecompressed = 0;
        }
    }
}