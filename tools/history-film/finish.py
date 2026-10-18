# /// script
# requires-python = ">=3.10"
# dependencies = ["usd-core==26.8", "pillow>=10.1", "imageio-ffmpeg>=0.5"]
# ///
"""Overlay the history onto rendered frames and encode the film.

    uv run --python 3.12 tools/history-film/finish.py out/history_flat.usdc out/frames out/film

Reads everything it prints from the stage written by tools/history-to-usd.py (``/World``'s
extras:tick, extras:day, extras:seconds, extras:pawns, extras:settlements, extras:nationTable and
events:history), draws it over each frame from tools/history-film/render.py, and writes:

* ``history.mp4``       H.264, one frame per time code
* ``history-poster.png`` the frame at ``--poster`` (fraction of the film, default 0.9)
* ``history.gif``       downscaled and thinned until it is at most ``--gif-max-mb``
"""

from __future__ import annotations

import argparse
import json
import subprocess
import tempfile
from pathlib import Path

import imageio_ffmpeg
from PIL import Image, ImageDraw, ImageFont
from pxr import Usd

# Events worth a caption; spawns and joins happen constantly and would drown them.
CAPTIONED = ("has been created", "has been founded", "is now the leader", "disbanded", "has died", "has left")
CAPTION_FRAMES = 75  # how long a caption stays up, in frames
FONT_CANDIDATES = (
    "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
    "/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf",
)
BOLD_CANDIDATES = (
    "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",
    "/usr/share/fonts/truetype/liberation/LiberationSans-Bold.ttf",
)


def font(size: int, bold: bool = False, override: str | None = None) -> ImageFont.ImageFont:
    for path in ([override] if override else []) + list(BOLD_CANDIDATES if bold else FONT_CANDIDATES):
        if path and Path(path).is_file():
            return ImageFont.truetype(path, size)
    return ImageFont.load_default(size)


def read_history(stage_path: Path) -> tuple[dict, dict[int, dict]]:
    stage = Usd.Stage.Open(str(stage_path))
    world = stage.GetPrimAtPath("/World")
    meta = stage.GetRootLayer().customLayerData
    frames = {}
    for time in range(int(stage.GetStartTimeCode()), int(stage.GetEndTimeCode()) + 1):

        def get(name: str, time: int = time):
            return world.GetAttribute(name).Get(time)

        frames[time] = {
            "tick": get("extras:tick"),
            "day": get("extras:day"),
            "seconds": get("extras:seconds"),
            "pawns": get("extras:pawns"),
            "settlements": get("extras:settlements"),
            "nations": json.loads(get("extras:nationTable") or "[]"),
            "events": [e for e in (get("events:history") or "").split("\n") if e],
        }
    return dict(meta), frames


def rgb255(color) -> tuple[int, int, int]:
    return tuple(int(max(0.0, min(1.0, c)) * 255 + 0.5) for c in color)


def overlay(image: Image.Image, meta: dict, state: dict, captions: list[tuple[str, int]], fonts) -> Image.Image:
    image = image.convert("RGBA")
    width, height = image.size
    unit = height / 720.0
    layer = Image.new("RGBA", image.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    small, medium, large = fonts
    pad = int(22 * unit)

    # Top left: what, when.
    panel_w, panel_h = int(360 * unit), int(128 * unit)
    draw.rounded_rectangle((pad - 10, pad - 8, pad + panel_w, pad + panel_h), int(10 * unit), fill=(8, 10, 16, 170))
    draw.text(
        (pad, pad),
        f"BEYOND NATIONS  ·  seed {meta.get('beyondNations:seed', '?')}",
        font=small,
        fill=(200, 205, 215, 255),
    )
    # Core's TimeOfDay counts completed days from 0; the overlay counts the day in progress.
    day = (state["day"] or 0) + 1
    draw.text((pad, pad + int(22 * unit)), f"Day {day}", font=large, fill=(255, 255, 255, 255))
    minutes = state["seconds"] / 60.0
    draw.text(
        (pad, pad + int(70 * unit)),
        f"tick {state['tick']:,}  ·  {minutes:,.0f} min of game time",
        font=small,
        fill=(200, 205, 215, 255),
    )
    draw.text(
        (pad, pad + int(94 * unit)),
        f"{len(state['nations'])} nations  ·  {state['settlements']} settlements  ·  {state['pawns']} pawns",
        font=small,
        fill=(235, 238, 245, 255),
    )

    # Top right: the nations, in founding order, each in its own colour.
    nations = state["nations"]
    if nations:
        row = int(22 * unit)
        box_w = int(230 * unit)
        x0 = width - pad - box_w
        draw.rounded_rectangle(
            (x0 - 10, pad - 8, width - pad + 10, pad + row * len(nations) + int(30 * unit)),
            int(10 * unit),
            fill=(8, 10, 16, 170),
        )
        draw.text((x0, pad), "NATIONS (members)", font=small, fill=(200, 205, 215, 255))
        for i, nation in enumerate(nations):
            y = pad + int(28 * unit) + i * row
            swatch = int(14 * unit)
            draw.rectangle(
                (x0, y + 2, x0 + swatch, y + 2 + swatch),
                fill=rgb255(nation["color"]) + (255,),
                outline=(255, 255, 255, 200),
            )
            draw.text(
                (x0 + swatch + int(8 * unit), y),
                f"{nation['name']}  ({nation['members']})",
                font=small,
                fill=(240, 242, 248, 255),
            )

    # Bottom: the latest captioned events, fading out.
    y = height - pad - int(30 * unit)
    for text, age in reversed(captions[-3:]):
        alpha = int(255 * max(0.0, 1.0 - age / CAPTION_FRAMES) ** 0.5)
        if alpha <= 0:
            continue
        bbox = draw.textbbox((0, 0), text, font=medium)
        w = bbox[2] - bbox[0]
        x = (width - w) // 2
        draw.rounded_rectangle(
            (x - 14, y - 6, x + w + 14, y + int(30 * unit)), int(8 * unit), fill=(8, 10, 16, int(alpha * 0.7))
        )
        draw.text((x, y), text, font=medium, fill=(255, 244, 214, alpha))
        y -= int(40 * unit)
    return Image.alpha_composite(image, layer).convert("RGB")


def encode_mp4(frames_dir: Path, fps: float, out: Path) -> None:
    ffmpeg = imageio_ffmpeg.get_ffmpeg_exe()
    subprocess.run(
        [
            ffmpeg,
            "-y",
            "-loglevel",
            "error",
            "-framerate",
            str(fps),
            "-i",
            str(frames_dir / "%04d.png"),
            "-c:v",
            "libx264",
            "-preset",
            "slow",
            "-crf",
            "20",
            "-pix_fmt",
            "yuv420p",
            "-movflags",
            "+faststart",
            str(out),
        ],
        check=True,
    )


def encode_gif(frames_dir: Path, fps: float, out: Path, max_mb: float) -> tuple[int, float]:
    ffmpeg = imageio_ffmpeg.get_ffmpeg_exe()
    for width, gif_fps in ((640, 12), (560, 10), (480, 10), (480, 8), (400, 8), (360, 6), (320, 6)):
        graph = (
            f"fps={gif_fps},scale={width}:-2:flags=lanczos,split[a][b];"
            "[a]palettegen=max_colors=128:stats_mode=diff[p];[b][p]paletteuse=dither=bayer:bayer_scale=4:diff_mode=rectangle"
        )
        subprocess.run(
            [
                ffmpeg,
                "-y",
                "-loglevel",
                "error",
                "-framerate",
                str(fps),
                "-i",
                str(frames_dir / "%04d.png"),
                "-filter_complex",
                graph,
                "-loop",
                "0",
                str(out),
            ],
            check=True,
        )
        size = out.stat().st_size / 1e6
        if size <= max_mb:
            return width, gif_fps
    raise SystemExit(f"{out}: still {size:.1f} MB at {width}px/{gif_fps} fps, over {max_mb} MB")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    parser.add_argument("stage")
    parser.add_argument("frames", help="frames from render.py (NNNN.png, NNNN = time code)")
    parser.add_argument("out", help="directory for history.mp4, history-poster.png, history.gif")
    parser.add_argument("--fps", type=float, default=30.0)
    parser.add_argument("--poster", type=float, default=0.9)
    parser.add_argument("--gif-max-mb", type=float, default=3.0)
    parser.add_argument("--font", default=None, help="a .ttf to use instead of DejaVu Sans")
    args = parser.parse_args()

    meta, history = read_history(Path(args.stage))
    frames_dir, out = Path(args.frames), Path(args.out)
    out.mkdir(parents=True, exist_ok=True)
    times = sorted(t for t in history if (frames_dir / f"{t:04d}.png").is_file())
    if not times:
        raise SystemExit(f"{frames_dir}: no NNNN.png frames for the stage's time codes")

    height = Image.open(frames_dir / f"{times[0]:04d}.png").size[1]
    unit = height / 720.0
    fonts = (
        font(int(16 * unit), override=args.font),
        font(int(21 * unit), True, args.font),
        font(int(40 * unit), True, args.font),
    )

    captions: list[list] = []
    with tempfile.TemporaryDirectory() as tmp:
        staged = Path(tmp)
        for index, time in enumerate(times):
            state = history[time]
            for caption in captions:
                caption[1] += 1
            for event in state["events"]:
                if any(marker in event for marker in CAPTIONED):
                    captions.append([event, 0])
            captions = [c for c in captions if c[1] < CAPTION_FRAMES]
            image = overlay(
                Image.open(frames_dir / f"{time:04d}.png"), meta, state, [tuple(c) for c in captions], fonts
            )
            image.save(staged / f"{index + 1:04d}.png")
        poster_index = max(1, round(len(times) * args.poster))
        Image.open(staged / f"{poster_index:04d}.png").save(out / "history-poster.png")
        encode_mp4(staged, args.fps, out / "history.mp4")
        width, gif_fps = encode_gif(staged, args.fps, out / "history.gif", args.gif_max_mb)

    for name in ("history.mp4", "history-poster.png", "history.gif"):
        print(f"{out / name}: {(out / name).stat().st_size / 1e6:.2f} MB")
    print(f"{len(times)} frames at {args.fps} fps = {len(times) / args.fps:.1f} s; gif {width}px at {gif_fps} fps")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
