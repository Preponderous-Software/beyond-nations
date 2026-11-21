namespace beyondnations {

    /**
    * CRC-32 (IEEE 802.3, the zip and PNG one), the checksum on every save.
    * Written out rather than taken from a package because Core takes no
    * packages, and from System.Security.Cryptography because the browser
    * build should not lean on a hash provider. It is there to catch a
    * truncated or bit-flipped file, not tampering.
    */
    public static class Crc32 {
        private static readonly uint[] table = buildTable();

        private static uint[] buildTable() {
            uint[] result = new uint[256];
            for (uint i = 0; i < 256; i++) {
                uint c = i;
                for (int k = 0; k < 8; k++) {
                    c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
                }
                result[i] = c;
            }
            return result;
        }

        public static uint compute(byte[] data, int offset, int count) {
            uint crc = 0xFFFFFFFFu;
            for (int i = offset; i < offset + count; i++) {
                crc = table[(crc ^ data[i]) & 0xFF] ^ (crc >> 8);
            }
            return crc ^ 0xFFFFFFFFu;
        }

        public static uint compute(byte[] data) {
            return compute(data, 0, data.Length);
        }

        public static string toHex(uint crc) {
            return crc.ToString("x8");
        }
    }
}
