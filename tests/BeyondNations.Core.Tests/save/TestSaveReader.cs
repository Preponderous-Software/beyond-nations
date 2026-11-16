using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text.Json;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * SaveReader's value parsers and SaveStreams, the pieces a load leans on
    * for every field. The round-trip tests exercise them only along the
    * happy path; these pin down what each one refuses, and how.
    */
    public class TestSaveReader {

        /** Parses JSON written with single quotes, so the tests stay readable. */
        private static JsonElement parse(string json) {
            using (JsonDocument document = JsonDocument.Parse(json.Replace('\'', '"'))) {
                return document.RootElement.Clone();
            }
        }

        // --- SaveStreams ---

        [Fact]
        public void testStreams_GetReturnsTheSameInstanceThatWasAdded() {
            SaveStreams streams = new SaveStreams();
            RandomSource main = new RandomSource(20260816);
            RandomSource traits = new RandomSource(20260816);
            streams.add(SaveStreams.Main, main);
            streams.add(SaveStreams.Traits, traits);

            Assert.Same(main, streams.get(SaveStreams.Main));
            Assert.Same(traits, streams.get(SaveStreams.Traits));
        }

        [Fact]
        public void testStreams_NameOfComparesByReferenceNotBySeed() {
            SaveStreams streams = new SaveStreams();
            RandomSource main = new RandomSource(20260816);
            RandomSource population = new RandomSource(20260816);
            streams.add(SaveStreams.Main, main);
            streams.add(SaveStreams.Population, population);

            Assert.Equal(SaveStreams.Main, streams.nameOf(main));
            Assert.Equal(SaveStreams.Population, streams.nameOf(population));
        }

        [Fact]
        public void testStreams_NameOfAStreamTheWorldDoesNotOwn_FallsBackToMain() {
            SaveStreams streams = new SaveStreams();
            streams.add(SaveStreams.SettlementLife, new RandomSource(20260816));

            Assert.Equal(SaveStreams.Main, streams.nameOf(new RandomSource(20260816)));
        }

        [Fact]
        public void testStreams_GetOfAnUnknownName_Throws() {
            SaveStreams streams = new SaveStreams();
            streams.add(SaveStreams.Main, new RandomSource(20260816));

            SaveFormatException e = Assert.Throws<SaveFormatException>(() => streams.get("weather"));
            Assert.Equal("unknown random stream 'weather'", e.Message);
        }

        [Fact]
        public void testStreams_AllKeepsTheOrderTheyWereAdded() {
            SaveStreams streams = new SaveStreams();
            streams.add(SaveStreams.Population, new RandomSource(1));
            streams.add(SaveStreams.Main, new RandomSource(2));
            streams.add(SaveStreams.Traits, new RandomSource(3));

            IReadOnlyList<KeyValuePair<string, RandomSource>> all = streams.all();
            Assert.Equal(3, all.Count);
            Assert.Equal(SaveStreams.Population, all[0].Key);
            Assert.Equal(SaveStreams.Main, all[1].Key);
            Assert.Equal(SaveStreams.Traits, all[2].Key);
        }

        // --- properties ---

        [Fact]
        public void testProperty_MissingName_ThrowsNamingIt() {
            JsonElement obj = parse("{'a': 1}");

            SaveFormatException e = Assert.Throws<SaveFormatException>(() => SaveReader.property(obj, "b"));
            Assert.Equal("the save is missing 'b'", e.Message);
        }

        [Fact]
        public void testProperty_OfANonObject_ThrowsRatherThanCrashing() {
            JsonElement array = parse("[1, 2]");

            Assert.Throws<SaveFormatException>(() => SaveReader.property(array, "a"));
            Assert.False(SaveReader.has(array, "a"));
        }

        [Fact]
        public void testHas_ReportsPresenceIncludingNull() {
            JsonElement obj = parse("{'a': null}");

            Assert.True(SaveReader.has(obj, "a"));
            Assert.False(SaveReader.has(obj, "b"));
        }

        [Fact]
        public void testScalars_ReadTheirValues() {
            JsonElement obj = parse("{'i': -7, 'l': 5000000000, 'b': true, 's': 'oak', 'n': null}");

            Assert.Equal(-7, SaveReader.getInt(obj, "i"));
            Assert.Equal(5000000000L, SaveReader.getLong(obj, "l"));
            Assert.True(SaveReader.getBool(obj, "b"));
            Assert.Equal("oak", SaveReader.getString(obj, "s"));
            Assert.Null(SaveReader.getString(obj, "n"));
        }

        // --- floats ---

        [Fact]
        public void testFloat_ReadsANumberExactly() {
            JsonElement obj = parse("{'f': 0.1, 'e': 1.5E-05}");

            Assert.Equal(0.1f, SaveReader.getFloat(obj, "f"));
            Assert.Equal(1.5E-05f, SaveReader.getFloat(obj, "e"));
        }

        [Fact]
        public void testFloat_ReadsTheStringsTheWriterUsesForNonFiniteValues() {
            // SaveWriter writes NaN and the infinities as their invariant-culture strings
            string nan = float.NaN.ToString(CultureInfo.InvariantCulture);
            string positive = float.PositiveInfinity.ToString(CultureInfo.InvariantCulture);
            string negative = float.NegativeInfinity.ToString(CultureInfo.InvariantCulture);
            JsonElement obj = parse("{'nan': '" + nan + "', 'pos': '" + positive + "', 'neg': '" + negative + "'}");

            Assert.True(float.IsNaN(SaveReader.getFloat(obj, "nan")));
            Assert.Equal(float.PositiveInfinity, SaveReader.getFloat(obj, "pos"));
            Assert.Equal(float.NegativeInfinity, SaveReader.getFloat(obj, "neg"));
        }

        [Fact]
        public void testVector3AndRgba_ReadTheirComponentsInOrder() {
            JsonElement obj = parse("{'v': [1, -2.5, 3], 'c': [0.25, 0.5, 0.75, 1]}");

            Assert.Equal(new Vector3(1f, -2.5f, 3f), SaveReader.getVector3(obj, "v"));
            Assert.Equal(new Rgba(0.25f, 0.5f, 0.75f, 1f), SaveReader.getRgba(obj, "c"));
        }

        // --- enums ---

        [Fact]
        public void testEnum_ReadsAMemberByName() {
            Assert.Equal(PrimitiveKind.Sphere, SaveReader.getEnum<PrimitiveKind>(parse("{'k': 'Sphere'}"), "k"));
        }

        [Fact]
        public void testEnum_IsCaseSensitive() {
            SaveFormatException e = Assert.Throws<SaveFormatException>(() =>
                SaveReader.getEnum<PrimitiveKind>(parse("{'k': 'sphere'}"), "k"));
            Assert.Equal("'sphere' is not a known PrimitiveKind", e.Message);
        }

        [Fact]
        public void testEnum_RefusesANumberOutsideTheEnum() {
            Assert.Throws<SaveFormatException>(() => SaveReader.getEnum<PrimitiveKind>(parse("{'k': '7'}"), "k"));
        }

        [Fact]
        public void testEnum_AcceptsANumericStringThatNamesAMember() {
            // characterised: Enum.TryParse accepts digits, and the defined-value
            // check passes, so '1' reads as the second member
            Assert.Equal(PrimitiveKind.Capsule, SaveReader.getEnum<PrimitiveKind>(parse("{'k': '1'}"), "k"));
        }

        [Fact]
        public void testEnum_RefusesNull() {
            Assert.Throws<SaveFormatException>(() => SaveReader.getEnum<PrimitiveKind>(parse("{'k': null}"), "k"));
        }

        // --- ids ---

        [Fact]
        public void testParseGuid_ReadsTheCompactForm() {
            Guid guid = new Guid("0123456789abcdef0123456789abcdef");

            Assert.Equal(guid, SaveReader.parseGuid("0123456789abcdef0123456789abcdef"));
            Assert.Equal(guid, SaveReader.getGuid(parse("{'id': '0123456789abcdef0123456789abcdef'}"), "id"));
        }

        [Fact]
        public void testParseGuid_RefusesTheHyphenatedForm() {
            SaveFormatException e = Assert.Throws<SaveFormatException>(() =>
                SaveReader.parseGuid("01234567-89ab-cdef-0123-456789abcdef"));
            Assert.Equal("'01234567-89ab-cdef-0123-456789abcdef' is not an id", e.Message);
        }

        [Fact]
        public void testParseGuid_RefusesNull() {
            Assert.Throws<SaveFormatException>(() => SaveReader.parseGuid(null));
            Assert.Throws<SaveFormatException>(() => SaveReader.getGuid(parse("{'id': null}"), "id"));
        }

        // --- lists and appearance ---

        [Fact]
        public void testStringList_KeepsOrderAndNulls() {
            List<string> values = SaveReader.getStringList(parse("{'l': ['a', null, 'c']}"), "l");

            Assert.Equal(new List<string> { "a", null, "c" }, values);
        }

        [Fact]
        public void testAppearance_NullReadsAsNoAppearance() {
            Assert.Null(SaveReader.getAppearance(parse("{'look': null}"), "look"));
        }

        [Fact]
        public void testAppearance_ReadsPartsLabelAndLabelPlacement() {
            JsonElement obj = parse("{'look': {"
                + "'parts': ["
                + "{'kind': 'Capsule', 'offset': [0, 1, 0], 'scale': [1, 2, 1], 'color': [1, 0, 0, 1]},"
                + "{'kind': 'Sphere', 'offset': [0, 2.5, 0], 'scale': [0.5, 0.5, 0.5], 'color': [0, 0, 1, 0.5]}"
                + "],"
                + "'label': 'Ada', 'labelOffset': [0, 3, 0], 'labelScale': 1.25}}");

            Appearance appearance = SaveReader.getAppearance(obj, "look");

            Assert.Equal(2, appearance.getParts().Count);
            AppearancePart body = appearance.getParts()[0];
            Assert.Equal(PrimitiveKind.Capsule, body.getKind());
            Assert.Equal(new Vector3(0f, 1f, 0f), body.getOffset());
            Assert.Equal(new Vector3(1f, 2f, 1f), body.getScale());
            Assert.Equal(new Rgba(1f, 0f, 0f, 1f), body.getColor());
            AppearancePart head = appearance.getParts()[1];
            Assert.Equal(PrimitiveKind.Sphere, head.getKind());
            Assert.Equal(new Rgba(0f, 0f, 1f, 0.5f), head.getColor());
            Assert.Equal("Ada", appearance.getLabel());
            Assert.Equal(new Vector3(0f, 3f, 0f), appearance.getLabelOffset());
            Assert.Equal(1.25f, appearance.getLabelScale());
        }

        [Fact]
        public void testAppearance_APartMissingItsColour_Throws() {
            JsonElement obj = parse("{'look': {"
                + "'parts': [{'kind': 'Cube', 'offset': [0, 0, 0], 'scale': [1, 1, 1]}],"
                + "'label': null, 'labelOffset': [0, 0, 0], 'labelScale': 1}}");

            SaveFormatException e = Assert.Throws<SaveFormatException>(() => SaveReader.getAppearance(obj, "look"));
            Assert.Equal("the save is missing 'color'", e.Message);
        }
    }
}
