"""Parametric IV infusion drip chamber (gravity macrodrip set, ISO 8536-4 style).

Same geometry as onshape/iv_drip_chamber.fs. Units mm, axis Z, spike up,
chamber bottom at z = 0. Dimensions are typical values, not taken from any
manufacturer's drawing.

    python src/drip_chamber.py --od 20                 # one diameter into models/d20/
    python src/drip_chamber.py --od 14 16 18 20 22 24  # the published set
"""

import argparse
from math import cos, pi, radians, sin, sqrt, tan
from pathlib import Path

import trimesh
from build123d import (
    Align, Axis, Box, BuildPart, BuildSketch, Color, Compound, Cylinder, Plane, Polygon,
    Pos, Rot, Sphere, export_step, export_stl, revolve,
)

ROOT = Path(__file__).resolve().parent.parent

# Fixed geometry: spike, hub, vent, outlet and tubing do not scale with the chamber.
CAP_WALL = 0.9
SOCKET = 6.0
DRIP_TUBE_R, DRIP_BORE_R = 1.6, 0.55
SPIKE_R_BASE, SPIKE_R_TIP, SPIKE_BEVEL = 3.2, 2.7, 62.0
HUB_R = 5.0
VENT_R, VENT_LEN = 3.0, 7.0
OUTLET_R = 3.2
TUBING_RO, TUBING_RI = 2.05, 1.45
CONE = 0.9467                      # funnel slope dz/dr
DROP_VOL = 50.0                    # mm^3 = 0.05 mL -> 20 drops/mL

WHITE = Color(0.95, 0.95, 0.93)
CLEAR = Color(0.85, 0.92, 0.97, 0.35)
FLUID = Color(0.55, 0.80, 1.0, 0.5)


def revolved(pts):
    """Revolve a closed (r, z) polygon about Z."""
    with BuildPart() as p:
        with BuildSketch(Plane.XZ):
            Polygon(*pts, align=None)
        revolve(axis=Axis.Z)
    return p.part


def cyl_z(r, z0, z1, x=0.0, y=0.0):
    return Pos(x, y, z0) * Cylinder(r, z1 - z0, align=(Align.CENTER, Align.CENTER, Align.MIN))


def cyl_x(r, x0, x1, z):
    return Pos((x0 + x1) / 2, 0, z) * Rot(0, 90, 0) * Cylinder(r, abs(x1 - x0))


def build_parts(od=20.0, wall=1.0, length=63.0, drop_former=13.0, tubing_len=44.0, fill=23.0):
    ro = od / 2
    ri = ro - wall
    if ri < 4:
        raise ValueError("chamber inner radius must be at least 4 mm")
    rc = ro + CAP_WALL
    zb, zt = 0.0, length
    ceil = zt + 1
    tip_z = ceil - drop_former
    flange0, flange_r = zt + 14, rc + 0.1
    spike_tip = zt + 44
    vent_z = zt + 10.5
    cav = ceil + 6
    zi = zb - 2.9 - (ri - 1.5) * CONE
    zo, zend = zi - 6, zi - 11
    x_end = -HUB_R - VENT_LEN

    # Top cap: socket, ceiling, drop former, hub, flange, spike
    cap = revolved([
        (0, tip_z), (DRIP_TUBE_R, tip_z), (DRIP_TUBE_R, ceil), (ri, ceil), (ri, zt), (ro, zt),
        (ro, zt - SOCKET), (rc, zt - SOCKET), (rc, ceil + 1.5), (HUB_R, ceil + 4.5), (HUB_R, flange0),
        (flange_r, flange0), (flange_r, flange0 + 2), (SPIKE_R_BASE + 0.8, flange0 + 2),
        (SPIKE_R_BASE, flange0 + 3), (SPIKE_R_TIP, spike_tip), (0, spike_tip),
    ])
    cap += cyl_x(VENT_R, -HUB_R + 0.5, x_end, vent_z)
    r0, r1 = HUB_R - 0.5, flange_r - 0.6
    for k in range(8):
        cap += Pos(0, 0, flange0 - 1.2) * Rot(0, 0, k * 45) * Pos((r0 + r1) / 2, 0, 0) \
            * Box(r1 - r0, 1.0, 1.2, align=(Align.CENTER, Align.CENTER, Align.MIN))
    cap -= cyl_z(0.6, cav, spike_tip + 1, x=1.0)
    cap -= cyl_z(1.8, cav, cav + 1.5)          # clear of the lumen wall (no tangency)
    cap -= cyl_z(DRIP_BORE_R, tip_z - 1, cav + 0.5)
    cap -= cyl_z(0.45, vent_z, spike_tip + 1, x=-1.2)
    cap -= cyl_x(0.5, -1.2, x_end - 1, vent_z)
    cap -= cyl_x(VENT_R - 0.7, -HUB_R - 2, x_end - 1, vent_z)
    th = radians(SPIKE_BEVEL)
    bevel = Plane(origin=(SPIKE_R_TIP, 0, spike_tip), z_dir=(-sin(th), 0, cos(th)))
    cap -= bevel * Box(24, 12, 20, align=(Align.CENTER, Align.CENTER, Align.MIN))

    # Vent membrane and slotted cap
    membrane = cyl_x(VENT_R - 0.7, x_end + 1.6, x_end + 1.1, vent_z)
    vent_cap = cyl_x(VENT_R + 0.6, x_end + 1.0, x_end - 1.5, vent_z) - cyl_x(VENT_R + 0.01, x_end + 1.1, x_end - 0.6, vent_z)
    for dy in (-1.2, 0.0, 1.2):
        vent_cap -= Pos(x_end - 1.0, dy, vent_z) * Box(2.0, 0.5, 3.2)

    body = revolved([(ri, zb), (ro, zb), (ro, zt), (ri, zt)])

    bottom = revolved([
        (ro, zb + SOCKET), (rc, zb + SOCKET), (rc, zb - 3.5), (OUTLET_R, zi - 1), (OUTLET_R, zend),
        (TUBING_RO, zend), (TUBING_RO, zo), (1.5, zo), (1.5, zi), (ri, zb - 2.9), (ri, zb), (ro, zb),
    ])

    # Perforated particle filter with rim
    fr, fz = ri - 0.05, zb - 2.5
    pitch = 1.1 * max(1.0, fr / 9)
    rows = int(fr / (pitch * sqrt(3) / 2)) + 1
    holes = []
    for j in range(-rows, rows + 1):
        y = j * pitch * sqrt(3) / 2
        for i in range(-rows - 1, rows + 2):
            x = i * pitch + (pitch / 2 if j % 2 else 0)
            if x * x + y * y < (fr - 0.9) ** 2:
                holes.append(cyl_z(0.35, fz - 0.1, fz + 0.5, x=x, y=y))
    filt = (cyl_z(fr, fz, fz + 0.4) - Compound(holes)) + (cyl_z(fr, fz, fz + 1.2) - cyl_z(fr - 0.6, fz - 0.1, fz + 1.3))

    tubing = revolved([(TUBING_RI, zo - tubing_len), (TUBING_RO, zo - tubing_len), (TUBING_RO, zo), (TUBING_RI, zo)])

    # Visual only: fluid and drops
    level = min(max(fill, zb + 2), tip_z - 6)
    g = 0.05
    fluid = revolved([(0, zi), (1.5 - g, zi), (ri - g, zb - 2.9), (ri - g, level), (0, level)]) \
        + cyl_z(TUBING_RI - g, zo - tubing_len, zi + 0.1)
    drop_r = (3 * DROP_VOL / (4 * pi)) ** (1 / 3)
    drops = Compound([Pos(0, 0, tip_z - 1.2) * Sphere(1.9), Pos(0, 0, (tip_z + level) / 2 - 2) * Sphere(drop_r)])

    return {
        "spike_cap": (cap, WHITE, True),
        "vent_membrane": (membrane, Color(1, 1, 1), True),
        "vent_cap": (vent_cap, Color(0.45, 0.70, 0.90), True),
        "chamber": (body, CLEAR, True),
        "particle_filter": (filt, Color(0.55, 0.75, 0.95), True),
        "bottom_cap": (bottom, WHITE, True),
        "tubing": (tubing, CLEAR, True),
        "fluid": (fluid, FLUID, False),
        "drops": (drops, FLUID, False),
    }


def write_stl(shape, path):
    export_stl(shape, str(path), tolerance=0.02, angular_tolerance=0.15)
    m = trimesh.load(path)
    m.merge_vertices(digits_vertex=4)
    return m


def export(od, out_dir, **kw):
    out_dir.mkdir(parents=True, exist_ok=True)
    tag = f"d{od:g}"
    parts = build_parts(od=od, **kw)
    device, meshes, report = [], [], []
    for name, (shape, color, is_device) in parts.items():
        shape.label, shape.color = name, color
        if not shape.is_valid:
            raise RuntimeError(f"{tag} {name}: invalid solid")
        if is_device:
            m = write_stl(shape, out_dir / f"_{name}.stl")
            (out_dir / f"_{name}.stl").unlink()
            if not m.is_watertight:
                raise RuntimeError(f"{tag} {name}: STL not watertight")
            meshes.append(m)
            device.append(shape)
        report.append(f"{name} {shape.volume:.0f} mm3")
    export_step(Compound(children=device, label=f"iv_drip_chamber_{tag}"), str(out_dir / f"iv-drip-chamber-{tag}.step"))
    export_step(Compound(children=[s for s, _, _ in parts.values()], label=f"iv_drip_chamber_{tag}_with_fluid"),
                str(out_dir / f"iv-drip-chamber-{tag}-with-fluid.step"))
    trimesh.util.concatenate(meshes).export(out_dir / f"iv-drip-chamber-{tag}.stl")
    bb = Compound(device).bounding_box()
    print(f"{tag}: {bb.size.X:.1f} x {bb.size.Y:.1f} x {bb.size.Z:.1f} mm | " + ", ".join(report))


if __name__ == "__main__":
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--od", type=float, nargs="+", default=[20.0], help="chamber outer diameter(s), mm")
    ap.add_argument("--wall", type=float, default=1.0)
    ap.add_argument("--length", type=float, default=63.0, help="chamber tube length, mm")
    ap.add_argument("--drop-former", type=float, default=13.0)
    ap.add_argument("--tubing", type=float, default=44.0)
    ap.add_argument("--out", type=Path, default=ROOT / "models")
    a = ap.parse_args()
    for od in a.od:
        export(od, a.out / f"d{od:g}", wall=a.wall, length=a.length, drop_former=a.drop_former, tubing_len=a.tubing)
