using System;
using System.IO;
using System.Text;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * A damaged, foreign or newer save must give a clear error, never a crash
    * and never a half-built world (docs/SAVES.md).
    */
    public class TestSaveIntegrity {
        private static byte[] aSave() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(11);
            Simulation simulation = new Simulation(config);
            for (int i = 0; i < 200; i++) {
                simulation.fixedUpdate(1f / 50f);
            }
            return SaveGame.encode(simulation, "test");
        }

        private static string refusal(byte[] file) {
            SaveFormatException e = Assert.Throws<SaveFormatException>(() => SaveGame.decode(file));
            Assert.False(string.IsNullOrWhiteSpace(e.Message));
            return e.Message;
        }

        private static int bodyStart(byte[] file) {
            return Array.IndexOf(file, (byte) '\n') + 1;
        }

        [Fact]
        public void testHeader_DescribesTheWorld() {
            SaveHeader header = SaveGame.check(aSave());
            Assert.Equal(SaveGame.CurrentFormatVersion, header.FormatVersion);
            Assert.Equal("test", header.GameVersion);
            Assert.Equal(11, header.Seed);
            Assert.Equal(200, header.Tick);
            Assert.Equal(1, header.Day);
            Assert.Null(header.Nation);
            Assert.Equal("Day 1 - no nation", header.describe());
        }

        [Fact]
        public void testAFlippedByteInTheWorld_IsCaughtByTheChecksum() {
            byte[] file = aSave();
            int at = bodyStart(file) + (file.Length - bodyStart(file)) / 2;
            file[at] = (byte) (file[at] ^ 0x04);
            Assert.Contains("checksum", refusal(file));
        }

        [Fact]
        public void testATruncatedSave_SaysItWasCutShort() {
            byte[] file = aSave();
            byte[] cut = new byte[file.Length - 100];
            Array.Copy(file, cut, cut.Length);
            Assert.Contains("cut short", refusal(cut));
        }

        [Fact]
        public void testAnEmptyOrForeignFile_IsNotASave() {
            Assert.Contains("empty", refusal(new byte[0]));
            Assert.Contains("not a Beyond Nations save", refusal(Encoding.UTF8.GetBytes("hello world")));
            Assert.Contains("not a Beyond Nations save", refusal(Encoding.UTF8.GetBytes("{\"magic\":\"something-else\"}\n{}")));
            Assert.Contains("damaged", refusal(Encoding.UTF8.GetBytes("{not json\n{}")));
        }

        [Fact]
        public void testASaveFromANewerVersion_IsRefusedPolitely() {
            byte[] file = aSave();
            string text = Encoding.UTF8.GetString(file);
            string newer = text.Replace("\"formatVersion\":" + SaveGame.CurrentFormatVersion + ",",
                "\"formatVersion\":" + (SaveGame.CurrentFormatVersion + 1) + ",");
            string message = refusal(Encoding.UTF8.GetBytes(newer));
            Assert.Contains("newer version", message);
            Assert.Contains("has not been changed", message);
        }

        [Fact]
        public void testAWorldThatChecksOutButCannotBeBuilt_IsAClearErrorNotACrash() {
            // a well-formed save whose world is nonsense: valid checksum, missing everything
            byte[] body = Encoding.UTF8.GetBytes("{\"formatVersion\":1,\"seed\":11}");
            string header = "{\"magic\":\"beyond-nations-save\",\"formatVersion\":1,\"gameVersion\":\"x\",\"seed\":11,\"tick\":0,\"day\":1,"
                + "\"nation\":null,\"savedAt\":\"2026-10-18T00:00:00Z\",\"bodyBytes\":" + body.Length + ",\"crc32\":\"" + Crc32.toHex(Crc32.compute(body)) + "\"}\n";
            byte[] header8 = Encoding.UTF8.GetBytes(header);
            byte[] file = new byte[header8.Length + body.Length];
            header8.CopyTo(file, 0);
            body.CopyTo(file, header8.Length);
            Assert.Contains("missing", refusal(file));
        }

        [Fact]
        public void testHeaderAndWorldThatDisagree_AreRefused() {
            byte[] file = aSave();
            string text = Encoding.UTF8.GetString(file);
            int newline = text.IndexOf('\n');
            string header = text.Substring(0, newline).Replace("\"seed\":11,", "\"seed\":12,");
            Assert.Contains("seed", refusal(Encoding.UTF8.GetBytes(header + text.Substring(newline))));
        }

        [Fact]
        public void testCrc32_MatchesTheStandardCheckValue() {
            // the CRC-32 check value for "123456789"
            Assert.Equal("cbf43926", Crc32.toHex(Crc32.compute(Encoding.ASCII.GetBytes("123456789"))));
        }
    }
}
