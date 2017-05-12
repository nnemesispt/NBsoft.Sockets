namespace NBsoft.Sockets
{
    static class Compressor
    {
        internal static System.IO.FileInfo CompressLZ4(System.IO.FileInfo OriginFile)
        {


            string destiFullName = string.Format("{0}{1}.l4z", System.IO.Path.GetTempPath(), OriginFile.Name);
            using (var istream = OriginFile.OpenRead())
            using (var ostream = new System.IO.FileStream(destiFullName, System.IO.FileMode.Create))
            using (var lzStream = new LZ4.LZ4Stream(ostream, System.IO.Compression.CompressionMode.Compress))
            {
                istream.CopyTo(lzStream);
            }
            return new System.IO.FileInfo(destiFullName);
        }
        internal static System.IO.FileInfo DecompressLZ4(System.IO.FileInfo CompressedFile)
        {
            string destinFile = CompressedFile.FullName.Replace(CompressedFile.Extension, "");
            using (var istream = new System.IO.FileStream(CompressedFile.FullName, System.IO.FileMode.Open, System.IO.FileAccess.Read))
            using (var ostream = new System.IO.FileStream(destinFile, System.IO.FileMode.Create))
            using (var lzStream = new LZ4.LZ4Stream(istream, System.IO.Compression.CompressionMode.Decompress))
            {
                lzStream.CopyTo(ostream);
            }
            return new System.IO.FileInfo(destinFile);
        }

        internal static System.IO.FileInfo CompressGZip(System.IO.FileInfo OriginFile)
        {

            string destiFullName = string.Format("{0}{1}.gz", System.IO.Path.GetTempPath(), OriginFile.Name);
            using (System.IO.FileStream inFile = OriginFile.OpenRead())
            {
                // Create the compressed file.
                using (System.IO.FileStream outFile =
                            System.IO.File.Create(destiFullName))
                {
                    using (System.IO.Compression.GZipStream Compress =
                        new System.IO.Compression.GZipStream(outFile,
                        System.IO.Compression.CompressionMode.Compress))
                    {
                        // Copy the source file into 
                        // the compression stream.
                        inFile.CopyTo(Compress);
                    }
                }

            }
            return new System.IO.FileInfo(destiFullName);
        }
        internal static System.IO.FileInfo DecompressGZip(System.IO.FileInfo CompressedFile)
        {
            // Get the stream of the source file.
            using (System.IO.FileStream inFile = CompressedFile.OpenRead())
            {
                // Get original file extension, for example
                // "doc" from report.doc.gz.
                string curFile = CompressedFile.FullName;
                string origName = curFile.Remove(curFile.Length -
                        CompressedFile.Extension.Length);

                //Create the decompressed file.
                using (System.IO.FileStream outFile = System.IO.File.Create(origName))
                {
                    using (System.IO.Compression.GZipStream Decompress = new System.IO.Compression.GZipStream(inFile,
                            System.IO.Compression.CompressionMode.Decompress))
                    {
                        // Copy the decompression stream 
                        // into the output file.
                        Decompress.CopyTo(outFile);
                    }
                }
                return new System.IO.FileInfo(origName);
            }

        }
    }
}
