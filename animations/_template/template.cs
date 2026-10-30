using System;

namespace AsciiFX.Animations
{
    // Starting point for a new animation: run tools/New-Animation.ps1 -Name <name> to copy this.
    // It draws a few layered waves rolling across the screen. Replace Frame with your own idea.
    //
    // The rules:
    //  - Take your settings in the constructor from Options (names match the Options list in the .psd1).
    //  - Palette lists your colors as 0xRRGGBB; Frame refers to them by index.
    //  - Resize is called before the first frame and whenever the area changes size.
    //  - Frame fills every cell: chars[k] is the character, colors[k] the palette index (-1 = empty),
    //    where k = row * cols + column. Cells are about 2.2 times taller than wide (Cell.Aspect).
    public class Template : IAnimation
    {
        const string Charset = "~-=*";

        readonly int[] palette;
        readonly double speed, waves;
        int w, h;

        public Template(Options o)
        {
            palette = Colors.Gradient(4, 0x89b4fa, 0x94e2d5);  // Catppuccin blue to teal
            speed = o.Number("Speed", 1);
            waves = Math.Max(1, o.Number("Waves", 3));
        }

        public int[] Palette { get { return palette; } }
        public int FrameMs { get { return 33; } }

        public void Resize(int cols, int rows)
        {
            w = cols; h = rows;
        }

        public void Frame(double t, char[] chars, int[] colors)
        {
            for (int k = 0; k < w * h; k++) { chars[k] = ' '; colors[k] = -1; }

            for (int i = 0; i < (int)waves; i++)
            {
                double baseline = h * (i + 1) / (waves + 1);
                for (int x = 0; x < w; x++)
                {
                    double y = baseline + Math.Sin(x * 0.12 + t * speed * (1 + i * 0.4) + i) * h * 0.12;
                    int row = (int)Math.Round(y);
                    if (row < 0 || row >= h) continue;
                    chars[row * w + x] = Charset[i % Charset.Length];
                    colors[row * w + x] = i % palette.Length;
                }
            }
        }
    }
}
