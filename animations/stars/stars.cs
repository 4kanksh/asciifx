using System;
using System.Collections.Generic;

namespace AsciiFX.Animations
{
    // A quiet night sky: sparse stars that slowly twinkle, thinning toward the bottom,
    // with a faint shooting star now and then.
    public class Stars : IAnimation
    {
        class Star { public int X, Y; public double Bright, Speed, Phase; }
        class Meteor { public double X, Y, VX, VY, Born, Life; }

        // Catppuccin surface1, overlay0, subtext0 and text: dim enough to stay in the background
        static readonly int[] palette = { 0x45475a, 0x6c7086, 0xa6adc8, 0xcdd6f4 };

        readonly double density;
        readonly bool shooting;
        readonly Random rng = new Random();
        readonly List<Star> stars = new List<Star>();
        int w, h;
        Meteor meteor;
        double nextMeteor;

        public Stars(Options o)
        {
            density = Math.Max(0, o.Number("Density", 1));
            shooting = !o.Flag("NoShootingStars");
            nextMeteor = 15 + rng.NextDouble() * 30;
        }

        public int[] Palette { get { return palette; } }
        public int FrameMs { get { return 50; } }

        public void Resize(int cols, int rows)
        {
            w = cols; h = rows;
            stars.Clear();
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    if (rng.NextDouble() < 0.018 * density * (1 - 0.7 * y / (double)h))
                    {
                        Star s = new Star();
                        s.X = x; s.Y = y;
                        s.Bright = 0.25 + 0.75 * Math.Pow(rng.NextDouble(), 2.5);  // mostly faint, a few bright
                        s.Speed = 0.4 + rng.NextDouble() * 1.6;
                        s.Phase = rng.NextDouble() * 2 * Math.PI;
                        stars.Add(s);
                    }
        }

        public void Frame(double t, char[] chars, int[] colors)
        {
            for (int k = 0; k < w * h; k++) { chars[k] = ' '; colors[k] = -1; }

            foreach (Star s in stars)
            {
                double b = s.Bright * (0.6 + 0.4 * Math.Sin(t * s.Speed + s.Phase));
                int k = s.Y * w + s.X;
                chars[k] = b > 0.9 ? '*' : b > 0.65 ? '+' : b > 0.4 ? '\'' : '.';
                colors[k] = Math.Min(3, (int)(b * 4));
            }

            if (!shooting) return;
            if (meteor == null && t >= nextMeteor)
            {
                meteor = new Meteor();
                meteor.X = w * (0.1 + 0.8 * rng.NextDouble());
                meteor.Y = h * 0.4 * rng.NextDouble();
                meteor.VX = (rng.NextDouble() < 0.5 ? -1 : 1) * (35 + rng.NextDouble() * 20);  // cells per second
                meteor.VY = 9 + rng.NextDouble() * 5;
                meteor.Born = t;
                meteor.Life = 0.9 + rng.NextDouble() * 0.5;
            }
            if (meteor == null) return;

            double age = t - meteor.Born;
            if (age > meteor.Life) { meteor = null; nextMeteor = t + 25 + rng.NextDouble() * 45; return; }
            double fade = Math.Sin(Math.PI * age / meteor.Life);  // brightens, then fades out
            for (int i = 0; i < 8; i++)  // the head, then a short tail along its path
            {
                double at = age - i * 0.025;
                if (at < 0) continue;
                int x = (int)(meteor.X + meteor.VX * at), y = (int)(meteor.Y + meteor.VY * at);
                if (x < 0 || y < 0 || x >= w || y >= h) continue;
                chars[y * w + x] = i == 0 ? '*' : i > 4 ? '.' : (meteor.VX > 0 ? '\\' : '/');
                colors[y * w + x] = (int)Math.Max(0, Math.Min(3, fade * (3.5 - i * 0.45)));
            }
        }
    }
}
