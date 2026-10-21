# /// script
# requires-python = ">=3.10"
# dependencies = [
#     "usd-recorder @ git+https://github.com/Preponderous-Software/usd-recorder@v0.1.0",
# ]
# ///
"""Turn a Beyond Nations history (JSON Lines from tools/BeyondNations.HistoryExport) into a
time-sampled OpenUSD stage.

    uv run --python 3.12 tools/history-to-usd.py history.jsonl out/history.usda \\
        --stride 800 --chunk-size 200 --flatten

One time code is one sampled tick (``tick // stride``). The stage holds:

``/World``                    extras:tick, extras:day, extras:seconds, extras:pawns, extras:settlements,
                              extras:nations, extras:nationTable (JSON of the live nations) and
                              events:history (every event since the previous sample, one per line)
``/World/Entities/e_<id>``    one per pawn and settlement (``--kinds``), written by usd-recorder:
                              time-sampled translate, visibility and displayColor, extras:kind,
                              extras:nation, extras:name; its ``geom`` is the entity's primary
                              Core primitive (Capsule, Cylinder, Cube or Sphere) at Core's size
``/World/Ground/c_<x>_<z>``   one mesh per generated chunk, invisible until the tick it was
                              generated, with a time-sampled colour per tile

Tile colour is Core's own tile colour, tinted with the colour of the nation whose settlement is
nearest when that settlement is within ``settlementJoinRange``: the same rule a pawn uses to
decide which nation to join (PawnBehaviorCalculator: JOIN_NATION when the nearest settlement is
inside the join range). Core has no territory of its own; this is that rule drawn on the map.

The stage is Y-up like Core, one unit per metre. Chunked recordings (``--chunk-size``) are
stitched with value clips by usd-recorder; ``--flatten`` also writes ``<stem>_flat.usdc`` with
every clip baked in, which is what Blender should import.
"""

from __future__ import annotations

import argparse
import json
import math
import sys
from collections.abc import Iterator
from pathlib import Path

from pxr import Gf, Sdf, Usd, UsdGeom, Vt
from usd_recorder import Recorder, flatten
from usd_recorder.clips import topology_path

FORMAT = "beyond-nations-history/1"
DEFAULT_KINDS = ("pawn", "settlement")
TERRITORY_TINT = 0.6  # how much of the nation colour a territory tile takes
GROUND_SOFTEN = 0.5  # Core tiles are (0, random, 0); pull them towards a mid green
MID_GREEN = (0.18, 0.36, 0.14)
INVISIBLE, VISIBLE = UsdGeom.Tokens.invisible, UsdGeom.Tokens.inherited


def read_history(path: Path) -> Iterator[dict]:
    with path.open(encoding="utf-8") as handle:
        for number, line in enumerate(handle, 1):
            line = line.strip()
            if line:
                try:
                    yield json.loads(line)
                except json.JSONDecodeError as error:
                    raise SystemExit(f"{path}:{number}: not JSON: {error}") from error


def mix(a, b, t):
    return tuple(x + (y - x) * t for x, y in zip(a, b))


class Ground:
    """Chunk meshes with per-tile colours that change when the settlements do."""

    def __init__(self, join_range: float):
        self.join_range = join_range
        self.chunks: dict[str, dict] = {}  # name -> {tiles, first, colors: {time: [...]}}

    def add_chunk(self, record: dict, time: int) -> None:
        name = f"c_{record['x']}_{record['z']}".replace("-", "n")
        if name in self.chunks:
            return
        tiles = record["tiles"]
        base = [mix(MID_GREEN, (r, g, b), GROUND_SOFTEN) for _, _, _, r, g, b in tiles]
        self.chunks[name] = {"tiles": tiles, "base": base, "first": time, "samples": {}, "last": None}

    def update(self, time: int, settlements: list[tuple[float, float, tuple]], only_new: bool) -> None:
        """Colour chunks for the settlements alive at ``time``: every chunk when the
        settlements changed, otherwise only chunks not coloured yet. A sample is written only
        where a chunk's colours changed (plus a hold key one time code earlier)."""
        reach = self.join_range * self.join_range
        for chunk in self.chunks.values():
            if only_new and chunk["last"] is not None:
                continue
            colors = []
            for (x, z, *_), base in zip(chunk["tiles"], chunk["base"]):
                best, owner = reach, None
                for sx, sz, color in settlements:
                    d = (sx - x) ** 2 + (sz - z) ** 2
                    if d <= best:
                        best, owner = d, color
                colors.append(base if owner is None else mix(base, owner, TERRITORY_TINT))
            colors = [tuple(round(c, 4) for c in rgb) for rgb in colors]
            if colors != chunk["last"]:
                if chunk["last"] is not None and time - 1 not in chunk["samples"]:
                    chunk["samples"][time - 1] = chunk["last"]  # hold: no fade between samples
                chunk["samples"][time] = colors
                chunk["last"] = colors

    def write(self, path: Path, start: int, end: int, up_axis: str, fps: float) -> None:
        stage = Usd.Stage.CreateNew(str(path))
        # A sublayer whose timeCodesPerSecond differs from the root's is retimed by the
        # ratio when composed, so it must match the recording's.
        stage.SetTimeCodesPerSecond(fps)
        stage.SetFramesPerSecond(fps)
        UsdGeom.SetStageUpAxis(stage, up_axis)
        UsdGeom.SetStageMetersPerUnit(stage, 1.0)
        stage.SetStartTimeCode(start)
        stage.SetEndTimeCode(end)
        UsdGeom.Scope.Define(stage, "/World/Ground")
        for name, chunk in sorted(self.chunks.items()):
            mesh = UsdGeom.Mesh.Define(stage, f"/World/Ground/{name}")
            points, counts, indices = [], [], []
            for x, z, size, *_ in chunk["tiles"]:
                h = size / 2.0
                i = len(points)
                # Tiles are Core's ground cubes (scale x 1 x scale, centred on y = 0); this is
                # their top face, which is what a pawn stands on.
                points += [(x - h, 0.5, z - h), (x - h, 0.5, z + h), (x + h, 0.5, z + h), (x + h, 0.5, z - h)]
                counts.append(4)
                indices += [i, i + 1, i + 2, i + 3]
            mesh.GetPointsAttr().Set(Vt.Vec3fArray([Gf.Vec3f(*p) for p in points]))
            mesh.GetFaceVertexCountsAttr().Set(counts)
            mesh.GetFaceVertexIndicesAttr().Set(indices)
            mesh.GetSubdivisionSchemeAttr().Set(UsdGeom.Tokens.none)
            mesh.GetDoubleSidedAttr().Set(True)
            xs = [p[0] for p in points]
            zs = [p[2] for p in points]
            mesh.GetExtentAttr().Set([Gf.Vec3f(min(xs), 0.5, min(zs)), Gf.Vec3f(max(xs), 0.5, max(zs))])
            primvar = mesh.CreateDisplayColorPrimvar(UsdGeom.Tokens.uniform)
            for time, colors in sorted(chunk["samples"].items()):
                primvar.Set(Vt.Vec3fArray([Gf.Vec3f(*c) for c in colors]), max(time, start))
            visibility = mesh.GetVisibilityAttr()
            first = chunk["first"]
            if first > start:
                visibility.Set(INVISIBLE, start)
                visibility.Set(INVISIBLE, first - 1)
            visibility.Set(VISIBLE, max(first, start))
        stage.GetRootLayer().Save()


def shape_geometry(stage: Usd.Stage, scope: str, shapes: dict[str, dict]) -> None:
    """Give each recorded entity Core's own primitive and size (Desktop's PrimitiveMesh.cs: a
    unit cube, a unit-diameter sphere, and a cylinder and capsule one unit across and two
    tall, each scaled by the appearance part's scale) in place of usd-recorder's cube."""
    for name, shape in shapes.items():
        prim = stage.GetPrimAtPath(f"{scope}/{name}/geom")
        if not prim:
            continue
        sx, sy, sz = shape["scale"]
        ox, oy, oz = shape["offset"]
        kind = shape["prim"]
        prim.RemoveProperty("size")
        prim.RemoveProperty("extent")
        if kind == "capsule":
            prim.SetTypeName("Capsule")
            geom = UsdGeom.Capsule(prim)
            geom.CreateAxisAttr(UsdGeom.Tokens.y)
            geom.CreateRadiusAttr(0.5)
            geom.CreateHeightAttr(1.0)  # the straight part; plus two 0.5 caps = 2 tall
            extent = [(-0.5, -1, -0.5), (0.5, 1, 0.5)]
        elif kind == "cylinder":
            prim.SetTypeName("Cylinder")
            geom = UsdGeom.Cylinder(prim)
            geom.CreateAxisAttr(UsdGeom.Tokens.y)
            geom.CreateRadiusAttr(0.5)
            geom.CreateHeightAttr(2.0)
            extent = [(-0.5, -1, -0.5), (0.5, 1, 0.5)]
        elif kind == "sphere":
            prim.SetTypeName("Sphere")
            geom = UsdGeom.Sphere(prim)
            geom.CreateRadiusAttr(0.5)
            extent = [(-0.5, -0.5, -0.5), (0.5, 0.5, 0.5)]
        else:
            prim.SetTypeName("Cube")
            geom = UsdGeom.Cube(prim)
            geom.CreateSizeAttr(1.0)
            extent = [(-0.5, -0.5, -0.5), (0.5, 0.5, 0.5)]
        geom.CreateExtentAttr(Vt.Vec3fArray([Gf.Vec3f(*e) for e in extent]))
        xform = UsdGeom.Xformable(prim)
        xform.ClearXformOpOrder()
        if (ox, oy, oz) != (0, 0, 0):
            xform.AddTranslateOp().Set(Gf.Vec3d(ox, oy, oz))
        xform.AddScaleOp().Set(Gf.Vec3f(sx, sy, sz))
        attr = prim.CreateAttribute("beyondNations:primitive", Sdf.ValueTypeNames.Token, custom=True)
        attr.Set(kind)


def convert(args: argparse.Namespace) -> Path:
    history = Path(args.history)
    out = Path(args.out)
    records = read_history(history)
    header = next(records, None)
    if not header or header.get("t") != "header" or header.get("format") != FORMAT:
        raise SystemExit(f"{history}: not a {FORMAT} file (run tools/BeyondNations.HistoryExport)")
    if args.stride % header["every"]:
        raise SystemExit(f"--stride {args.stride} must be a multiple of the export's --every {header['every']}")
    kinds = set(args.kinds.split(","))
    tps = header["ticksPerSecond"]
    ground = Ground(float(header["settlementJoinRange"]))

    positions: dict[int, list] = {}  # last known position per entity (frames are sparse)
    shapes: dict[str, dict] = {}  # entity prim name -> primary appearance part
    colors: dict[int, tuple] = {}
    names: dict[int, str] = {}
    pending_events: list[str] = []
    pending_chunks: list[dict] = []
    last_settlements = None
    first_time = last_time = None
    counts = {"samples": 0, "entities": set(), "events": 0}

    recorder = Recorder(
        out,
        time_codes_per_second=args.fps,
        chunk_size=args.chunk_size,
        up_axis="Y",
        meters_per_unit=1.0,
        geometry="cube",
        metadata={
            "beyondNations:format": FORMAT,
            "beyondNations:seed": header["seed"],
            "beyondNations:ticks": header["ticks"],
            "beyondNations:stride": args.stride,
            "beyondNations:ticksPerSecond": tps,
        },
    )
    with recorder as rec:
        for record in records:
            kind = record["t"]
            if kind == "chunk":
                pending_chunks.append(record)
                continue
            if kind == "event":
                pending_events.append(record.get("description") or record["kind"])
                counts["events"] += 1
                continue
            if kind != "frame":
                continue

            for entity in record["entities"]:
                eid = entity["id"]
                if "pos" in entity:
                    positions[eid] = entity["pos"]
                if "parts" in entity:
                    names[eid] = entity.get("name", "")
                    colors[eid] = tuple(entity["parts"][0]["color"])
                    if entity["kind"] in kinds:
                        shapes[f"e_{eid}"] = entity["parts"][0]

            tick = record["tick"]
            if tick % args.stride and tick != header["ticks"]:
                continue
            time = math.ceil(tick / args.stride)
            if first_time is None:
                first_time = time
            last_time = time

            for chunk in pending_chunks:
                ground.add_chunk(chunk, time)
            pending_chunks.clear()

            entities = {}
            pawns = 0
            settlements = []
            nations = {n["id"]: n for n in record["nations"]}
            for entity in record["entities"]:
                eid = entity["id"]
                if entity["kind"] == "pawn":
                    pawns += 1
                if entity["kind"] == "settlement" and entity.get("nation") in nations:
                    x, _, z = positions[eid]
                    settlements.append((x, z, tuple(nations[entity["nation"]]["color"])))
                # A pawn inside a settlement is not drawn by the game either.
                if entity["kind"] not in kinds or entity.get("visible", True) is False:
                    continue
                nation = entity.get("nation", 0)
                entities[eid] = {
                    "position": tuple(positions[eid]),
                    "color": colors[eid],
                    "kind": entity["kind"],
                    "nation": nation,
                    "name": names.get(eid, ""),
                }
                counts["entities"].add(eid)

            ground.update(time, settlements, only_new=settlements == last_settlements)
            last_settlements = settlements

            table = [
                {"id": n["id"], "name": n["name"], "color": n["color"], "members": n["members"]}
                for n in record["nations"]
            ]
            rec.record(
                time,
                entities=entities,
                extras={
                    "tick": tick,
                    "day": record.get("day", 0),
                    "seconds": tick / tps,
                    "pawns": pawns,
                    "settlements": len(settlements),
                    "nations": len(record["nations"]),
                    "nationTable": json.dumps(table, separators=(",", ":")),
                },
                events={"history": pending_events},
            )
            pending_events = []
            counts["samples"] += 1

    if first_time is None:
        raise SystemExit(f"{history}: no frames at a multiple of --stride {args.stride}")

    ground_path = out.with_name(out.stem + ".ground.usda")
    ground.write(ground_path, first_time, last_time, "Y", args.fps)
    root = Sdf.Layer.FindOrOpen(str(out))
    root.subLayerPaths.append("./" + ground_path.name)
    root.Save()

    # Geometry is static, so it goes in the layer that holds the entity prims: the topology
    # layer when chunked, the recording itself when not.
    topology = topology_path(out) if args.chunk_size else out
    stage = Usd.Stage.Open(str(topology))
    shape_geometry(stage, "/World/Entities", shapes)
    stage.GetRootLayer().Save()

    print(
        f"{out}: {counts['samples']} samples (time codes {first_time}..{last_time}, "
        f"stride {args.stride} ticks), {len(counts['entities'])} entities, "
        f"{len(ground.chunks)} chunks, {counts['events']} events",
        file=sys.stderr,
    )
    if args.flatten:
        flat = out.with_name(out.stem + "_flat.usdc")
        flatten(out, flat, binary=True)
        print(f"{flat}: flattened", file=sys.stderr)
        return flat
    return out


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    parser.add_argument("history", help="JSON Lines from tools/BeyondNations.HistoryExport")
    parser.add_argument("out", help="stage to write (.usda; with --chunk-size, the stitched root)")
    parser.add_argument("--stride", type=int, default=250, help="ticks per time code (default 250)")
    parser.add_argument("--chunk-size", type=int, default=None, help="time codes per chunk layer")
    parser.add_argument("--kinds", default=",".join(DEFAULT_KINDS), help="entity kinds to record")
    parser.add_argument("--fps", type=float, default=30.0, help="time codes per second (default 30)")
    parser.add_argument("--flatten", action="store_true", help="also write <stem>_flat.usdc")
    convert(parser.parse_args(argv))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
