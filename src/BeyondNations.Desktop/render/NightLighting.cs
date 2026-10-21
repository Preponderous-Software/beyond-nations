using System;
using System.Numerics;

namespace beyondnations.desktop.render {

    /**
    * Light the game adds at night so the world stays playable after dark.
    *
    * Strata's sun model (#280) drops to a hemisphere ambient of about 0.05
    * and a moon of about 0.1 at midnight, and neither is exposed through the
    * ABI, so on the native renderer a third of every day was close to black:
    * the player could not see the trees they were walking to. Rather than
    * patching the engine, the game uses the point lights Strata already has
    * (ABI 1.1): a warm lantern carried by the player, and a broad, cool
    * moonlight fill hung high above them that pools on the ground around the
    * player and fades with distance. Both scale with how dark it is, so the
    * day is untouched and the night is moody but readable.
    *
    * There is no engine call in this file; it turns a time of day and a
    * position into lights, so it can be checked without a GPU.
    */
    public static class NightLighting {

        public struct Light {
            public Vector3 Position;
            public float Range;
            public Vector3 Colour;
            public float Intensity;
        }

        public const int LightCount = 2;

        private static readonly Vector3 LanternOffset = new Vector3(0f, 2.5f, 0f);
        private const float LanternRange = 30f;
        private static readonly Vector3 LanternColour = new Vector3(1.0f, 0.78f, 0.52f);
        private const float LanternIntensity = 0.9f;

        private static readonly Vector3 MoonOffset = new Vector3(0f, 60f, 0f);
        private const float MoonRange = 230f;
        private static readonly Vector3 MoonColour = new Vector3(0.55f, 0.65f, 1.0f);
        private const float MoonIntensity = 0.75f;

        /**
        * The sun's height above the horizon as a sine, the way Strata
        * computes it for time_of_day (declination 0): 1 at noon on the
        * equator, 0 at sunrise and sunset, negative at night.
        */
        public static float getSunHeight(float timeOfDay, float latitudeDegrees) {
            float t = timeOfDay - MathF.Floor(timeOfDay);
            float hourAngle = (t - 0.5f) * 2f * MathF.PI;
            return MathF.Cos(latitudeDegrees * MathF.PI / 180f) * MathF.Cos(hourAngle);
        }

        /**
        * 0 in full daylight, 1 in deep night, easing through dusk and dawn.
        */
        public static float getDarkness(float timeOfDay, float latitudeDegrees) {
            float height = getSunHeight(timeOfDay, latitudeDegrees);
            return 1f - smoothstep(-0.15f, 0.10f, height);
        }

        /**
        * Fills lights (at least LightCount long) for this moment and returns
        * how many are lit; 0 in daylight, when nothing needs adding.
        */
        public static int compute(float timeOfDay, float latitudeDegrees, Vector3 playerPosition, Light[] lights) {
            float darkness = getDarkness(timeOfDay, latitudeDegrees);
            if (darkness <= 0.001f) {
                return 0;
            }
            lights[0] = new Light {
                Position = playerPosition + LanternOffset,
                Range = LanternRange,
                Colour = LanternColour,
                Intensity = LanternIntensity * darkness,
            };
            lights[1] = new Light {
                Position = playerPosition + MoonOffset,
                Range = MoonRange,
                Colour = MoonColour,
                Intensity = MoonIntensity * darkness,
            };
            return LightCount;
        }

        private static float smoothstep(float edge0, float edge1, float x) {
            float t = Math.Clamp((x - edge0) / (edge1 - edge0), 0f, 1f);
            return t * t * (3f - 2f * t);
        }
    }
}
