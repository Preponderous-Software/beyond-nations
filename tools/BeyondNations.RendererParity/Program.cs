using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using StbImageWriteSharp;
using beyondnations;
using beyondnations.desktop.render;

namespace beyondnations.tools.parity {

    /**
    * Renderer parity for engine phase B1 (#275), items 3 and 4 of RFC 0020's
    * parity plan.
    *
    * For each seed and each camera view, one WorldSnapshot is captured and
    * drawn twice in the same GL context: once by SilkWorldRenderer and once
    * by NativeWorldRenderer (Strata), each with the same view, projection and
    * a freshly begun RenderCuller. Then:
    *
    *   draw parity    per-kind instance counts, instances drawn and instances
    *                  culled must be equal
    *   image parity   at most --max-fraction of pixels may differ by more
    *                  than --threshold CIE76 Delta E
    *
    * Both renderers see one simulation state, so the comparison does not
    * depend on the simulation being reproducible across runs (#272).
    *
    *   BeyondNations.RendererParity [--seeds 42,7,1234] [--ticks 300] [--out DIR]
    *                                [--width 960] [--height 540]
    *                                [--threshold 4] [--max-fraction 0.01]
    */
    public static unsafe class Program {
        private static readonly (string name, float yaw, float pitch)[] Views = {
            ("behind", 0f, 0f),
            ("side", 90f, 10f),
            ("across", -120f, -15f),
        };

        private static int[] seeds = { 42, 7, 1234 };
        private static int ticks = 300;
        private static string outDir = "parity-out";
        private static int width = 960, height = 540;
        private static double threshold = 4.0, maxFraction = 0.01;
        private static int exitCode = 1;

        public static int Main(string[] args) {
            for (int i = 0; i < args.Length; i++) {
                string value = i + 1 < args.Length ? args[i + 1] : null;
                switch (args[i]) {
                    case "--seeds": seeds = Array.ConvertAll(value.Split(','), s => int.Parse(s, CultureInfo.InvariantCulture)); i++; break;
                    case "--ticks": ticks = int.Parse(value, CultureInfo.InvariantCulture); i++; break;
                    case "--out": outDir = value; i++; break;
                    case "--width": width = int.Parse(value, CultureInfo.InvariantCulture); i++; break;
                    case "--height": height = int.Parse(value, CultureInfo.InvariantCulture); i++; break;
                    case "--threshold": threshold = double.Parse(value, CultureInfo.InvariantCulture); i++; break;
                    case "--max-fraction": maxFraction = double.Parse(value, CultureInfo.InvariantCulture); i++; break;
                    default: Console.Error.WriteLine("unknown argument " + args[i]); return 2;
                }
            }
            Directory.CreateDirectory(outDir);

            WindowOptions options = WindowOptions.Default;
            options.Size = new Vector2D<int>(width, height);
            options.Title = "Beyond Nations renderer parity";
            options.VSync = false;
            options.API = new GraphicsAPI(options.API.API, options.API.Profile, options.API.Flags, new APIVersion(4, 5));
            // Silk.NET hides why GLFW could not load; ask it directly first so a
            // failure says something useful.
            try {
                Silk.NET.GLFW.Glfw.GetApi().Dispose();
            } catch (Exception e) {
                Console.Error.WriteLine("GLFW could not be loaded: " + e);
                return 1;
            }
            IWindow window = Window.Create(options);
            window.Load += () => {
                try {
                    exitCode = run(window);
                } catch (Exception e) {
                    Console.Error.WriteLine(e);
                    exitCode = 1;
                }
                window.Close();
            };
            window.Run();
            window.Dispose();
            return exitCode;
        }

        private static int run(IWindow window) {
            GL gl = GL.GetApi(window);
            Vector2D<int> size = window.FramebufferSize;
            width = size.X;
            height = size.Y;
            Console.WriteLine("GL: " + gl.GetStringS(StringName.Renderer) + " | " + gl.GetStringS(StringName.Version));

            // The host's own state, exactly as Game.onLoad sets it.
            gl.Enable(EnableCap.DepthTest);
            gl.Enable(EnableCap.CullFace);
            gl.CullFace(TriangleFace.Back);
            gl.ClearColor(0.45f, 0.65f, 0.90f, 1.0f);
            gl.Viewport(0, 0, (uint) width, (uint) height);

            int failures = 0;
            using (SilkWorldRenderer silk = new SilkWorldRenderer(gl))
            using (NativeWorldRenderer native = new NativeWorldRenderer(name => window.GLContext.GetProcAddress(name), width, height)) {
                native.setViewportSize(width, height);
                Console.WriteLine();
                Console.WriteLine("seed  view      | silk: cube capsule cylinder sphere drawn culled | native: cube capsule cylinder sphere drawn culled | pixels > dE " + threshold + "    max dE  result");
                foreach (int seed in seeds) {
                    GameConfig config = new GameConfig();
                    config.setWorldSeed(seed);
                    Simulation simulation = new Simulation(config);
                    for (int t = 0; t < ticks; t++) {
                        simulation.getTickCounter().increment();
                        simulation.fixedUpdate(1f / 50f);
                    }
                    WorldSnapshot snapshot = new WorldSnapshot();
                    snapshot.capture(simulation.getEntityRepository(), simulation.getEnvironment());
                    Player player = simulation.getPlayer();

                    foreach (var view in Views) {
                        PlayerCamera camera = new PlayerCamera();
                        camera.setAspectRatio(width, height);
                        camera.setRenderDistance(player.getRenderDistance());
                        camera.follow(player.getPosition(), player.getYaw(), view.yaw, view.pitch);
                        RenderCuller culler = new RenderCuller();

                        Counts silkCounts = draw(gl, silk, snapshot, camera, culler, out byte[] silkPixels);
                        Counts nativeCounts = draw(gl, native, snapshot, camera, culler, out byte[] nativePixels);

                        string stem = Path.Combine(outDir, "seed" + seed + "-" + view.name);
                        writePng(stem + "-silk.png", silkPixels);
                        writePng(stem + "-native.png", nativePixels);
                        ImageDiff diff = compare(silkPixels, nativePixels);
                        bool countsMatch = silkCounts.Equals(nativeCounts);
                        bool imageMatches = diff.fraction <= maxFraction;
                        bool ok = countsMatch && imageMatches && silkCounts.drawn > 0;
                        if (!imageMatches) {
                            writePng(stem + "-diff.png", diff.image);
                        }
                        Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                            "{0,-5} {1,-9} | {2} | {3} | {4,7} ({5:P3}) {6,7:F2}  {7}",
                            seed, view.name, silkCounts, nativeCounts, diff.over, diff.fraction, diff.maxDeltaE,
                            ok ? "ok" : (!countsMatch ? "COUNTS DIFFER" : !imageMatches ? "IMAGE DIFFERS" : "NOTHING DRAWN")));
                        if (!ok) failures++;
                    }
                }
            }
            Console.WriteLine();
            Console.WriteLine(failures == 0 ? "renderer parity: all views match" : "renderer parity: " + failures + " view(s) differ");
            return failures == 0 ? 0 : 1;
        }

        private struct Counts : IEquatable<Counts> {
            public int cube, capsule, cylinder, sphere, drawn, culled;
            public bool Equals(Counts o) {
                return cube == o.cube && capsule == o.capsule && cylinder == o.cylinder && sphere == o.sphere
                    && drawn == o.drawn && culled == o.culled;
            }
            public override string ToString() {
                return string.Format(CultureInfo.InvariantCulture, "{0,4} {1,7} {2,8} {3,6} {4,5} {5,6}",
                    cube, capsule, cylinder, sphere, drawn, culled);
            }
        }

        private static Counts draw(GL gl, IWorldRenderer renderer, WorldSnapshot snapshot, PlayerCamera camera,
                                   RenderCuller culler, out byte[] pixels) {
            gl.Viewport(0, 0, (uint) width, (uint) height);
            gl.Clear((uint) (ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));
            culler.beginFrame(camera);
            renderer.render(snapshot, camera.getViewMatrix(), camera.getProjectionMatrix(), culler);
            gl.Finish();
            pixels = new byte[width * height * 4];
            fixed (byte* p = pixels) {
                gl.ReadBuffer(ReadBufferMode.Back);
                gl.PixelStore(PixelStoreParameter.PackAlignment, 1);
                gl.ReadPixels(0, 0, (uint) width, (uint) height, PixelFormat.Rgba, PixelType.UnsignedByte, p);
            }
            return new Counts {
                cube = renderer.getInstanceCount(PrimitiveKind.Cube),
                capsule = renderer.getInstanceCount(PrimitiveKind.Capsule),
                cylinder = renderer.getInstanceCount(PrimitiveKind.Cylinder),
                sphere = renderer.getInstanceCount(PrimitiveKind.Sphere),
                drawn = renderer.getInstancesDrawn(),
                culled = renderer.getInstancesCulled(),
            };
        }

        // ---- image comparison: sRGB -> CIE L*a*b* (D65), CIE76 Delta E, as Strata's golden tests do

        private struct ImageDiff {
            public int over;
            public double fraction, maxDeltaE;
            public byte[] image;
        }

        private static ImageDiff compare(byte[] a, byte[] b) {
            ImageDiff d = new ImageDiff { image = new byte[a.Length] };
            int pixels = a.Length / 4;
            for (int i = 0; i < a.Length; i += 4) {
                double e = deltaE(a, b, i);
                d.maxDeltaE = Math.Max(d.maxDeltaE, e);
                if (e > threshold) {
                    d.over++;
                    d.image[i] = 255;
                } else {
                    byte grey = (byte) ((0.299 * a[i] + 0.587 * a[i + 1] + 0.114 * a[i + 2]) * 0.35);
                    d.image[i] = d.image[i + 1] = d.image[i + 2] = grey;
                }
                d.image[i + 3] = 255;
            }
            d.fraction = (double) d.over / pixels;
            return d;
        }

        private static double deltaE(byte[] a, byte[] b, int i) {
            if (a[i] == b[i] && a[i + 1] == b[i + 1] && a[i + 2] == b[i + 2]) return 0;
            lab(a[i], a[i + 1], a[i + 2], out double l1, out double a1, out double b1);
            lab(b[i], b[i + 1], b[i + 2], out double l2, out double a2, out double b2);
            return Math.Sqrt((l1 - l2) * (l1 - l2) + (a1 - a2) * (a1 - a2) + (b1 - b2) * (b1 - b2));
        }

        private static double linear(byte c) {
            double v = c / 255.0;
            return v <= 0.04045 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
        }

        private static double f(double t) {
            const double delta = 6.0 / 29.0;
            return t > delta * delta * delta ? Math.Cbrt(t) : t / (3 * delta * delta) + 4.0 / 29.0;
        }

        private static void lab(byte r8, byte g8, byte b8, out double l, out double a, out double b) {
            double r = linear(r8), g = linear(g8), bl = linear(b8);
            double x = (0.4124564 * r + 0.3575761 * g + 0.1804375 * bl) / 0.95047;
            double y = 0.2126729 * r + 0.7151522 * g + 0.0721750 * bl;
            double z = (0.0193339 * r + 0.1191920 * g + 0.9503041 * bl) / 1.08883;
            double fx = f(x), fy = f(y), fz = f(z);
            l = 116 * fy - 16;
            a = 500 * (fx - fy);
            b = 200 * (fy - fz);
        }

        /** GL rows are bottom-up; PNG rows are top-down. */
        private static void writePng(string path, byte[] bottomUp) {
            byte[] topDown = new byte[bottomUp.Length];
            int row = width * 4;
            for (int y = 0; y < height; y++) {
                System.Buffer.BlockCopy(bottomUp, (height - 1 - y) * row, topDown, y * row, row);
            }
            for (int i = 3; i < topDown.Length; i += 4) topDown[i] = 255;
            using (FileStream stream = File.Create(path)) {
                new ImageWriter().WritePng(topDown, width, height, ColorComponents.RedGreenBlueAlpha, stream);
            }
        }
    }
}
