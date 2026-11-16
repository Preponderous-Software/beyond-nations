using System;
using System.Numerics;
using Xunit;

using beyondnations.desktop.ui;

namespace beyondnationstests.desktop.ui {

    /**
    * Buttons are drawn over the world, so whatever is behind them is whatever
    * the camera sees. In the 0.6.4 playtest ImGui's translucent default let
    * sunlit grass through, leaving white text at about 1.8:1, and a disabled
    * button looked like an enabled one. Each colour is measured here over a
    * white and a black backdrop, the two extremes of what can be behind it,
    * using the WCAG contrast ratio.
    */
    public class TestUiTheme {
        private const float MinimumContrast = 4.5f;

        private static readonly Vector4 BrightestBackdrop = new Vector4(1f, 1f, 1f, 1f);
        private static readonly Vector4 DarkestBackdrop = new Vector4(0f, 0f, 0f, 1f);

        private static Vector4 over(Vector4 top, Vector4 under) {
            Vector4 mixed = top * top.W + under * (1f - top.W);
            mixed.W = 1f;
            return mixed;
        }

        private static float linear(float channel) {
            return channel <= 0.04045f ? channel / 12.92f : MathF.Pow((channel + 0.055f) / 1.055f, 2.4f);
        }

        private static float luminance(Vector4 colour) {
            return 0.2126f * linear(colour.X) + 0.7152f * linear(colour.Y) + 0.0722f * linear(colour.Z);
        }

        private static float contrast(Vector4 text, Vector4 fill, Vector4 backdrop) {
            Vector4 behind = over(fill, backdrop);
            float textLuminance = luminance(over(text, behind));
            float behindLuminance = luminance(behind);
            return (MathF.Max(textLuminance, behindLuminance) + 0.05f) / (MathF.Min(textLuminance, behindLuminance) + 0.05f);
        }

        private static float colourfulness(Vector4 colour) {
            return MathF.Max(colour.X, MathF.Max(colour.Y, colour.Z)) - MathF.Min(colour.X, MathF.Min(colour.Y, colour.Z));
        }

        private static void assertReadable(ButtonColours colours, Vector4 fill) {
            Assert.True(contrast(colours.Text, fill, BrightestBackdrop) >= MinimumContrast);
            Assert.True(contrast(colours.Text, fill, DarkestBackdrop) >= MinimumContrast);
        }

        [Theory]
        [InlineData("Standard")]
        [InlineData("Drastic")]
        [InlineData("Armed")]
        [InlineData("Disabled")]
        public void aButton_ReadsOverAnyBackdrop_InEveryState(string name) {
            ButtonColours colours = name switch {
                "Standard" => UiTheme.Standard,
                "Drastic" => UiTheme.Drastic,
                "Armed" => UiTheme.Armed,
                _ => UiTheme.Disabled,
            };
            assertReadable(colours, colours.Fill);
            assertReadable(colours, colours.Hovered);
            assertReadable(colours, colours.Active);
        }

        [Fact]
        public void aDisabledButton_LooksDifferentFromAnEnabledOne() {
            Vector4 enabledFill = over(UiTheme.Standard.Fill, BrightestBackdrop);
            Vector4 disabledFill = over(UiTheme.Disabled.Fill, BrightestBackdrop);
            // grey beside coloured, so it does not rest on seeing a small change in brightness
            Assert.True(colourfulness(enabledFill) >= 0.2f);
            Assert.True(colourfulness(disabledFill) <= 0.05f);
            // and its text is dimmer
            Assert.True(luminance(over(UiTheme.Disabled.Text, disabledFill)) < luminance(over(UiTheme.Standard.Text, enabledFill)));
        }

        [Fact]
        public void aCommandButton_ThatTakesNoClick_LooksDisabled_WhateverElseItIs() {
            Assert.Same(UiTheme.Disabled, UiTheme.commandButton(false, false, false));
            Assert.Same(UiTheme.Disabled, UiTheme.commandButton(false, true, false));
            Assert.Same(UiTheme.Disabled, UiTheme.commandButton(false, false, true));
            Assert.Same(UiTheme.Disabled, UiTheme.commandButton(false, true, true));
        }

        [Fact]
        public void aCommandButton_IsArmed_ThenDrastic_ThenStandard() {
            Assert.Same(UiTheme.Armed, UiTheme.commandButton(true, true, true));
            Assert.Same(UiTheme.Armed, UiTheme.commandButton(true, true, false));
            Assert.Same(UiTheme.Drastic, UiTheme.commandButton(true, false, true));
            Assert.Same(UiTheme.Standard, UiTheme.commandButton(true, false, false));
        }
    }
}
