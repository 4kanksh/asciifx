// Fuji Dawn Lake: a snow-capped volcano glowing pink at dawn over a mirror lake, with drifting mist,
// cranes flying past, a boat poling across, ripples and swaying reeds.
//
// Ported to C# from the "fuji-dawn-lake" piece in ASCII Magic's open-source library
// (https://github.com/kailashsr38/ascii-magic-art: pieces/fuji-dawn-lake.js and lib/kit.js).
// Copyright (c) 2026 ASCII Magic (ascii-magic.com). MIT License, see LICENSE in this folder.
//
// The scene is painted once on a 192 x 108 grid of square pixels; each frame adds the moving parts on a copy,
// which is then scaled to fit and drawn two pixels per character cell with half blocks, in the piece's palette.
using System;
using System.Collections.Generic;

namespace AsciiFX.Animations
{
    public class FujiDawnLake : IBackgroundAnimation
    {
        const int W = 192, H = 108, HOR = 66, CX = 88, PEAK = 16, SUNX = 150, SUNY = 50, SUNR = 7;
        const byte SKY = 0, HILL = 1, MTN = 2, LAKE = 3, POINT = 4, FORE = 5;
        const char Upper = '▀', Lower = '▄', Full = '█';

        // The piece's palette: hand-picked anchors, filled in with steps between (count per group)
        static readonly string[][] AnchorColors = {
            new[] { "#3c4a86", "#4f5a9a", "#5e5c9e", "#8a74b0", "#b884b4", "#e59bb0", "#ffbf96", "#ffdca0" },
            new[] { "#fff8e0", "#ffeab0" },
            new[] { "#7a6aa0", "#5c5490", "#4a4678" },
            new[] { "#3e4280", "#4b4f8a", "#565a98", "#6b64a0", "#8a76ae", "#9a86b8" },
            new[] { "#c7c3e6", "#e6dcf0", "#fff2ea" },
            new[] { "#1f2348", "#2b2f5c", "#3c3f72", "#5a5288", "#8a6c98", "#c48aa0", "#e8a890" },
            new[] { "#ffd48a", "#f0b080" },
            new[] { "#110f22", "#1c1a33", "#2a2440" },
            new[] { "#1e2a3a", "#2c3a48" },
            new[] { "#f0c0c8", "#d8b0c8" },
        };
        static readonly int[] AnchorCounts = { 13, 2, 4, 7, 4, 10, 2, 3, 2, 2 };

        static readonly int[][] Pines = {
            new[] { 4, 22 }, new[] { 10, 26 }, new[] { 16, 19 }, new[] { 23, 24 }, new[] { 29, 15 },
            new[] { 35, 18 }, new[] { 41, 12 }, new[] { 47, 9 }, new[] { 53, 7 }
        };
        static readonly int[][] Cranes = { new[] { 0, 0 }, new[] { 11, 4 }, new[] { 21, -2 } };

        static readonly Dictionary<string, double[]> HexCache = new Dictionary<string, double[]>();

        readonly double[] rgb = new double[W * H * 3];   // the painted scene
        readonly byte[] layer = new byte[W * H];         // what each pixel belongs to, nearer = larger
        readonly double[] work = new double[W * H * 3];  // this frame: the scene plus the moving parts
        readonly double[][] pal;
        readonly int[] palette;
        readonly Dictionary<int, int> nearestCache = new Dictionary<int, int>();
        readonly double speed;
        readonly double[] reedX = new double[20], reedH = new double[20], reedPh = new double[20];
        readonly double[] rippleU = new double[30], ripplePh = new double[30];

        // Output scaling: each output pixel averages a box of scene pixels
        int w, h, outW, outH, offX, offY;
        int[] boxX0, boxX1, boxY0, boxY1;
        int[] pixels, scratch;

        public FujiDawnLake(Options o)
        {
            speed = Math.Max(0.05, o.Number("Speed", 1));

            List<double[]> colors = new List<double[]>();
            for (int a = 0; a < AnchorColors.Length; a++)
            {
                int n = AnchorCounts[a];
                string[] cs = AnchorColors[a];
                for (int i = 0; i < n; i++)
                {
                    double t = (double)i / Math.Max(1, n - 1) * (cs.Length - 1);
                    int k = Math.Min(cs.Length - 2, (int)Math.Floor(t));
                    colors.Add(cs.Length == 1 ? Hex(cs[0]) : Mix(Hex(cs[k]), Hex(cs[k + 1]), t - k));
                }
            }
            pal = colors.ToArray();
            palette = new int[pal.Length];
            for (int i = 0; i < pal.Length; i++)
                palette[i] = ((int)Math.Round(Clamp(pal[i][0], 0, 255)) << 16) | ((int)Math.Round(Clamp(pal[i][1], 0, 255)) << 8) | (int)Math.Round(Clamp(pal[i][2], 0, 255));

            for (int i = 0; i < 20; i++)
            {
                reedX[i] = 150 + i * 2.1 + Math.Sin(i * 7.1) * 1.5;
                reedH[i] = 5 + ((i * 37) % 9) + Clamp((i - 6) / 14.0) * 6;
                reedPh[i] = i * 0.61;
            }
            for (int i = 0; i < 30; i++) { rippleU[i] = ((i * 73) % 97) / 97.0; ripplePh[i] = ((i * 41) % 53) / 53.0; }

            Paint();
        }

        public int[] Palette { get { return palette; } }
        public int FrameMs { get { return 66; } }  // the piece runs at 15 fps

        public void Resize(int cols, int rows)
        {
            w = cols; h = rows;
            scratch = new int[w * h];
            // Fit the scene in cols x (rows * 2) pixels, keeping its shape, centered
            double s = Math.Min((double)cols / W, rows * 2.0 / H);
            outW = Math.Max(1, (int)(W * s));
            outH = Math.Max(1, (int)(H * s));
            offX = (cols - outW) / 2;
            offY = (rows * 2 - outH) / 2;
            boxX0 = new int[outW]; boxX1 = new int[outW]; boxY0 = new int[outH]; boxY1 = new int[outH];
            for (int x = 0; x < outW; x++) { boxX0[x] = x * W / outW; boxX1[x] = Math.Max(boxX0[x] + 1, (x + 1) * W / outW); }
            for (int y = 0; y < outH; y++) { boxY0[y] = y * H / outH; boxY1[y] = Math.Max(boxY0[y] + 1, (y + 1) * H / outH); }
            pixels = new int[outW * outH];
        }

        public void Frame(double t, char[] chars, int[] colors)
        {
            Frame(t, chars, colors, scratch);
        }

        public void Frame(double t, char[] chars, int[] colors, int[] backgrounds)
        {
            Animate(t * speed);

            // Scale the frame to the output size, picking the nearest palette color for each pixel
            for (int y = 0; y < outH; y++)
                for (int x = 0; x < outW; x++)
                {
                    double r = 0, g = 0, b = 0;
                    int count = 0;
                    for (int sy = boxY0[y]; sy < boxY1[y]; sy++)
                        for (int sx = boxX0[x]; sx < boxX1[x]; sx++)
                        {
                            int i = (sy * W + sx) * 3;
                            r += work[i]; g += work[i + 1]; b += work[i + 2]; count++;
                        }
                    pixels[y * outW + x] = Nearest(r / count, g / count, b / count);
                }

            // Two pixels per cell: the upper one as a half block over a background of the lower one
            for (int cy = 0; cy < h; cy++)
                for (int cx = 0; cx < w; cx++)
                {
                    int k = cy * w + cx;
                    int top = PixelAt(cx, cy * 2), bottom = PixelAt(cx, cy * 2 + 1);
                    backgrounds[k] = -1;
                    if (top < 0 && bottom < 0) { chars[k] = ' '; colors[k] = -1; }
                    else if (top == bottom) { chars[k] = Full; colors[k] = top; }
                    else if (bottom < 0) { chars[k] = Upper; colors[k] = top; }
                    else if (top < 0) { chars[k] = Lower; colors[k] = bottom; }
                    else { chars[k] = Upper; colors[k] = top; backgrounds[k] = bottom; }
                }
        }

        int PixelAt(int cx, int py)
        {
            int x = cx - offX, y = py - offY;
            return x >= 0 && y >= 0 && x < outW && y < outH ? pixels[y * outW + x] : -1;
        }

        int Nearest(double r, double g, double b)
        {
            r = Clamp(r, 0, 255); g = Clamp(g, 0, 255); b = Clamp(b, 0, 255);
            int key = (((int)r >> 3) << 10) | (((int)g >> 3) << 5) | ((int)b >> 3);
            int hit;
            if (nearestCache.TryGetValue(key, out hit)) return hit;
            int best = 0;
            double bestDistance = 1e9;
            for (int i = 0; i < pal.Length; i++)
            {
                double d = (pal[i][0] - r) * (pal[i][0] - r) * 0.3 + (pal[i][1] - g) * (pal[i][1] - g) * 0.59 + (pal[i][2] - b) * (pal[i][2] - b) * 0.11;
                if (d < bestDistance) { bestDistance = d; best = i; }
            }
            nearestCache[key] = best;
            return best;
        }

        #region The still scene

        double FarTop(double x) { return 57 + 3 * Math.Sin(x / 13 + 1) + 2.5 * Fbm(x / 18, 2, 3, 4); }
        double NearTop(double x) { return 62 + 2 * Math.Sin(x / 9 + 3) + 2 * Fbm(x / 10, 5, 3, 8); }
        double PointTop(double x) { return 64 - 3 * Clamp(1 - x / 60) + 0.5 * Noise2(x / 3, 1, 0); }
        double Shore(double x) { return 101 + 2 * Math.Sin(x / 17) + 1.5 * Fbm(x / 6, 7, 3, 2) - Clamp((x - 150) / 40) * 6; }

        double MountainTop(double x)
        {
            double d = Math.Abs(x - CX);
            if (d < 6) return PEAK + (d < 2 ? 1 : 0);
            double s = Clamp((d - 6) / 82);
            return PEAK + (HOR - PEAK) * (1 - Math.Pow(1 - s, 1.75)) + (x < CX ? 0.6 : 0);
        }

        double SnowLine(double x)
        {
            return 31 + 6 * Fbm(x / 2.2, 9, 3, 21) + 4 * Math.Sin(x * 0.9) * Math.Sin(x * 0.23) + Math.Abs(x - CX) * 0.05;
        }

        double[] Sky(double x, double y)
        {
            double[] c = Ramp(y / HOR,
                new[] { 0, 0.3, 0.52, 0.68, 0.82, 0.93, 1 },
                new[] { "#4f5a9a", "#6a64a6", "#8a74b0", "#b884b4", "#e59bb0", "#ffbf96", "#ffdca0" });
            double d = Hypot(x - SUNX, (y - SUNY) * 1.4);
            return Mix(c, Hex("#ffdca0"), Clamp(1 - d / 48) * 0.6);
        }

        void Paint()
        {
            // 1. sky
            for (int y = 0; y < HOR; y++) for (int x = 0; x < W; x++) Put(x, y, Sky(x, y), SKY);

            // 2. far hills either side of the mountain, hazy lavender
            for (int x = 0; x < W; x++)
            {
                double ft = FarTop(x);
                for (int y = (int)Math.Floor(ft); y < HOR; y++) Put(x, y, Mix(Hex("#7a6aa0"), Hex("#5c5490"), Clamp((y - ft) / 8)), HILL);
            }

            // 3. the mountain: concave slopes, a notched crater rim, snow fingers
            for (int x = 0; x < W; x++)
            {
                double lit = Clamp((x - CX + 8) / 50.0), sl = SnowLine(x);
                for (int y = Math.Max(0, (int)Math.Floor(MountainTop(x))); y < HOR; y++)
                {
                    if (y < sl)
                    {
                        double n = Noise2(x / 1.5, y / 3.0, 5);
                        double ridge = Math.Sin((x - CX) * 0.55 + y * 0.05) > 0.55 && x < CX + 30 ? 0.25 : 0;
                        Put(x, y, Mix(Mix(Hex("#c7c3e6"), Hex("#fff2ea"), Clamp(lit * 1.3 - ridge + (n - 0.5) * 0.3)), Hex("#e6dcf0"), 0.15), MTN);
                    }
                    else
                    {
                        double k = Clamp((y - sl) / 26), n = Fbm(x / 4.0, y / 4.0, 3, 13);
                        double[] c = Mix(Hex("#565a98"), Hex("#8a76ae"), Clamp(lit * 0.9 + (n - 0.5) * 0.4));
                        c = Mix(c, Hex("#857ab4"), Clamp(k * 0.5) * (1 - lit * 0.5));  // haze near the foot
                        Put(x, y, c, MTN);
                    }
                }
            }
            // nearer low hills over the mountain foot
            for (int x = 0; x < W; x++)
            {
                double nt = NearTop(x);
                for (int y = (int)Math.Floor(nt); y < HOR; y++) Put(x, y, Mix(Hex("#4a4678"), Hex("#3e4280"), Clamp((y - nt) / 4)), HILL);
            }

            // 4. lake: the scene mirrored, darker with depth, plus a broken sun path
            for (int y = HOR; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    int sy = Math.Max(0, (int)JsRound(HOR - 1 - (y - HOR) * 1.5)), i = (sy * W + x) * 3;
                    double k = (double)(y - HOR) / (H - HOR);
                    double[] c = { rgb[i], rgb[i + 1], rgb[i + 2] };
                    c = Mix(c, Hex("#2b2f5c"), 0.12 + 0.3 * k);
                    double sd = Math.Abs(x - SUNX) / (3 + k * 10), band = Noise2(x / 6.0, y * 0.8, 3);
                    if (sd < 1 && band > 0.45) c = Mix(c, Hex("#ffd48a"), (1 - sd) * 0.75);
                    Put(x, y, c, LAKE);
                }

            // 5. the pine point on the left shore, and its reflection
            for (int x = 0; x < 62; x++)
                for (int y = (int)Math.Floor(PointTop(x)); y < 69 - x / 30.0; y++) Put(x, y, Hex("#1c1a33"), POINT);
            foreach (int[] pine in Pines)
            {
                int px = pine[0], ph = pine[1];
                double baseY = PointTop(px);
                for (int j = 0; j < ph; j++)
                {
                    int y = (int)JsRound(baseY - j), wd = Math.Max(0, (int)JsRound((ph - j) * 0.28 + (j % 3 == 0 ? 1 : 0)));
                    for (int x = px - wd; x <= px + wd; x++) Put(x, y, x > px + wd - 1 && wd > 1 ? Hex("#2c3a48") : Hex("#1e2a3a"), POINT);
                }
            }
            for (int x = 0; x < 62; x++)
                for (int y = HOR; y < H; y++)
                {
                    int sy = (int)JsRound(HOR - 1 - (y - HOR) * 1.5) + 2;
                    if (sy >= 0 && layer[sy * W + x] == POINT)
                    {
                        int i = (y * W + x) * 3;
                        Put(x, y, Mix(new[] { rgb[i], rgb[i + 1], rgb[i + 2] }, Hex("#110f22"), 0.75), LAKE);
                    }
                }

            // 6. near shore at the bottom
            for (int x = 0; x < W; x++)
                for (int y = (int)Math.Floor(Shore(x)); y < H; y++)
                    Put(x, y, Fbm(x / 3.0, y / 3.0, 2, 31) > 0.55 ? Hex("#2a2440") : Hex("#110f22"), FORE);
        }

        void Put(double fx, double fy, double[] c, byte l)
        {
            int x = (int)JsRound(fx), y = (int)JsRound(fy);
            if (x < 0 || y < 0 || x >= W || y >= H) return;
            int i = y * W + x;
            rgb[i * 3] = c[0]; rgb[i * 3 + 1] = c[1]; rgb[i * 3 + 2] = c[2];
            layer[i] = l;
        }

        #endregion

        #region The moving parts

        // Blend colour c by a at x,y, only where the painted layer is at or behind upTo
        void Add(double fx, double fy, double[] c, double a, int upTo = 255)
        {
            int x = (int)JsRound(fx), y = (int)JsRound(fy);
            if (x < 0 || y < 0 || x >= W || y >= H) return;
            int i = y * W + x;
            if (layer[i] > upTo) return;
            work[i * 3] = Lerp(work[i * 3], c[0], a);
            work[i * 3 + 1] = Lerp(work[i * 3 + 1], c[1], a);
            work[i * 3 + 2] = Lerp(work[i * 3 + 2], c[2], a);
        }

        void Animate(double t)
        {
            Array.Copy(rgb, work, rgb.Length);
            double[] dark = Hex("#2a2440"), ink = Hex("#110f22"), white = Hex("#fff2ea");

            // sun rising behind the hills on the right
            for (int y = SUNY - SUNR - 1; y <= SUNY + SUNR; y++)
                for (int x = SUNX - SUNR - 1; x <= SUNX + SUNR + 1; x++)
                {
                    double d = Hypot(x - SUNX, y - SUNY);
                    if (d < SUNR) Add(x, y, d < SUNR - 1.5 ? Hex("#fff8e0") : Hex("#ffeab0"), 1, SKY);
                }

            // mist drifting along the mountain foot
            for (int y = 50; y < HOR; y++)
                for (int x = 0; x < W; x++)
                {
                    double n = Fbm(x / 26.0 - t * 0.05, y / 3.5, 3, 7);
                    double a = Clamp((n - 0.45) * 2.4) * 0.55 * Math.Sin((y - 50) / 16.0 * Math.PI);
                    if (a > 0.03) Add(x, y, Hex("#f0c0c8"), a, MTN);
                }

            // cranes: three birds crossing, long necks out, slow wingbeats
            double ct = ((t + 8) % 34) / 34, cx0 = Lerp(W + 30, -30, ct), cy0 = 9 + Math.Sin(ct * 5) * 2;
            for (int k = 0; k < Cranes.Length; k++)
            {
                double x = JsRound(cx0 + Cranes[k][0]), y = JsRound(cy0 + Cranes[k][1] + Math.Sin(t * 0.9 + k) * 0.8);
                double f = Math.Sin(t * 3.2 + k * 1.3);
                for (int j = -2; j <= 3; j++) Add(x + j, y, white, 1);                   // body
                for (int j = 3; j <= 6; j++) Add(x - j, y - (j > 5 ? 1 : 0), white, 1);  // neck and head
                Add(x - 7, y - 1, Hex("#ffd48a"), 1);
                Add(x + 4, y + 1, dark, 1); Add(x + 5, y + 1, dark, 0.8);              // trailing legs
                double tip = JsRound(-5 * f);
                for (int j = 0; j <= 4; j++) Add(x + j * 0.5, y + JsRound(tip * j / 4), j > 2 ? dark : white, 1);
                Add(x - 1, y + JsRound(tip / 3), white, 1);
            }

            // ripple lines moving toward the shore, longer as they come near
            for (int i = 0; i < 30; i++)
            {
                double p = (ripplePh[i] + t * 0.035) % 1, y = JsRound(HOR + 2 + (H - HOR - 6) * Math.Pow(p, 1.5));
                double len = 3 + 22 * p, x0 = rippleU[i] * (W + 30) - 15 + Math.Sin(t * 0.2 + rippleU[i] * 9) * 4;
                double[] lc = Math.Abs(x0 - SUNX) < 14 + p * 10 ? Hex("#ffd48a") : Hex("#8a6c98");
                for (double x = JsRound(x0 - len / 2); x < x0 + len / 2; x++) { Add(x, y, lc, 0.6, LAKE); Add(x, y + 1, Hex("#1f2348"), 0.45, LAKE); }
            }

            // a small boat poling across, with its reflection and wake
            double bx = JsRound(Lerp(-24, W + 24, ((t + 30) % 70) / 70)), by = 86;
            double[] wake = Hex("#8a6c98");
            for (int j = 1; j < 14; j++)
            {
                Add(bx - 12 - j * 1.6, by + 1 + j / 3, wake, 0.7 - j * 0.04, LAKE);
                Add(bx - 12 - j * 1.6, by - j / 4, wake, 0.5 - j * 0.03, LAKE);
            }
            for (int x = -11; x <= 11; x++)
            {
                Add(bx + x, by, ink, 1);
                if (Math.Abs(x) < 10) Add(bx + x, by + 1, ink, 1);
                if (Math.Abs(x) < 8) Add(bx + x, by + 2, ink, 1);
                Add(bx + x, by + 4, ink, 0.45, LAKE);
            }
            Add(bx - 12, by - 1, ink, 1); Add(bx + 12, by - 1, ink, 1); Add(bx - 13, by - 2, ink, 1);
            for (int x = -6; x <= -1; x++) for (int y = 1; y <= 3; y++) Add(bx + x, by - y, ink, 1);  // cabin
            for (int j = 1; j <= 7; j++) { Add(bx + 3, by - j, ink, 1); if (j < 5) Add(bx + 4, by - j, ink, 1); }  // boatman
            for (int x = 1; x <= 6; x++) Add(bx + x, by - 8, ink, 1);
            Add(bx + 3, by - 9, ink, 1); Add(bx + 4, by - 9, ink, 1);  // hat
            double lean = Math.Sin(t * 0.8) * 2;
            for (int j = 0; j < 16; j++) Add(bx + 5 + lean * (j / 12.0) - j * 0.35, by - 4 + j, ink, 0.9);  // pole
            for (int j = 1; j <= 6; j++) Add(bx + 2, by + 3 + j, ink, 0.35, LAKE);

            // reeds on the right shore swaying in the breeze
            for (int i = 0; i < 20; i++)
            {
                double baseY = Shore(reedX[i]) + 1, sway = Math.Sin(t * 1.1 + reedPh[i]) * 1.6 + Math.Sin(t * 0.37 + reedX[i] * 0.1) * 1.2;
                for (int j = 0; j < reedH[i]; j++)
                {
                    double k = j / reedH[i];
                    Add(reedX[i] + sway * k * k, baseY - j, k > 0.8 ? dark : ink, 1);
                }
            }
        }

        #endregion

        #region Helpers (from kit.js)

        static double Clamp(double v, double a = 0, double b = 1) { return v < a ? a : v > b ? b : v; }
        static double Lerp(double a, double b, double t) { return a + (b - a) * t; }
        static double Smooth(double t) { return t * t * (3 - 2 * t); }
        static double Hypot(double x, double y) { return Math.Sqrt(x * x + y * y); }
        static double JsRound(double v) { return Math.Floor(v + 0.5); }  // JavaScript's Math.round

        static double[] Mix(double[] a, double[] b, double t)
        {
            return new[] { Lerp(a[0], b[0], t), Lerp(a[1], b[1], t), Lerp(a[2], b[2], t) };
        }

        static double[] Hex(string hex)
        {
            double[] v;
            if (!HexCache.TryGetValue(hex, out v))
            {
                v = new double[] { Convert.ToInt32(hex.Substring(1, 2), 16), Convert.ToInt32(hex.Substring(3, 2), 16), Convert.ToInt32(hex.Substring(5, 2), 16) };
                HexCache[hex] = v;
            }
            return v;
        }

        static double[] Ramp(double t, double[] at, string[] colors)
        {
            t = Clamp(t);
            for (int i = 1; i < at.Length; i++)
                if (t <= at[i]) return Mix(Hex(colors[i - 1]), Hex(colors[i]), (t - at[i - 1]) / (at[i] - at[i - 1] == 0 ? 1 : at[i] - at[i - 1]));
            return Hex(colors[colors.Length - 1]);
        }

        static double Hash2(int x, int y, int s)
        {
            int h = unchecked((int)(long)((double)x * 374761393 + (double)y * 668265263 + (double)s * 2147483647));
            h = unchecked((h ^ (int)((uint)h >> 13)) * 1274126177);
            return (uint)(h ^ (int)((uint)h >> 16)) / 4294967296.0;
        }

        // 2D value noise in 0..1
        static double Noise2(double x, double y, int seed)
        {
            int xi = (int)Math.Floor(x), yi = (int)Math.Floor(y);
            double xf = Smooth(x - xi), yf = Smooth(y - yi);
            double a = Hash2(xi, yi, seed), b = Hash2(xi + 1, yi, seed);
            double c = Hash2(xi, yi + 1, seed), d = Hash2(xi + 1, yi + 1, seed);
            return Lerp(Lerp(a, b, xf), Lerp(c, d, xf), yf);
        }

        // Fractal noise: a few octaves of Noise2, in 0..1
        static double Fbm(double x, double y, int octaves, int seed)
        {
            double v = 0, amp = 0.5, f = 1, norm = 0;
            for (int i = 0; i < octaves; i++)
            {
                v += amp * Noise2(x * f, y * f, seed + i * 17);
                norm += amp; amp *= 0.5; f *= 2;
            }
            return v / norm;
        }

        #endregion
    }
}
