using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace beyondnations.web {

    /**
    * How a save file is kept in browser storage (LocalStorageSaveStorage):
    * gzipped, base64-encoded, behind a "gz:" marker. decode gives back the
    * exact bytes encode was given, so a save's checksum still holds; a value
    * without the marker is taken as the save's text as it is.
    */
    public static class SaveStorageCodec {
        public const string GzipMarker = "gz:";

        public static string encode(byte[] data) {
            using (MemoryStream compressed = new MemoryStream()) {
                using (GZipStream gzip = new GZipStream(compressed, CompressionLevel.Fastest, true)) {
                    gzip.Write(data, 0, data.Length);
                }
                return GzipMarker + Convert.ToBase64String(compressed.GetBuffer(), 0, (int) compressed.Length);
            }
        }

        public static byte[] decode(string value) {
            if (!value.StartsWith(GzipMarker, StringComparison.Ordinal)) {
                return Encoding.UTF8.GetBytes(value);
            }
            byte[] compressed = Convert.FromBase64String(value.Substring(GzipMarker.Length));
            using (MemoryStream input = new MemoryStream(compressed))
            using (GZipStream gzip = new GZipStream(input, CompressionMode.Decompress))
            using (MemoryStream output = new MemoryStream()) {
                gzip.CopyTo(output);
                return output.ToArray();
            }
        }
    }
}
