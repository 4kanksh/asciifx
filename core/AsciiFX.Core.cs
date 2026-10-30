using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace AsciiFX
{
    // Every animation implements this. The players (terminal and desktop) own the screen;
    // an animation only decides which character and color goes in each cell.
    public interface IAnimation
    {
        // The colors the animation draws with, as 0xRRGGBB. Frame refers to them by index.
        int[] Palette { get; }

        // Milliseconds between frames that the animation looks best at
        int FrameMs { get; }

        // Called before the first frame, and again whenever the drawing area changes size
        void Resize(int cols, int rows);

        // Draws the frame at t seconds: one character and one palette index (-1 = empty) per cell, row by row
        void Frame(double t, char[] chars, int[] colors);
    }

    public static class Cell
    {
        // Terminal cells are about 2.2 times taller than they are wide; animations use this to keep circles round
        public const double Aspect = 2.2;
    }

    // An animation's settings as passed from PowerShell, with a fallback for anything missing
    public sealed class Options
    {
        readonly Dictionary<string, object> values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        public Options(IDictionary source)
        {
            if (source != null)
                foreach (DictionaryEntry e in source) values[e.Key.ToString()] = e.Value;
        }

        public double Number(string name, double fallback)
        {
            object v;
            double d;
            if (!values.TryGetValue(name, out v) || v == null) return fallback;
            return double.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture), NumberStyles.Float,
                                   CultureInfo.InvariantCulture, out d) ? d : fallback;
        }

        public bool Flag(string name)
        {
            object v;
            if (!values.TryGetValue(name, out v) || v == null) return false;
            string s = v.ToString();
            return s.Equals("True", StringComparison.OrdinalIgnoreCase) || s == "1";
        }

        public string Text(string name, string fallback)
        {
            object v;
            return values.TryGetValue(name, out v) && v != null && v.ToString() != "" ? v.ToString() : fallback;
        }
    }

    // Color helpers. Colors are plain ints, 0xRRGGBB.
    public static class Colors
    {
        // `count` colors blending smoothly through the given stops, first to last
        public static int[] Gradient(int count, params int[] stops)
        {
            int[] result = new int[count];
            for (int i = 0; i < count; i++) result[i] = At((double)i / Math.Max(1, count - 1), stops);
            return result;
        }

        // The color at position t (0..1) along the stops
        public static int At(double t, params int[] stops)
        {
            if (stops.Length == 1) return stops[0];
            double pos = Math.Max(0, Math.Min(1, t)) * (stops.Length - 1);
            int i = Math.Min((int)pos, stops.Length - 2);
            return Mix(stops[i], stops[i + 1], pos - i);
        }

        public static int Mix(int a, int b, double f)
        {
            int r = (int)(((a >> 16) & 255) + (((b >> 16) & 255) - ((a >> 16) & 255)) * f);
            int g = (int)(((a >> 8) & 255) + (((b >> 8) & 255) - ((a >> 8) & 255)) * f);
            int bl = (int)((a & 255) + ((b & 255) - (a & 255)) * f);
            return (r << 16) | (g << 8) | bl;
        }

        // "#rrggbb" (or "rrggbb") to 0xRRGGBB; anything else gives the fallback
        public static int Parse(string hex, int fallback)
        {
            int value;
            if (hex == null) return fallback;
            hex = hex.Trim().TrimStart('#');
            return hex.Length == 6 && int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value)
                ? value : fallback;
        }
    }

    // Plays an animation full-screen in the terminal until a key is pressed (or for a set time)
    public static class TerminalPlayer
    {
        [DllImport("kernel32.dll")] static extern IntPtr GetStdHandle(int handle);
        [DllImport("kernel32.dll")] static extern bool GetConsoleMode(IntPtr handle, out uint mode);
        [DllImport("kernel32.dll")] static extern bool SetConsoleMode(IntPtr handle, uint mode);

        const string Esc = "\u001b";

        public static void Run(IAnimation anim, double seconds, int fps)
        {
            // Windows consoles need escape-sequence support switched on; other terminals already have it
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                uint mode;
                IntPtr outHandle = GetStdHandle(-11);
                if (GetConsoleMode(outHandle, out mode)) SetConsoleMode(outHandle, mode | 0x0004);
            }

            string[] colorCodes = new string[anim.Palette.Length];
            for (int i = 0; i < colorCodes.Length; i++)
            {
                int c = anim.Palette[i];
                colorCodes[i] = Esc + "[38;2;" + ((c >> 16) & 255) + ";" + ((c >> 8) & 255) + ";" + (c & 255) + "m";
            }

            int frameMs = fps > 0 ? 1000 / fps : anim.FrameMs;
            Stream stdout = Console.OpenStandardOutput();
            StringBuilder sb = new StringBuilder();
            int w = -1, h = -1;
            char[] chars = null;
            int[] colors = null;

            bool oldCtrlC = false;
            try { oldCtrlC = Console.TreatControlCAsInput; Console.TreatControlCAsInput = true; } catch (IOException) { }
            Write(stdout, Esc + "[?1049h" + Esc + "[?25l");  // alternate screen, hidden cursor
            Stopwatch clock = Stopwatch.StartNew();
            try
            {
                while (seconds <= 0 || clock.Elapsed.TotalSeconds < seconds)
                {
                    if (KeyPressed()) break;
                    double t = clock.Elapsed.TotalSeconds;

                    int cw = Math.Max(10, Console.WindowWidth), ch = Math.Max(6, Console.WindowHeight);
                    if (cw != w || ch != h)
                    {
                        w = cw; h = ch;
                        chars = new char[w * h];
                        colors = new int[w * h];
                        anim.Resize(w, h);
                        Write(stdout, Esc + "[2J");
                    }

                    anim.Frame(t, chars, colors);
                    sb.Length = 0;
                    string current = null;
                    for (int y = 0; y < h; y++)
                    {
                        sb.Append(Esc).Append('[').Append(y + 1).Append(";1H");
                        for (int x = 0; x < w; x++)
                        {
                            int k = y * w + x;
                            if (colors[k] < 0) { sb.Append(' '); continue; }
                            string col = colorCodes[colors[k]];
                            if (!ReferenceEquals(col, current)) { sb.Append(col); current = col; }
                            sb.Append(chars[k]);
                        }
                    }
                    Write(stdout, sb.ToString());

                    int wait = (int)(frameMs - (clock.Elapsed.TotalSeconds - t) * 1000);
                    if (wait > 0) Thread.Sleep(wait);
                }
            }
            finally
            {
                Write(stdout, Esc + "[0m" + Esc + "[?25h" + Esc + "[?1049l");
                try { Console.TreatControlCAsInput = oldCtrlC; } catch (IOException) { }
            }
        }

        static bool KeyPressed()
        {
            try
            {
                if (!Console.KeyAvailable) return false;
                Console.ReadKey(true);
                return true;
            }
            catch (InvalidOperationException) { return false; }
        }

        static void Write(Stream stream, string text)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(text);
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush();
        }
    }
}
