using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace AsciiFX.Animations
{
    // Plays a picture: the frames of a GIF or animated WebP turned into pixels by tools/New-PictureAnimation.py.
    // Each character cell shows two pixels stacked vertically, using a half block ('▀') in the upper pixel's color
    // over a background of the lower one's. The picture is scaled up by whole steps to fill the space it's given.
    //
    // The .picture file is plain text:
    //   asciifx-picture 1
    //   <width> <height> <frames> <milliseconds per frame>
    //   then <height> lines per frame, each with <width> pixels: rrggbb, or . for transparent
    public class Picture : IBackgroundAnimation
    {
        const char Upper = '▀', Lower = '▄', Full = '█';

        readonly int width, height, frameMs;
        readonly int[][] frames;  // per frame: palette index per pixel, -1 = transparent
        readonly int[] palette;
        readonly double speed;
        int w, h, scale, left, top;
        int[] scratch;

        public Picture(Options o)
        {
            string file = o.Text("File", "");
            if (!File.Exists(file)) throw new FileNotFoundException("Picture file not found: " + file);
            string[] lines = File.ReadAllLines(file);
            if (lines.Length < 2 || !lines[0].StartsWith("asciifx-picture")) throw new InvalidDataException("Not an AsciiFX picture: " + file);

            string[] header = lines[1].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            width = int.Parse(header[0]); height = int.Parse(header[1]);
            int count = int.Parse(header[2]);
            frameMs = Math.Max(20, int.Parse(header[3]));
            speed = Math.Max(0.05, o.Number("Speed", 1));

            Dictionary<int, int> index = new Dictionary<int, int>();
            List<int> colors = new List<int>();
            frames = new int[count][];
            for (int f = 0; f < count; f++)
            {
                frames[f] = new int[width * height];
                for (int y = 0; y < height; y++)
                {
                    string[] pixels = lines[2 + f * height + y].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int x = 0; x < width; x++)
                    {
                        if (x >= pixels.Length || pixels[x] == ".") { frames[f][y * width + x] = -1; continue; }
                        int rgb = int.Parse(pixels[x], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                        int i;
                        if (!index.TryGetValue(rgb, out i)) { i = colors.Count; index[rgb] = i; colors.Add(rgb); }
                        frames[f][y * width + x] = i;
                    }
                }
            }
            palette = colors.Count > 0 ? colors.ToArray() : new int[] { 0xffffff };
        }

        public int[] Palette { get { return palette; } }
        public int FrameMs { get { return frameMs; } }

        public void Resize(int cols, int rows)
        {
            w = cols; h = rows;
            scratch = new int[w * h];
            // Biggest whole-number scale that fits (cells are one pixel wide and two pixels tall), centered
            scale = Math.Max(1, Math.Min(w / width, rows * 2 / height));
            left = (w - width * scale) / 2;
            top = (rows * 2 - height * scale) / 2;
        }

        public void Frame(double t, char[] chars, int[] colors)
        {
            Frame(t, chars, colors, scratch);
        }

        public void Frame(double t, char[] chars, int[] colors, int[] backgrounds)
        {
            int[] frame = frames[(int)(t * 1000 * speed / frameMs) % frames.Length];
            for (int cy = 0; cy < h; cy++)
                for (int cx = 0; cx < w; cx++)
                {
                    int k = cy * w + cx;
                    int upper = PixelAt(frame, cx, cy * 2), lower = PixelAt(frame, cx, cy * 2 + 1);
                    backgrounds[k] = -1;
                    if (upper < 0 && lower < 0) { chars[k] = ' '; colors[k] = -1; }
                    else if (upper == lower) { chars[k] = Full; colors[k] = upper; }
                    else if (lower < 0) { chars[k] = Upper; colors[k] = upper; }
                    else if (upper < 0) { chars[k] = Lower; colors[k] = lower; }
                    else { chars[k] = Upper; colors[k] = upper; backgrounds[k] = lower; }
                }
        }

        int PixelAt(int[] frame, int cx, int py)
        {
            int x = cx - left, y = py - top;
            if (x < 0 || y < 0) return -1;
            x /= scale; y /= scale;
            return x < width && y < height ? frame[y * width + x] : -1;
        }
    }
}
