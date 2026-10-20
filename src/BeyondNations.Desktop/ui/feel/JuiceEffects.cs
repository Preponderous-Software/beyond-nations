using System;
using System.Collections.Generic;
using System.Numerics;

namespace beyondnations.desktop.ui.feel {

    /**
    * The small visual rewards ("juice"), made of the primitives the world is
    * already drawn with: wood chips and stone flakes that fly off and fall,
    * floating "+5 stone" text, a short camera shake, new buildings rising out
    * of the ground inside a scaffold, and a warm flash when an objective is
    * done.
    *
    * All of it is presentation. It is appended to the frame's snapshot after
    * capture() and gone by the next one; its randomness is its own
    * System.Random, never the simulation's RandomSource; and it runs on real
    * frame time, so the world steps exactly as it would without it.
    */
    public class JuiceEffects {
        public const float Gravity = -22f;
        public const float FloatingTextSeconds = 1.4f;
        public const float FloatingTextRise = 1.4f;
        public const float RiseSeconds = 1.6f;
        public const float RiseRadius = 5.5f;
        public const float FlashSeconds = 0.7f;
        public const float ShakeSeconds = 0.25f;
        public const int MaxParticles = 400;
        public const int MaxFloatingTexts = 12;

        public static readonly Rgba WoodChip = new Rgba(0.62f, 0.42f, 0.22f);
        public static readonly Rgba LeafBit = new Rgba(0.25f, 0.55f, 0.18f);
        public static readonly Rgba StoneChip = new Rgba(0.62f, 0.62f, 0.64f);
        public static readonly Rgba Feather = new Rgba(0.95f, 0.93f, 0.88f);
        public static readonly Rgba Dust = new Rgba(0.78f, 0.70f, 0.52f);
        public static readonly Rgba Spark = new Rgba(1f, 0.85f, 0.3f);
        public static readonly Rgba ScaffoldWood = new Rgba(0.50f, 0.34f, 0.18f);

        public struct Particle {
            public Vector3 position;
            public Vector3 velocity;
            public float age;
            public float life;
            public float size;
            public Rgba color;
        }

        public struct FloatingText {
            public Vector3 position;
            public string text;
            public float age;
            public Vector4 colour;
        }

        public struct Rise {
            public Vector3 centre;
            public float age;
        }

        private readonly List<Particle> particles = new List<Particle>();
        private readonly List<FloatingText> texts = new List<FloatingText>();
        private readonly List<Rise> rises = new List<Rise>();
        private readonly Random random;
        private float flashAge = FlashSeconds;
        private float shakeAge = ShakeSeconds;
        private float shakeStrength;
        private float clock;

        public JuiceEffects(int seed = 7) {
            random = new Random(seed);
        }

        public IReadOnlyList<Particle> getParticles() { return particles; }
        public IReadOnlyList<FloatingText> getFloatingTexts() { return texts; }
        public IReadOnlyList<Rise> getRises() { return rises; }

        public void clear() {
            particles.Clear();
            texts.Clear();
            rises.Clear();
            flashAge = FlashSeconds;
            shakeAge = ShakeSeconds;
        }

        /** A spray of small cubes from a point, thrown up and out. */
        public void burst(Vector3 at, Rgba color, int count, float speed, float size) {
            for (int i = 0; i < count && particles.Count < MaxParticles; i++) {
                float angle = (float) (random.NextDouble() * Math.PI * 2.0);
                float out_ = speed * (0.4f + (float) random.NextDouble() * 0.6f);
                Particle p;
                p.position = at + new Vector3(0f, 0.6f + (float) random.NextDouble() * 1.2f, 0f);
                p.velocity = new Vector3(MathF.Cos(angle) * out_, speed * (0.8f + (float) random.NextDouble() * 0.8f), MathF.Sin(angle) * out_);
                p.age = 0f;
                p.life = 0.55f + (float) random.NextDouble() * 0.45f;
                p.size = size * (0.6f + (float) random.NextDouble() * 0.6f);
                p.color = color;
                particles.Add(p);
            }
        }

        /** "+5 stone" at a world position, rising and fading; a stack of them is spread out so they do not overlap. */
        public void floatText(Vector3 at, string text, Vector4 colour) {
            if (texts.Count >= MaxFloatingTexts) {
                texts.RemoveAt(0);
            }
            int stacked = 0;
            for (int i = 0; i < texts.Count; i++) {
                if (texts[i].age < 0.3f) {
                    stacked++;
                }
            }
            texts.Add(new FloatingText {
                position = at + new Vector3(0f, 1.6f + stacked * 0.55f, 0f),
                text = text,
                age = 0f,
                colour = colour
            });
        }

        public void rise(Vector3 centre) {
            rises.Add(new Rise { centre = centre, age = 0f });
        }

        public void flash() {
            flashAge = 0f;
        }

        public void shake(float strength) {
            bool shaking = shakeAge < ShakeSeconds;
            shakeStrength = shaking ? Math.Max(strength, shakeStrength) : strength;
            shakeAge = 0f;
        }

        public void update(float deltaSeconds) {
            float dt = Math.Clamp(deltaSeconds, 0f, 0.1f);
            clock += dt;
            for (int i = particles.Count - 1; i >= 0; i--) {
                Particle p = particles[i];
                p.age += dt;
                if (p.age >= p.life) {
                    particles.RemoveAt(i);
                    continue;
                }
                p.velocity.Y += Gravity * dt;
                p.position += p.velocity * dt;
                if (p.position.Y < SettlementLayout.GroundTopY + p.size * 0.5f) {
                    p.position.Y = SettlementLayout.GroundTopY + p.size * 0.5f;
                    p.velocity *= 0.3f;
                }
                particles[i] = p;
            }
            for (int i = texts.Count - 1; i >= 0; i--) {
                FloatingText t = texts[i];
                t.age += dt;
                if (t.age >= FloatingTextSeconds) {
                    texts.RemoveAt(i);
                    continue;
                }
                texts[i] = t;
            }
            for (int i = rises.Count - 1; i >= 0; i--) {
                Rise r = rises[i];
                r.age += dt;
                if (r.age >= RiseSeconds) {
                    rises.RemoveAt(i);
                    continue;
                }
                rises[i] = r;
            }
            flashAge = Math.Min(FlashSeconds, flashAge + dt);
            shakeAge = Math.Min(ShakeSeconds, shakeAge + dt);
        }

        /** 0 to 1: how strong the objective flash is right now. */
        public float getFlashStrength() {
            return 1f - flashAge / FlashSeconds;
        }

        /** Where the camera is nudged this frame: a quick decaying wobble. */
        public Vector3 getShakeOffset() {
            if (shakeAge >= ShakeSeconds) {
                return Vector3.Zero;
            }
            float fade = 1f - shakeAge / ShakeSeconds;
            float s = shakeStrength * fade * fade;
            return new Vector3(MathF.Sin(clock * 71f) * s, MathF.Sin(clock * 53f + 1.3f) * s * 0.6f, MathF.Cos(clock * 67f) * s);
        }

        /** How far up a rising building is, 0 to 1, eased so it settles into place. */
        public static float riseProgress(float age) {
            float t = Math.Clamp(age / RiseSeconds, 0f, 1f);
            float inv = 1f - t;
            return 1f - inv * inv * inv;
        }

        /** Floating text's height above where it started and its opacity, for its age. */
        public static float textRise(float age) {
            return FloatingTextRise * Math.Clamp(age / FloatingTextSeconds, 0f, 1f);
        }

        public static float textAlpha(float age) {
            float t = Math.Clamp(age / FloatingTextSeconds, 0f, 1f);
            return t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
        }

        /**
        * Adds this frame's particles and scaffolds to the snapshot and squashes
        * whatever stands on a rising building's plot down towards the ground,
        * so the building grows up out of it.
        */
        public void decorate(WorldSnapshot snapshot) {
            for (int i = 0; i < particles.Count; i++) {
                Particle p = particles[i];
                float shrink = 1f - Math.Clamp((p.age - p.life * 0.5f) / (p.life * 0.5f), 0f, 1f);
                float size = p.size * Math.Max(0.15f, shrink);
                snapshot.addEffectItem(new RenderItem {
                    kind = PrimitiveKind.Cube,
                    position = p.position,
                    scale = new Vector3(size, size, size),
                    color = p.color
                });
            }
            if (rises.Count == 0) {
                return;
            }
            // Squash first, over the captured items only, then add scaffolds.
            IReadOnlyList<RenderItem> items = snapshot.getEntityItems();
            int captured = items.Count - particles.Count;
            float ground = SettlementLayout.GroundTopY;
            for (int r = 0; r < rises.Count; r++) {
                Rise rise = rises[r];
                float progress = riseProgress(rise.age);
                float radiusSquared = RiseRadius * RiseRadius;
                for (int i = 0; i < captured; i++) {
                    RenderItem item = items[i];
                    float dx = item.position.X - rise.centre.X;
                    float dz = item.position.Z - rise.centre.Z;
                    if (dx * dx + dz * dz > radiusSquared || item.position.Y <= ground + 0.06f) {
                        continue;
                    }
                    item.position.Y = ground + (item.position.Y - ground) * progress;
                    item.scale.Y *= Math.Max(0.02f, progress);
                    snapshot.setEntityItem(i, item);
                }
                addScaffold(snapshot, rise.centre, progress);
            }
        }

        private static void addScaffold(WorldSnapshot snapshot, Vector3 centre, float progress) {
            // Four poles and a cross-beam that stand while the walls go up and
            // sink away as it finishes.
            float height = 5f * (1f - progress * progress);
            if (height < 0.1f) {
                return;
            }
            float ground = SettlementLayout.GroundTopY;
            float half = 2.8f;
            for (int corner = 0; corner < 4; corner++) {
                float x = (corner & 1) == 0 ? -half : half;
                float z = (corner & 2) == 0 ? -half : half;
                snapshot.addEffectItem(new RenderItem {
                    kind = PrimitiveKind.Cube,
                    position = new Vector3(centre.X + x, ground + height / 2f, centre.Z + z),
                    scale = new Vector3(0.22f, height, 0.22f),
                    color = ScaffoldWood
                });
            }
            snapshot.addEffectItem(new RenderItem {
                kind = PrimitiveKind.Cube,
                position = new Vector3(centre.X, ground + height * 0.8f, centre.Z - half),
                scale = new Vector3(half * 2f, 0.18f, 0.18f),
                color = ScaffoldWood
            });
            snapshot.addEffectItem(new RenderItem {
                kind = PrimitiveKind.Cube,
                position = new Vector3(centre.X, ground + height * 0.8f, centre.Z + half),
                scale = new Vector3(half * 2f, 0.18f, 0.18f),
                color = ScaffoldWood
            });
        }
    }
}
