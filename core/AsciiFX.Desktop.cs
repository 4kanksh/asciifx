using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace AsciiFX
{
    // Shows an animation on the desktop: above the wallpaper (static or live), below every normal window,
    // with a see-through background and clicks passing straight through. Windows only.
    public class DesktopPlayer : Form
    {
        [DllImport("user32.dll")] static extern bool SetProcessDPIAware();
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string cls, string title);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetWindowText(IntPtr hwnd, StringBuilder text, int max);
        [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr hwnd);
        [DllImport("user32.dll")] static extern IntPtr GetWindow(IntPtr hwnd, uint cmd);
        [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int cx, int cy, uint flags);

        const string TitlePrefix = "AsciiFX ";
        const int MaxBrushes = 8;  // nearby colors share a brush, so each row needs only a few text draws

        readonly IAnimation anim;
        readonly IBackgroundAnimation withBackgrounds;
        readonly int cols, rows;
        readonly char[] chars;
        readonly int[] colors, backgrounds;
        readonly char[] line;
        readonly int[] group;          // palette index -> brush
        readonly SolidBrush[] brushes;
        readonly SolidBrush[] exact;   // palette index -> its exact color, for filled blocks and backgrounds
        readonly SolidBrush outlineBrush;
        readonly bool outline;
        readonly Font font;
        readonly float cellW, cellH;
        readonly Stopwatch clock = Stopwatch.StartNew();
        int ticks;

        // layout "box": a cols x rows window placed by position/x/y.
        // layout "sky": the full screen width and the top heightPercent of the screen (100 = all of it).
        public static void Run(IAnimation anim, string name, float fontSize, string layout, int cols, int rows,
                               int heightPercent, bool outline, string position, int x, int y)
        {
            SetProcessDPIAware();

            // Size the font for the screen's scaling and measure one monospace cell
            Font font;
            float cellW;
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            {
                font = new Font("Consolas", fontSize * g.DpiX / 96f, GraphicsUnit.Pixel);
                g.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;
                cellW = g.MeasureString(new string('M', 20), font, PointF.Empty, StringFormat.GenericTypographic).Width / 20;
            }
            float cellH = (float)(cellW * Cell.Aspect);

            if (layout == "sky")
            {
                Rectangle screen = Screen.PrimaryScreen.Bounds;
                cols = (int)((screen.Width - 4) / cellW);
                rows = (int)(screen.Height * Math.Max(1, Math.Min(100, heightPercent)) / 100.0 / cellH);
                position = "sky";
            }
            Application.Run(new DesktopPlayer(anim, name, font, cellW, cellH, cols, rows, outline, position, x, y));
        }

        DesktopPlayer(IAnimation anim, string name, Font font, float cellW, float cellH, int cols, int rows,
                      bool outline, string position, int x, int y)
        {
            this.anim = anim; this.font = font; this.cellW = cellW; this.cellH = cellH;
            this.cols = Math.Max(1, cols); this.rows = Math.Max(1, rows); this.outline = outline;
            withBackgrounds = anim as IBackgroundAnimation;
            chars = new char[this.cols * this.rows];
            colors = new int[this.cols * this.rows];
            backgrounds = new int[this.cols * this.rows];
            line = new char[this.cols];
            anim.Resize(this.cols, this.rows);

            Text = TitlePrefix + name;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.FromArgb(1, 0, 1);  // keyed out, so only the characters show
            TransparencyKey = BackColor;
            DoubleBuffered = true;

            group = GroupColors(anim.Palette, out brushes);
            exact = new SolidBrush[anim.Palette.Length];
            for (int i = 0; i < exact.Length; i++)
                exact[i] = new SolidBrush(Color.FromArgb((anim.Palette[i] >> 16) & 255, (anim.Palette[i] >> 8) & 255, anim.Palette[i] & 255));
            outlineBrush = new SolidBrush(Color.FromArgb(17, 17, 27));

            ClientSize = new Size((int)(this.cols * cellW) + 4, (int)(this.rows * cellH) + 4);
            // The sky starts at the very top of the screen, so at 100% it covers all of it
            Location = position == "sky" ? Screen.PrimaryScreen.Bounds.Location
                                         : PlaceOn(Screen.PrimaryScreen.WorkingArea, position, x, y);

            Timer timer = new Timer();
            timer.Interval = Math.Max(15, anim.FrameMs);
            timer.Tick += delegate
            {
                if (ticks++ % 15 == 0) KeepOnDesktop();
                Invalidate();
            };
            timer.Start();
        }

        // Up to MaxBrushes representative colors from the palette; every palette color uses the closest one
        static int[] GroupColors(int[] palette, out SolidBrush[] brushes)
        {
            int count = Math.Min(MaxBrushes, palette.Length);
            int[] reps = new int[count];
            for (int i = 0; i < count; i++) reps[i] = palette[count == 1 ? 0 : i * (palette.Length - 1) / (count - 1)];

            int[] group = new int[palette.Length];
            for (int p = 0; p < palette.Length; p++)
            {
                int best = 0, bestDist = int.MaxValue;
                for (int r = 0; r < count; r++)
                {
                    int dr = ((palette[p] >> 16) & 255) - ((reps[r] >> 16) & 255);
                    int dg = ((palette[p] >> 8) & 255) - ((reps[r] >> 8) & 255);
                    int db = (palette[p] & 255) - (reps[r] & 255);
                    int dist = dr * dr + dg * dg + db * db;
                    if (dist < bestDist) { bestDist = dist; best = r; }
                }
                group[p] = best;
            }

            brushes = new SolidBrush[count];
            for (int r = 0; r < count; r++)
                brushes[r] = new SolidBrush(Color.FromArgb((reps[r] >> 16) & 255, (reps[r] >> 8) & 255, reps[r] & 255));
            return group;
        }

        // A named spot ("center", "top-left", "bottom", ...), or exact pixels from the top-left when x/y are given
        Point PlaceOn(Rectangle area, string position, int x, int y)
        {
            const int margin = 40;
            string p = (position ?? "center").ToLowerInvariant();
            int left = area.Left + (area.Width - Width) / 2, top = area.Top + (area.Height - Height) / 2;
            if (p.Contains("left")) left = area.Left + margin;
            if (p.Contains("right")) left = area.Right - Width - margin;
            if (p.Contains("top")) top = area.Top + margin;
            if (p.Contains("bottom")) top = area.Bottom - Height - margin;
            if (x >= 0) left = area.Left + x;
            if (y >= 0) top = area.Top + y;
            return new Point(left, top);
        }

        // Tool window (no taskbar/Alt+Tab), layered + transparent (clicks pass through), never takes focus
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x80 | 0x20 | 0x80000 | 0x08000000;
                return cp;
            }
        }

        protected override bool ShowWithoutActivation { get { return true; } }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (withBackgrounds != null) withBackgrounds.Frame(clock.Elapsed.TotalSeconds, chars, colors, backgrounds);
            else
            {
                anim.Frame(clock.Elapsed.TotalSeconds, chars, colors);
                for (int k = 0; k < backgrounds.Length; k++) backgrounds[k] = -1;
            }
            Graphics g = e.Graphics;
            g.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;  // no antialiasing, so no colored fringes

            for (int y = 0; y < rows; y++)
            {
                float top = 2 + y * cellH;
                int row = y * cols;
                FillBlocks(g, y);
                for (int x = 0; x < cols; x++) line[x] = colors[row + x] < 0 ? ' ' : chars[row + x];

                // A dark copy offset by a pixel keeps the characters readable on bright wallpapers
                if (outline) g.DrawString(new string(line), font, outlineBrush, 3, top + 1, StringFormat.GenericTypographic);

                // Then each run of same-brush characters in one go
                int start = 0;
                while (start < cols)
                {
                    int b = colors[row + start] < 0 ? -1 : group[colors[row + start]], end = start + 1;
                    while (end < cols && (colors[row + end] < 0 ? -1 : group[colors[row + end]]) == b) end++;
                    if (b >= 0)
                        g.DrawString(new string(line, start, end - start), font, brushes[b],
                                     2 + start * cellW, top, StringFormat.GenericTypographic);
                    start = end;
                }
            }
        }

        // Backgrounds, and the block characters '▀' '▄' '█', are painted as exact rectangles rather than text, so
        // pictures come out crisp in any font. Those cells are then cleared so the text pass skips them.
        void FillBlocks(Graphics g, int y)
        {
            int row = y * cols;
            int top = (int)(2 + y * cellH), middle = (int)(2 + (y + 0.5f) * cellH), bottom = (int)(2 + (y + 1) * cellH);
            for (int x = 0; x < cols; x++)
            {
                int k = row + x;
                char c = chars[k];
                bool upper = c == '▀' || c == '█', lower = c == '▄' || c == '█';
                if (backgrounds[k] < 0 && !(colors[k] >= 0 && (upper || lower))) continue;

                int left = (int)(2 + x * cellW), right = (int)(2 + (x + 1) * cellW);
                int topColor = colors[k] >= 0 && upper ? colors[k] : backgrounds[k];
                int bottomColor = colors[k] >= 0 && lower ? colors[k] : backgrounds[k];
                if (topColor >= 0) g.FillRectangle(exact[topColor], left, top, right - left, middle - top);
                if (bottomColor >= 0) g.FillRectangle(exact[bottomColor], left, middle, right - left, bottom - middle);
                if (upper || lower) colors[k] = -1;  // fully painted already
            }
        }

        // Stay just above the window that holds the desktop icons and wallpaper, below everything else.
        // Other AsciiFX windows share that layer, so only move when a normal window got in between.
        void KeepOnDesktop()
        {
            IntPtr host = FindWindowEx(IntPtr.Zero, IntPtr.Zero, "Progman", null);
            if (FindWindowEx(host, IntPtr.Zero, "SHELLDLL_DefView", null) == IntPtr.Zero)
            {
                IntPtr worker = IntPtr.Zero;
                while ((worker = FindWindowEx(IntPtr.Zero, worker, "WorkerW", null)) != IntPtr.Zero)
                    if (FindWindowEx(worker, IntPtr.Zero, "SHELLDLL_DefView", null) != IntPtr.Zero) { host = worker; break; }
            }
            StringBuilder text = new StringBuilder(64);
            for (IntPtr w = GetWindow(host, 3); w != IntPtr.Zero; w = GetWindow(w, 3))  // GW_HWNDPREV: walk upward
            {
                if (w == Handle) return;
                text.Length = 0;
                GetWindowText(w, text, 64);
                if (IsWindowVisible(w) && !text.ToString().StartsWith(TitlePrefix)) break;
            }
            IntPtr above = GetWindow(host, 3);
            if (above == IntPtr.Zero) return;
            SetWindowPos(Handle, above, 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0010);  // NOSIZE | NOMOVE | NOACTIVATE
        }
    }
}
