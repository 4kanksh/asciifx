using System;
using System.Collections.Generic;

namespace AsciiFX.Animations
{
    // A wall of fire. Heat rises from fuel rows at the bottom, drifts with the wind and cools as it climbs,
    // while embers float up slowly and sparks shoot out of the flames.
    public class Fire : IAnimation
    {
        class Particle { public double X, Y, VX, VY, Life; public bool Spark; }

        const int Shades = 32;
        const string Charset = " .'`,:;*+xX#";  // coolest to hottest; index 0 is empty air

        static readonly int[] Blue = { 0x0a0f32, 0x142d82, 0x235ad2, 0x4696fa, 0x8ccdff, 0xdcf5ff, 0xffffff };
        static readonly int[] Mocha = { 0x181825, 0x45475a, 0xcba6f7, 0xf38ba8, 0xfab387, 0xf9e2af, 0xf5e0dc };  // Catppuccin

        readonly int[] palette;  // Shades heat colors, then the ember color, then the spark color
        readonly double intensity, wind, decay, turbulence;
        readonly int thickness;
        readonly bool embers, sparks, pulse;
        readonly Random rng = new Random();
        readonly List<Particle> particles = new List<Particle>();
        int w, h;
        double[] heat, next;
        double last;

        public Fire(Options o)
        {
            int[] stops = o.Text("Palette", "blue") == "mocha" ? Mocha : Blue;
            palette = new int[Shades + 2];
            Array.Copy(Colors.Gradient(Shades, stops), palette, Shades);
            palette[Shades] = Colors.At(0.7, stops);
            palette[Shades + 1] = stops[stops.Length - 1];

            intensity = o.Number("Intensity", 8);
            wind = o.Number("Wind", 4);
            decay = o.Number("Decay", 2.2);
            turbulence = o.Number("Turbulence", 8);
            thickness = (int)o.Number("Thickness", 3);
            embers = !o.Flag("NoEmbers");
            sparks = !o.Flag("NoSparks");
            pulse = !o.Flag("NoPulse");
        }

        public int[] Palette { get { return palette; } }
        public int FrameMs { get { return 22; } }

        public void Resize(int cols, int rows)
        {
            w = cols; h = rows;
            heat = new double[w * h];
            next = new double[w * h];
            particles.Clear();
        }

        public void Frame(double t, char[] chars, int[] colors)
        {
            double dt = Math.Min(0.1, Math.Max(0, t - last));
            last = t;

            // Refuel the bottom rows, with random hot spots and a slow pulse
            int fuelRows = Math.Max(1, Math.Min(thickness, h / 3));
            int top = h - fuelRows;
            double flicker = pulse ? 1 + 0.12 * Math.Sin(t * 3.4) + 0.05 * Math.Sin(t * 7.9) : 1;
            double fuel = Math.Min(1.0, 0.45 + intensity * 0.07) * flicker;
            for (int k = top * w; k < h * w; k++)
                heat[k] = rng.NextDouble() < 0.04 ? 1.2 : fuel * (0.65 + 0.35 * rng.NextDouble());

            // Each cell takes heat from just below it, shifted by wind and turbulence, then cools
            double cool = decay / (2.2 * 0.75 * Math.Max(1, top));
            double drift = wind * 0.12, jitter = turbulence * 0.18;
            for (int y = 0; y < top; y++)
            {
                int below = (y + 1) * w;
                for (int x = 0; x < w; x++)
                {
                    int sx = x - (int)Math.Round(drift + (rng.NextDouble() - 0.5) * jitter);
                    sx = ((sx % w) + w) % w;
                    int l = sx == 0 ? w - 1 : sx - 1, r = sx == w - 1 ? 0 : sx + 1;
                    double v = heat[below + sx] * 0.6 + heat[below + l] * 0.2 + heat[below + r] * 0.2
                               - cool * (0.4 + 1.2 * rng.NextDouble());
                    next[y * w + x] = v > 0 ? v : 0;
                }
            }
            Array.Copy(heat, top * w, next, top * w, fuelRows * w);
            double[] swap = heat; heat = next; next = swap;

            for (int k = 0; k < w * h; k++)
            {
                double v = Math.Min(1.0, heat[k]);
                if (v < 0.05) { chars[k] = ' '; colors[k] = -1; continue; }
                chars[k] = Charset[1 + (int)((v - 0.05) / 0.95 * (Charset.Length - 2))];
                colors[k] = (int)(v * (Shades - 1));
            }

            // Embers drift up slowly; sparks shoot up fast and burn out
            if (embers) Spawn(w * 0.18 * intensity / 8.0 * dt, false, top);
            if (sparks) Spawn(w * 0.06 * intensity / 8.0 * dt, true, top);
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                Particle p = particles[i];
                p.Life -= dt;
                p.VX += (rng.NextDouble() - 0.5) * turbulence * 0.8 * dt;
                p.X += p.VX * dt; p.Y += p.VY * dt;
                if (p.X < 0) p.X += w;
                if (p.X >= w) p.X -= w;
                if (p.Life <= 0 || p.Y < 0) { particles.RemoveAt(i); continue; }
                int px = Math.Min(w - 1, (int)p.X), py = (int)p.Y;
                if (py >= h) continue;
                chars[py * w + px] = p.Spark ? (rng.NextDouble() < 0.5 ? '*' : '+') : (rng.NextDouble() < 0.5 ? '.' : '\'');
                colors[py * w + px] = p.Spark ? Shades + 1 : Shades;
            }
        }

        void Spawn(double expected, bool spark, int top)
        {
            for (double n = expected; n > 0; n -= 1)
            {
                if (n < 1 && rng.NextDouble() >= n) break;
                Particle p = new Particle();
                p.Spark = spark;
                p.X = rng.NextDouble() * w;
                p.Y = top - rng.NextDouble() * top * 0.35;
                p.VX = wind * 0.8 + (rng.NextDouble() - 0.5) * 3;
                p.VY = spark ? -(14 + rng.NextDouble() * 10) : -(4 + rng.NextDouble() * 5);
                p.Life = spark ? 0.4 + rng.NextDouble() * 0.5 : 1.2 + rng.NextDouble() * 1.8;
                particles.Add(p);
            }
        }
    }
}
