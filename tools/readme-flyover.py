"""Turn the frames --flyover-out wrote into the README's GIF and still.

    uv run --python 3.12 --with pillow tools/readme-flyover.py FRAMES_DIR OUT.gif OUT.png

Every frame is scaled to --width (default 480) and cut to --colours (default
64), which keeps the GIF under 1 MB; it plays at --fps (default 10) and
loops. The still is the frame at --still (a share of
the way through; default 0.24, over the first town) at the frames' full size.
tools/readme-flyover.sh runs the game and then this.
"""
import argparse
import pathlib
import sys

from PIL import Image


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("frames", type=pathlib.Path)
    parser.add_argument("gif", type=pathlib.Path)
    parser.add_argument("still", type=pathlib.Path)
    parser.add_argument("--width", type=int, default=480)
    parser.add_argument("--fps", type=int, default=10)
    parser.add_argument("--colours", type=int, default=64)
    parser.add_argument("--still", type=float, default=0.24, dest="still_at")
    args = parser.parse_args()

    paths = sorted(args.frames.glob("frame-*.png"))
    if not paths:
        print(f"no frame-*.png in {args.frames}", file=sys.stderr)
        return 1

    first = Image.open(paths[0])
    height = round(first.height * args.width / first.width)
    frames = []
    for path in paths:
        image = Image.open(path).convert("RGB").resize((args.width, height), Image.LANCZOS)
        # octree keeps the small bright things -- border caps, nation colours,
        # autumn leaves -- that a median cut gives up to the ground
        frames.append(image.quantize(colors=args.colours, method=Image.Quantize.FASTOCTREE, dither=Image.Dither.NONE))

    args.gif.parent.mkdir(parents=True, exist_ok=True)
    frames[0].save(
        args.gif,
        save_all=True,
        append_images=frames[1:],
        duration=round(1000 / args.fps),
        loop=0,
        optimize=True,
    )

    still = paths[min(len(paths) - 1, int(len(paths) * args.still_at))]
    args.still.parent.mkdir(parents=True, exist_ok=True)
    Image.open(still).convert("RGB").save(args.still, optimize=True)

    print(f"{args.gif}: {len(frames)} frames, {args.width}x{height}, {args.gif.stat().st_size / 1e6:.2f} MB")
    print(f"{args.still}: {still.name}, {args.still.stat().st_size / 1e6:.2f} MB")
    return 0


if __name__ == "__main__":
    sys.exit(main())
