using System;

namespace AsciiFX.Animations
{
    // A spinning, lit 3D torus. Points on the surface are rotated, projected with perspective and
    // shaded by how directly they face the light; a depth buffer lets the nearest surface win.
    public class Donut : IAnimation
    {
        const string Shades = ".,-~:;=!*#$@";  // darkest (facing away from the light) to brightest

        readonly int[] palette;
        readonly double rotX, rotY, size, thickness, lightAngle, lightHeight;
        int w, h;
        double[] depth;

        public Donut(Options o)
        {
            palette = new int[] { Colors.Parse(o.Text("Color", "#cdd6f4"), 0xcdd6f4) };
            rotX = o.Number("RotationX", 2);
            rotY = o.Number("RotationY", 1);
            size = o.Number("Size", 75);
            thickness = o.Number("Thickness", 50);
            lightAngle = o.Number("LightAngle", 45);
            lightHeight = o.Number("LightHeight", 45);
        }

        public int[] Palette { get { return palette; } }
        public int FrameMs { get { return 33; } }

        public void Resize(int cols, int rows)
        {
            w = cols; h = rows;
            depth = new double[w * h];
        }

        public void Frame(double t, char[] chars, int[] colors)
        {
            for (int k = 0; k < w * h; k++) { chars[k] = ' '; colors[k] = -1; depth[k] = 0; }

            // Tube radius r around a ring of radius 1; the camera sits far enough back to keep it all in view
            double r = Math.Max(1, Math.Min(100, thickness)) / 100.0, outer = 1 + r, camera = 2.5 * outer;
            double scale = size / 100.0 * Math.Min(h, w / Cell.Aspect) / 2 * camera / outer;
            double cx = (w - 1) / 2.0, cy = (h - 1) / 2.0;

            double a = t * rotX * 0.6, b = t * rotY * 0.6;
            double cosA = Math.Cos(a), sinA = Math.Sin(a), cosB = Math.Cos(b), sinB = Math.Sin(b);

            // Light direction: angle around the screen (0 = right, 90 = up), height toward the viewer
            double az = lightAngle * Math.PI / 180, el = lightHeight * Math.PI / 180;
            double lx = Math.Cos(el) * Math.Cos(az), ly = Math.Cos(el) * Math.Sin(az), lz = -Math.Sin(el);

            // Sample finely enough that neighboring points land well under a cell apart
            double cols = scale / (camera - outer) * Cell.Aspect;
            int ringSteps = (int)Math.Ceiling(2 * Math.PI * outer * cols * 2) + 8;
            int tubeSteps = (int)Math.Ceiling(2 * Math.PI * r * cols * 2) + 8;

            for (int i = 0; i < ringSteps; i++)
            {
                double phi = 2 * Math.PI * i / ringSteps, cosP = Math.Cos(phi), sinP = Math.Sin(phi);
                for (int j = 0; j < tubeSteps; j++)
                {
                    double theta = 2 * Math.PI * j / tubeSteps, cosT = Math.Cos(theta), sinT = Math.Sin(theta);

                    // Point and surface normal on a torus lying flat in the XY plane
                    double ring = 1 + r * cosT;
                    double px = ring * cosP, py = ring * sinP, pz = r * sinT;
                    double nx = cosT * cosP, ny = cosT * sinP, nz = sinT;

                    // Spin around X, then around Y
                    double py1 = py * cosA - pz * sinA, pz1 = py * sinA + pz * cosA;
                    double px2 = px * cosB + pz1 * sinB, pz2 = pz1 * cosB - px * sinB;
                    double ny1 = ny * cosA - nz * sinA, nz1 = ny * sinA + nz * cosA;
                    double nx2 = nx * cosB + nz1 * sinB, nz2 = nz1 * cosB - nx * sinB;

                    // Perspective projection, keeping only the surface nearest the viewer
                    double ooz = 1 / (camera + pz2);
                    int x = (int)Math.Round(cx + scale * px2 * ooz * Cell.Aspect);
                    int y = (int)Math.Round(cy - scale * py1 * ooz);
                    if (x < 0 || y < 0 || x >= w || y >= h) continue;
                    int k = y * w + x;
                    if (ooz <= depth[k]) continue;
                    depth[k] = ooz;

                    double lum = nx2 * lx + ny1 * ly + nz2 * lz;
                    chars[k] = Shades[lum > 0 ? Math.Min(Shades.Length - 1, (int)(lum * Shades.Length)) : 0];
                    colors[k] = 0;
                }
            }
        }
    }
}
