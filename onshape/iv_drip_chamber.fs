// IV drip chamber: parametric custom feature for Onshape.
// Paste everything below these comment lines into a new Feature Studio, UNDER the two
// lines Onshape generates itself ("FeatureScript NNNN;" and the common.fs import).
// Same geometry as src/drip_chamber.py. Units mm, axis Z, spike up, chamber bottom at z = 0.

const DC_OD_BOUNDS = { (millimeter) : [12, 20, 40] } as LengthBoundSpec;
const DC_WALL_BOUNDS = { (millimeter) : [0.5, 1, 3] } as LengthBoundSpec;
const DC_LENGTH_BOUNDS = { (millimeter) : [30, 63, 150] } as LengthBoundSpec;
const DC_DROP_FORMER_BOUNDS = { (millimeter) : [5, 13, 40] } as LengthBoundSpec;
const DC_FILL_BOUNDS = { (millimeter) : [8, 23, 120] } as LengthBoundSpec;
const DC_TUBING_BOUNDS = { (millimeter) : [5, 44, 300] } as LengthBoundSpec;

// Fixed geometry (mm): ISO-style spike, hub, vent, outlet and tubing do not scale with the chamber.
const DC_CAP_WALL = 0.9;
const DC_SOCKET = 6.0;
const DC_DRIP_TUBE_R = 1.6;
const DC_DRIP_BORE_R = 0.55;
const DC_SPIKE_R_BASE = 3.2;
const DC_SPIKE_R_TIP = 2.7;
const DC_SPIKE_BEVEL = 62;            // degrees from horizontal
const DC_HUB_R = 5.0;
const DC_VENT_R = 3.0;
const DC_VENT_LEN = 7.0;
const DC_OUTLET_R = 3.2;
const DC_TUBING_RO = 2.05;
const DC_TUBING_RI = 1.45;
const DC_CONE = 0.9467;               // funnel slope dz/dr

annotation { "Feature Type Name" : "IV drip chamber", "Feature Type Description" : "Parametric infusion drip chamber with spike, vent, filter and outlet" }
export const ivDripChamber = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Chamber outer diameter" }
        isLength(definition.od, DC_OD_BOUNDS);

        annotation { "Name" : "Chamber wall" }
        isLength(definition.wall, DC_WALL_BOUNDS);

        annotation { "Name" : "Chamber length" }
        isLength(definition.length, DC_LENGTH_BOUNDS);

        annotation { "Name" : "Drop former length" }
        isLength(definition.dropFormer, DC_DROP_FORMER_BOUNDS);

        annotation { "Name" : "Tubing length" }
        isLength(definition.tubing, DC_TUBING_BOUNDS);

        annotation { "Name" : "Show fluid and drops", "Default" : true }
        definition.showFluid is boolean;

        if (definition.showFluid)
        {
            annotation { "Name" : "Fluid level above chamber bottom" }
            isLength(definition.fill, DC_FILL_BOUNDS);
        }
    }
    {
        const ro = definition.od / millimeter / 2;
        const ri = ro - definition.wall / millimeter;
        if (ri < 4)
            throw regenError("Chamber inner radius must be at least 4 mm", ["od", "wall"]);
        const rc = ro + DC_CAP_WALL;
        const zb = 0;
        const zt = definition.length / millimeter;
        const ceil = zt + 1;
        const tipZ = ceil - definition.dropFormer / millimeter;
        const flange0 = zt + 14;
        const flangeR = rc + 0.1;
        const spikeTip = zt + 44;
        const ventZ = zt + 10.5;
        const cav = ceil + 6;
        const zi = zb - 2.9 - (ri - 1.5) * DC_CONE;      // funnel bottom (inner)
        const zo = zi - 6;                            // tubing socket top
        const zend = zi - 11;                         // outlet end
        const xEnd = -DC_HUB_R - DC_VENT_LEN;

        // --- Top cap: socket, ceiling, drop former, hub, flange, spike -------------------
        const capRev = dcRevolveProfile(context, id + "cap", [
                    [0, tipZ], [DC_DRIP_TUBE_R, tipZ], [DC_DRIP_TUBE_R, ceil], [ri, ceil], [ri, zt], [ro, zt],
                    [ro, zt - DC_SOCKET], [rc, zt - DC_SOCKET], [rc, ceil + 1.5], [DC_HUB_R, ceil + 4.5], [DC_HUB_R, flange0],
                    [flangeR, flange0], [flangeR, flange0 + 2], [DC_SPIKE_R_BASE + 0.8, flange0 + 2],
                    [DC_SPIKE_R_BASE, flange0 + 3], [DC_SPIKE_R_TIP, spikeTip], [0, spikeTip]]);
        fCylinder(context, id + "ventBoss", { "bottomCenter" : vector(-DC_HUB_R + 0.5, 0, ventZ) * millimeter,
                    "topCenter" : vector(xEnd, 0, ventZ) * millimeter, "radius" : DC_VENT_R * millimeter });
        var ribs = [];
        const r0 = DC_HUB_R - 0.5;
        const r1 = flangeR - 0.6;
        for (var k = 0; k < 8; k += 1)
        {
            const a = k * 45 * degree;
            var poly = [];
            for (var c in [[r0, -0.5], [r1, -0.5], [r1, 0.5], [r0, 0.5]])
                poly = append(poly, [c[0] * cos(a) - c[1] * sin(a), c[0] * sin(a) + c[1] * cos(a)]);
            ribs = append(ribs, poly);
        }
        const ribQ = dcExtrudePolys(context, id + "ribs",
            plane(vector(0, 0, flange0 - 1.2) * millimeter, vector(0, 0, 1), vector(1, 0, 0)), ribs, 1.2);
        const capParts = qUnion([capRev, qCreatedBy(id + "ventBoss", EntityType.BODY), ribQ]);
        opBoolean(context, id + "capUnion", { "tools" : capParts, "operationType" : BooleanOperationType.UNION });

        // Fluid path: spike lumen -> cavity -> central bore -> drop former. Air path: lumen -> vent.
        dcCylZ(context, id + "fluidLumen", 0.6, cav, spikeTip + 1, 1.0, 0);
        dcCylZ(context, id + "cavity", 1.8, cav, cav + 1.5, 0, 0);
        dcCylZ(context, id + "bore", DC_DRIP_BORE_R, tipZ - 1, cav + 0.5, 0, 0);
        dcCylZ(context, id + "airLumen", 0.45, ventZ, spikeTip + 1, -1.2, 0);
        dcCylX(context, id + "airBore", 0.5, -1.2, xEnd - 1, ventZ);
        dcCylX(context, id + "ventBore", DC_VENT_R - 0.7, -DC_HUB_R - 2, xEnd - 1, ventZ);
        const t = tan(DC_SPIKE_BEVEL * degree);
        dcExtrudePolys(context, id + "bevel", plane(vector(0, 6, 0) * millimeter, vector(0, -1, 0), vector(1, 0, 0)),
            [[[4, spikeTip + (4 - DC_SPIKE_R_TIP) * t], [-4, spikeTip + (-4 - DC_SPIKE_R_TIP) * t], [-4, spikeTip + 3], [4, spikeTip + 3]]], 12);
        const capCutters = qUnion([qCreatedBy(id + "fluidLumen", EntityType.BODY), qCreatedBy(id + "cavity", EntityType.BODY),
                    qCreatedBy(id + "bore", EntityType.BODY), qCreatedBy(id + "airLumen", EntityType.BODY),
                    qCreatedBy(id + "airBore", EntityType.BODY), qCreatedBy(id + "ventBore", EntityType.BODY),
                    qCreatedBy(id + "bevel" + "ext", EntityType.BODY)]);
        opBoolean(context, id + "capCut", { "targets" : capParts, "tools" : capCutters, "operationType" : BooleanOperationType.SUBTRACTION });
        dcFinish(context, capParts, "Spike, cap and drop former", color(0.95, 0.95, 0.93));

        // --- Vent membrane and cap ---------------------------------------------------------
        dcCylX(context, id + "ventMembrane", DC_VENT_R - 0.7, xEnd + 1.6, xEnd + 1.1, ventZ);
        dcFinish(context, qCreatedBy(id + "ventMembrane", EntityType.BODY), "Vent membrane", color(1, 1, 1));

        dcCylX(context, id + "ventCap", DC_VENT_R + 0.6, xEnd + 1.0, xEnd - 1.5, ventZ);
        dcCylX(context, id + "ventCapBore", DC_VENT_R + 0.01, xEnd + 1.1, xEnd - 0.6, ventZ);
        var slotQ = [];
        for (var j = 0; j < 3; j += 1)
        {
            const dy = (j - 1) * 1.2;
            fCuboid(context, id + ("slot" ~ j), { "corner1" : vector(xEnd - 2.0, dy - 0.25, ventZ - 1.6) * millimeter,
                        "corner2" : vector(xEnd, dy + 0.25, ventZ + 1.6) * millimeter });
            slotQ = append(slotQ, qCreatedBy(id + ("slot" ~ j), EntityType.BODY));
        }
        var ventCap = qCreatedBy(id + "ventCap", EntityType.BODY);
        opBoolean(context, id + "ventCapCut", { "targets" : ventCap,
                    "tools" : qUnion(append(slotQ, qCreatedBy(id + "ventCapBore", EntityType.BODY))),
                    "operationType" : BooleanOperationType.SUBTRACTION });
        dcFinish(context, ventCap, "Vent cap", color(0.45, 0.70, 0.90));

        // --- Chamber body --------------------------------------------------------------------
        const body = dcRevolveProfile(context, id + "body", [[ri, zb], [ro, zb], [ro, zt], [ri, zt]]);
        dcFinish(context, body, "Drip chamber", color(0.85, 0.92, 0.97, 0.35));

        // --- Bottom cap: socket, funnel, outlet, particle filter ----------------------------
        const bottom = dcRevolveProfile(context, id + "bottom", [
                    [ro, zb + DC_SOCKET], [rc, zb + DC_SOCKET], [rc, zb - 3.5], [DC_OUTLET_R, zi - 1], [DC_OUTLET_R, zend], [DC_TUBING_RO, zend],
                    [DC_TUBING_RO, zo], [1.5, zo], [1.5, zi], [ri, zb - 2.9], [ri, zb], [ro, zb]]);
        dcParticleFilter(context, id + "filter", ri - 0.05, zb - 2.5);
        dcFinish(context, bottom, "Bottom cap", color(0.95, 0.95, 0.93));

        // --- Tubing --------------------------------------------------------------------------
        const tubeLen = definition.tubing / millimeter;
        const tubing = dcRevolveProfile(context, id + "tubing", [[DC_TUBING_RI, zo - tubeLen], [DC_TUBING_RO, zo - tubeLen], [DC_TUBING_RO, zo], [DC_TUBING_RI, zo]]);
        dcFinish(context, tubing, "Tubing", color(0.85, 0.92, 0.97, 0.35));

        // --- Fluid and drops (visual) -------------------------------------------------------
        if (definition.showFluid)
        {
            const level = min(max(definition.fill / millimeter, zb + 2), tipZ - 6);
            const g = 0.05;
            var fluid = dcRevolveProfile(context, id + "fluid", [[0, zi], [1.5 - g, zi], [ri - g, zb - 2.9], [ri - g, level], [0, level]]);
            dcCylZ(context, id + "fluidLine", DC_TUBING_RI - g, zo - tubeLen, zi + 0.1, 0, 0);
            fluid = qUnion([fluid, qCreatedBy(id + "fluidLine", EntityType.BODY)]);
            opBoolean(context, id + "fluidUnion", { "tools" : fluid, "operationType" : BooleanOperationType.UNION });
            dcFinish(context, fluid, "Fluid", color(0.55, 0.80, 1.0, 0.5));

            const dropR = (3 * 50 / (4 * PI)) ^ (1 / 3);   // 0.05 mL -> 20 drops/mL
            const hanging = dcSphereOnAxis(context, id + "dropHanging", tipZ - 1.2, 1.9);
            const falling = dcSphereOnAxis(context, id + "dropFalling", (tipZ + level) / 2 - 2, dropR);
            dcFinish(context, qUnion([hanging, falling]), "Drop", color(0.55, 0.80, 1.0, 0.6));
        }
    });

// Revolve a closed (r, z) polygon in mm about the Z axis. Returns the body query.
function dcRevolveProfile(context is Context, id is Id, pts is array) returns Query
{
    const sk = newSketchOnPlane(context, id + "sk", { "sketchPlane" : plane(vector(0, 0, 0) * millimeter, vector(0, -1, 0), vector(1, 0, 0)) });
    var p = [];
    for (var pt in pts)
        p = append(p, vector(pt[0], pt[1]) * millimeter);
    p = append(p, p[0]);
    skPolyline(sk, "poly", { "points" : p });
    skSolve(sk);
    opRevolve(context, id + "rev", { "entities" : qSketchRegion(id + "sk"),
                "axis" : line(vector(0, 0, 0) * millimeter, vector(0, 0, 1)), "angleForward" : 2 * PI * radian });
    opDeleteBodies(context, id + "del", { "entities" : qCreatedBy(id + "sk", EntityType.BODY) });
    return qCreatedBy(id + "rev", EntityType.BODY);
}

// Sphere centred on the Z axis (mm), as a revolved half disk.
function dcSphereOnAxis(context is Context, id is Id, z is number, r is number) returns Query
{
    const sk = newSketchOnPlane(context, id + "sk", { "sketchPlane" : plane(vector(0, 0, 0) * millimeter, vector(0, -1, 0), vector(1, 0, 0)) });
    skArc(sk, "arc", { "start" : vector(0, z - r) * millimeter, "mid" : vector(r, z) * millimeter, "end" : vector(0, z + r) * millimeter });
    skLineSegment(sk, "axis", { "start" : vector(0, z + r) * millimeter, "end" : vector(0, z - r) * millimeter });
    skSolve(sk);
    opRevolve(context, id + "rev", { "entities" : qSketchRegion(id + "sk"),
                "axis" : line(vector(0, 0, 0) * millimeter, vector(0, 0, 1)), "angleForward" : 2 * PI * radian });
    opDeleteBodies(context, id + "del", { "entities" : qCreatedBy(id + "sk", EntityType.BODY) });
    return qCreatedBy(id + "rev", EntityType.BODY);
}

// Extrude closed polygons (sketch coordinates in mm) along the plane normal.
function dcExtrudePolys(context is Context, id is Id, pl is Plane, polys is array, depth is number) returns Query
{
    const sk = newSketchOnPlane(context, id + "sk", { "sketchPlane" : pl });
    for (var i = 0; i < size(polys); i += 1)
    {
        var p = [];
        for (var pt in polys[i])
            p = append(p, vector(pt[0], pt[1]) * millimeter);
        p = append(p, p[0]);
        skPolyline(sk, "poly" ~ i, { "points" : p });
    }
    skSolve(sk);
    opExtrude(context, id + "ext", { "entities" : qSketchRegion(id + "sk"), "direction" : pl.normal,
                "endBound" : BoundingType.BLIND, "endDepth" : depth * millimeter });
    opDeleteBodies(context, id + "del", { "entities" : qCreatedBy(id + "sk", EntityType.BODY) });
    return qCreatedBy(id + "ext", EntityType.BODY);
}

// Perforated disk with a rim, standing in for the 15 um mesh.
function dcParticleFilter(context is Context, id is Id, r is number, z is number)
{
    const pl = plane(vector(0, 0, z) * millimeter, vector(0, 0, 1), vector(1, 0, 0));
    const sk = newSketchOnPlane(context, id + "disk", { "sketchPlane" : pl });
    skCircle(sk, "outer", { "center" : vector(0, 0) * millimeter, "radius" : r * millimeter });
    const pitch = 1.1 * max(1, r / 9);
    const rows = floor(r / (pitch * sqrt(3) / 2)) + 1;
    var n = 0;
    for (var j = -rows; j <= rows; j += 1)
    {
        const y = j * pitch * sqrt(3) / 2;
        for (var i = -rows - 1; i <= rows + 1; i += 1)
        {
            const x = i * pitch + (j % 2 != 0 ? pitch / 2 : 0);
            if (x * x + y * y < (r - 0.9) * (r - 0.9))
            {
                skCircle(sk, "h" ~ n, { "center" : vector(x, y) * millimeter, "radius" : 0.35 * millimeter });
                n += 1;
            }
        }
    }
    skSolve(sk);
    opExtrude(context, id + "diskExt", { "entities" : qSketchRegion(id + "disk", true), "direction" : vector(0, 0, 1),
                "endBound" : BoundingType.BLIND, "endDepth" : 0.4 * millimeter });

    const skRim = newSketchOnPlane(context, id + "rim", { "sketchPlane" : pl });
    skCircle(skRim, "o", { "center" : vector(0, 0) * millimeter, "radius" : r * millimeter });
    skCircle(skRim, "i", { "center" : vector(0, 0) * millimeter, "radius" : (r - 0.6) * millimeter });
    skSolve(skRim);
    opExtrude(context, id + "rimExt", { "entities" : qSketchRegion(id + "rim", true), "direction" : vector(0, 0, 1),
                "endBound" : BoundingType.BLIND, "endDepth" : 1.2 * millimeter });
    opDeleteBodies(context, id + "del", { "entities" : qUnion([qCreatedBy(id + "disk", EntityType.BODY), qCreatedBy(id + "rim", EntityType.BODY)]) });

    const filter = qUnion([qCreatedBy(id + "diskExt", EntityType.BODY), qCreatedBy(id + "rimExt", EntityType.BODY)]);
    opBoolean(context, id + "union", { "tools" : filter, "operationType" : BooleanOperationType.UNION });
    dcFinish(context, filter, "Particle filter", color(0.55, 0.75, 0.95));
}

function dcCylZ(context is Context, id is Id, r is number, z0 is number, z1 is number, x is number, y is number)
{
    fCylinder(context, id, { "bottomCenter" : vector(x, y, z0) * millimeter, "topCenter" : vector(x, y, z1) * millimeter, "radius" : r * millimeter });
}

function dcCylX(context is Context, id is Id, r is number, x0 is number, x1 is number, z is number)
{
    fCylinder(context, id, { "bottomCenter" : vector(x0, 0, z) * millimeter, "topCenter" : vector(x1, 0, z) * millimeter, "radius" : r * millimeter });
}

function dcFinish(context is Context, bodies is Query, name is string, c is Color)
{
    setProperty(context, { "entities" : bodies, "propertyType" : PropertyType.NAME, "value" : name });
    setProperty(context, { "entities" : bodies, "propertyType" : PropertyType.APPEARANCE, "value" : c });
}
