using System;
using System.Collections.Generic;

namespace AsciiFX.Animations
{
    // A rotating spiral galaxy: stars scattered along trailing spiral arms around a glowing core,
    // each twinkling on its own rhythm.
    public class Galaxy : IAnimation
    {
        class Star { public double R, Angle, Bright, TwinkleSpeed, TwinklePhase; }

        const int Shades = 32;
        const string Charset = " .:-=+*#%@";  // faintest to brightest; index 0 is empty space

        static readonly int[] Blue = { 0x0a143c, 0x1e3ca0, 0x3c78e6, 0x78b4ff, 0xc8e6ff, 0xffffff };
        static readonly int[] Magenta = { 0x280a3c, 0x6e1e96, 0xbe46dc, 0xf082f0, 0xffc8ff, 0xffffff };

        readonly int[] palette;
        readonly int arms;
        readonly double speed, size, twist, noise, glow, twinkle;
        readonly Random rng = new Random();
        int w, h;
        double radius;
        List<Star> stars;
        double[] light;

        public Galaxy(Options o)
        {
            palette = Colors.Gradient(Shades, o.Text("Palette", "blue") == "magenta" ? Magenta : Blue);
            arms = Math.Max(1, (int)o.Number("Arms", 2));
            speed = o.Number("Speed", 20);
            size = o.Number("Size", 70);
            twist = o.Number("Twist", 0.45);
            noise = o.Number("Noise", 0.15);
            glow = o.Number("Glow", 0.45);
            twinkle = o.Number("Twinkle", 0.35);
        }

        public int[] Palette { get { return palette; } }
        public int FrameMs { get { return 33; } }

        public void Resize(int cols, int rows)
        {
            w = cols; h = rows;
            light = new double[w * h];
            radius = Math.Max(2, size / 100.0 * Math.Min(h, w / Cell.Aspect) / 2);
            stars = MakeStars((int)(Math.PI * radius * radius * Cell.Aspect * 1.1));
        }

        public void Frame(double t, char[] chars, int[] colors)
        {
            Array.Clear(light, 0, light.Length);
            double cx = (w - 1) / 2.0, cy = (h - 1) / 2.0;
            double rot = t * speed * Math.PI / 180;

            // Soft glow around the core
            if (glow > 0)
            {
                int reach = (int)Math.Ceiling(radius * 0.4);
                for (int y = Math.Max(0, (int)cy - reach); y <= Math.Min(h - 1, (int)cy + reach + 1); y++)
                    for (int x = Math.Max(0, (int)(cx - reach * Cell.Aspect)); x <= Math.Min(w - 1, (int)(cx + reach * Cell.Aspect) + 1); x++)
                    {
                        double u = (x - cx) / Cell.Aspect / radius, v = (y - cy) / radius;
                        double core = glow * 5 * Math.Exp(-(u * u + v * v) / 0.02);
                        if (core > 0.02) light[y * w + x] += core;
                    }
            }

            // Every star turns with the galaxy and twinkles on its own rhythm
            foreach (Star s in stars)
            {
                double a = s.Angle + rot;
                int x = (int)Math.Round(cx + Math.Cos(a) * s.R * radius * Cell.Aspect);
                int y = (int)Math.Round(cy + Math.Sin(a) * s.R * radius);
                if (x < 0 || y < 0 || x >= w || y >= h) continue;
                double b = s.Bright * (1 - twinkle * 0.5 * (1 + Math.Sin(t * s.TwinkleSpeed + s.TwinklePhase)));
                light[y * w + x] += b;
                if (glow > 0)
                {
                    double g = b * glow * 0.15;
                    if (x > 0) light[y * w + x - 1] += g;
                    if (x < w - 1) light[y * w + x + 1] += g;
                    if (y > 0) light[(y - 1) * w + x] += g;
                    if (y < h - 1) light[(y + 1) * w + x] += g;
                }
            }

            for (int k = 0; k < w * h; k++)
            {
                double v = 1 - Math.Exp(-light[k] * 0.6);
                int idx = (int)(v * (Charset.Length - 1) + 0.5);
                chars[k] = Charset[idx];
                colors[k] = idx == 0 ? -1 : (int)(v * (Shades - 1));
            }
        }

        List<Star> MakeStars(int count)
        {
            List<Star> result = new List<Star>();
            for (int i = 0; i < Math.Max(300, count); i++)
            {
                Star s = new Star();
                if (rng.NextDouble() < 0.2)
                {
                    // Faint stars scattered between the arms
                    s.R = Math.Sqrt(rng.NextDouble());
                    s.Angle = rng.NextDouble() * 2 * Math.PI;
                    s.Bright = 0.15 + 0.25 * rng.NextDouble();
                }
                else
                {
                    // Arm stars: the further out, the more the arm has curled back (a trailing spiral)
                    int arm = rng.Next(arms);
                    s.R = 0.05 + 0.95 * Math.Pow(rng.NextDouble(), 0.85);
                    double spread = (0.08 + noise * 1.2) * (1.2 - 0.5 * s.R) * Gauss();
                    s.Angle = arm * 2 * Math.PI / arms - twist * 9 * s.R + spread;
                    s.R = Math.Max(0.02, s.R + noise * 0.08 * Gauss());
                    s.Bright = (0.35 + 0.65 * rng.NextDouble()) * (1.1 - 0.6 * s.R);
                }
                s.TwinkleSpeed = 1.5 + 4 * rng.NextDouble();
                s.TwinklePhase = rng.NextDouble() * 2 * Math.PI;
                result.Add(s);
            }
            return result;
        }

        double Gauss()
        {
            return Math.Sqrt(-2 * Math.Log(1 - rng.NextDouble())) * Math.Cos(2 * Math.PI * rng.NextDouble());
        }
    }
}
