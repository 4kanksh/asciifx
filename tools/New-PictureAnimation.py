"""Turn a GIF, animated WebP or still image into an AsciiFX picture animation.

    python tools/New-PictureAnimation.py <image> <name> [--width 36] [--description "..."]

It writes ~/.config/asciifx/animations/<name>/<name>.picture (the pixels of every frame) and <name>.psd1 (its
settings), so `<name>` and `desktop-<name>` work like the built-in animations in new PowerShell windows.
Needs Python 3 with Pillow (pip install pillow). Near-white and transparent pixels become see-through.
"""
import argparse
import os
from collections import Counter
from PIL import Image

parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
parser.add_argument("image")
parser.add_argument("name", help="command name: lowercase letters and digits, e.g. cat")
parser.add_argument("--width", type=int, default=36, help="width in pixels, which is also characters (default 36)")
parser.add_argument("--description", help="shown by `asciifx` and in help")
parser.add_argument("--keep-white", action="store_true", help="keep white pixels instead of making them see-through")
parser.add_argument("--out", help="folder to write to (default ~/.config/asciifx/animations/<name>)")
args = parser.parse_args()

if not args.name.isalnum() or not args.name.islower() or not args.name[0].isalpha():
    parser.error("name must be lowercase letters and digits, starting with a letter")

def is_background(p):
    r, g, b, a = p
    return a < 128 or (not args.keep_white and r > 235 and g > 235 and b > 235)

image = Image.open(args.image)
frames, durations = [], []
for i in range(getattr(image, "n_frames", 1)):
    image.seek(i)
    frames.append(image.convert("RGBA"))
    durations.append(image.info.get("duration") or 90)

# One crop box around the drawing in every frame, so the frames line up
left, top, right, bottom = frames[0].size[0], frames[0].size[1], 0, 0
for f in frames:
    px = f.load()
    for y in range(f.size[1]):
        for x in range(f.size[0]):
            if not is_background(px[x, y]):
                left, top, right, bottom = min(left, x), min(top, y), max(right, x + 1), max(bottom, y + 1)
if right <= left:
    parser.error("the image looks empty (everything is white or transparent); try --keep-white")
frames = [f.crop((left, top, right, bottom)) for f in frames]

w, h = frames[0].size
width = max(4, args.width)
height = round(h * width / w)
height += height % 2  # two pixels per character row

def shrink(img):
    """Each output pixel takes the most common color in its block, if the drawing covers at least 40% of it."""
    px = img.load()
    rows = []
    for ty in range(height):
        row = []
        for tx in range(width):
            block = [px[x, y] for y in range(ty * h // height, max(ty * h // height + 1, (ty + 1) * h // height))
                              for x in range(tx * w // width, max(tx * w // width + 1, (tx + 1) * w // width))]
            drawn = [p for p in block if not is_background(p)]
            if len(drawn) >= 0.4 * len(block):
                r, g, b = Counter(p[:3] for p in drawn).most_common(1)[0][0]
                row.append("%02x%02x%02x" % (r, g, b))
            else:
                row.append(".")
        rows.append(" ".join(row))
    return rows

out = args.out or os.path.join(os.path.expanduser("~"), ".config", "asciifx", "animations", args.name)
os.makedirs(out, exist_ok=True)
frame_ms = max(20, round(sum(durations) / len(durations)))

with open(os.path.join(out, args.name + ".picture"), "w", encoding="utf-8", newline="\n") as f:
    f.write("asciifx-picture 1\n%d %d %d %d\n" % (width, height, len(frames), frame_ms))
    for frame in frames:
        f.write("\n".join(shrink(frame)) + "\n")

description = (args.description or "%s (picture)" % args.name).replace("'", "''")
with open(os.path.join(out, args.name + ".psd1"), "w", encoding="utf-8", newline="\n") as f:
    f.write("""@{
    Name        = '%(name)s'
    Description = '%(description)s'
    Picture     = '%(name)s.picture'

    # On the desktop it gets a window just big enough for the picture; -Size makes it bigger or smaller
    Desktop = @{ Layout = 'box'; Cols = %(width)d; Rows = %(rows)d; FontSize = 10; Outline = $false }
    Preview = @{ Cols = %(width)d; Rows = %(rows)d; Seconds = 2; Fps = 12 }
}
""" % {"name": args.name, "description": description, "width": width, "rows": height // 2})

print("Made '%s': %d frames, %dx%d pixels (%d characters x %d rows), %d ms per frame" %
      (args.name, len(frames), width, height, width, height // 2, frame_ms))
print("Saved in %s" % out)
print("Open a new PowerShell window, then try:  %s   or   desktop-%s -Position top-right" % (args.name, args.name))
