namespace UsdzSharpie
{
    public struct UsdcVersion
    {
        public byte Major;
        public byte Minor;
        public byte Patch;

        public bool Is32Bit()
        {
            return Major == 0 && Minor < 7;
        }
    }
}
