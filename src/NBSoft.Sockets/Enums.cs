namespace NBsoft.Sockets
{
    public enum CompressionType : byte
    {
        NoCompression = 0x00,
        GZCompression = 0x01,
        LZ4Compression = 0x02
    }
}
