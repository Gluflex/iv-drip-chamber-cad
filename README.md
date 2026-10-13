# IV drip chamber CAD

Ready-to-use 3D models of a gravity IV infusion drip chamber (macrodrip, 20 drops ≈ 1 mL) in six outer diameters from 14 to 24 mm, plus the parametric source for any other size.

Use them as reference geometry for clamps, sensors and enclosures that mount on a drip chamber, for fit checks, renders and teaching.

![The six published diameters](docs/lineup.png)

## Download

GitHub previews the STL files in the browser. Click a link, then use *Download raw file*.

| Outer Ø | STEP (device) | STEP (with fluid and drops) | STL (device) |
|---|---|---|---|
| 14 mm | [step](models/d14/iv-drip-chamber-d14.step) | [step](models/d14/iv-drip-chamber-d14-with-fluid.step) | [stl](models/d14/iv-drip-chamber-d14.stl) |
| 16 mm | [step](models/d16/iv-drip-chamber-d16.step) | [step](models/d16/iv-drip-chamber-d16-with-fluid.step) | [stl](models/d16/iv-drip-chamber-d16.stl) |
| 18 mm | [step](models/d18/iv-drip-chamber-d18.step) | [step](models/d18/iv-drip-chamber-d18-with-fluid.step) | [stl](models/d18/iv-drip-chamber-d18.stl) |
| 20 mm | [step](models/d20/iv-drip-chamber-d20.step) | [step](models/d20/iv-drip-chamber-d20-with-fluid.step) | [stl](models/d20/iv-drip-chamber-d20.stl) |
| 22 mm | [step](models/d22/iv-drip-chamber-d22.step) | [step](models/d22/iv-drip-chamber-d22-with-fluid.step) | [stl](models/d22/iv-drip-chamber-d22.stl) |
| 24 mm | [step](models/d24/iv-drip-chamber-d24.step) | [step](models/d24/iv-drip-chamber-d24-with-fluid.step) | [stl](models/d24/iv-drip-chamber-d24.stl) |

The STEP files are assemblies with named, coloured parts. The STL is the device as one mesh, every part watertight.

## Onshape

Open the [public Onshape document](https://cad.onshape.com/documents/036ee7c603fcfc9991b4e10a/w/f269ac826d1c214e2cdabac3/e/6dd47121a96d592e15f63fe8). Pick the diameter from the *Chamber outer diameter* list at the top of the feature tree, then right-click a part or the Part Studio tab and choose *Export* (STEP, STL, Parasolid and more). A free Onshape account is enough, no parametrising needed. To change other dimensions, use *Make a copy* and edit the feature.

To use the feature in your own documents, create a Feature Studio, keep the two header lines Onshape generates, paste [`onshape/iv_drip_chamber.fs`](onshape/iv_drip_chamber.fs) below them and commit. *IV drip chamber* then appears at the right end of the Part Studio toolbar. Parameters: outer diameter (12–40 mm), wall, chamber length, drop former length, tubing length, fluid level.

## What is modelled

<img src="docs/section.png" align="right" width="110" alt="Section through the 20 mm chamber">

- **Spike** with a bevelled tip and two lumens: fluid to the drop former, air to the side vent
- **Air vent** with membrane and slotted cap
- **Top cap** with finger flange, grip ribs and a socket for the chamber tube
- **Drop former**, inner bore 1.1 mm (20 drops ≈ 1 mL class)
- **Chamber tube**, transparent, wall 1 mm
- **Bottom cap** with funnel, perforated particle filter and an outlet socket
- **Tubing** 2.9 × 4.1 mm
- **Fluid and drops** (0.05 mL each), only in the *with fluid* STEP

Units are millimetres. The chamber axis is Z with the spike pointing up, and the chamber tube starts at z = 0. Spike, vent, outlet and tubing stay the same for every diameter; caps, flange, funnel and filter follow the chamber.

| Dimension | Value |
|---|---|
| Chamber tube length | 63 mm |
| Overall height (spike tip to tubing end) | 164–169 mm |
| Spike shaft | Ø 5.4–6.4 mm, 62° bevel |
| Drop former length | 13 mm |

<br clear="right">

## Regenerate or make other sizes

```
pip install -r requirements.txt
python src/drip_chamber.py --od 19.5
python src/drip_chamber.py --od 14 16 18 20 22 24 --length 70 --drop-former 15
```

Files land in `models/d<diameter>/`. The script stops if a part is not a valid solid or its STL is not watertight.

## Limitations

- The dimensions are typical values chosen to match common ISO 8536-4 style gravity sets. They are **not** measured from, or based on drawings of, any manufacturer's product. Measure the set you actually use before relying on a fit.
- The perforated disk stands in for the filter mesh; the vent membrane is a plain disk.
- **Not for clinical use.** These models are reference geometry for design and education, not a medical device design.

## Related

[Dripito](https://github.com/Gluflex/iv-flow-monitor) is an open IV flow monitor that clips onto drip chambers of 14–24 mm, the range these models cover.

## License

© 2026 Leandro Catarci.

- Models and source (`models/`, `src/`, `onshape/`): [CERN-OHL-P v2](LICENSE-CERN-OHL-P-2.0.md)
- Images and documentation: [CC BY 4.0](LICENSE-CC-BY-4.0.md)
