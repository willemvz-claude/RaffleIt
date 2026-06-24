# Phase 0 spike: can a generated track be exported and AI-driven at all?

This is the de-risking spike for the AC Track Generator project. Before building
any real OSM ingestion, it answers the two riskiest unknowns:

1. Can a KN5 file be produced **headlessly** (no GUI, no user clicking "Export"),
   driving the open-source `moppius/blender-assetto-corsa-tools` add-on from a
   generated Blender Python script?
2. Can a **hand-written `fast_lane.ai`** (there is no open-source generator to
   copy from) get Assetto Corsa's AI to actually drive the track?

It does this by generating a synthetic flat "stadium oval" (no OSM data involved)
and running it through the full pipeline: centerline -> road mesh -> KN5 export
-> AI line -> minimal track package.

## What's been verified in this sandbox

Run from `AcTrackGenerator/`:

```
dotnet run --project tools/Phase0Spike/Phase0Spike.csproj -- --output-dir /tmp/phase0-spike-output
```

This was executed against a real headless Blender 4.0.2 install and the actual
`third_party/blender-assetto-corsa-tools` submodule checkout, and confirmed:

- The add-on imports and registers cleanly under `blender --background` (no
  window manager), once invoked via the staged-module workaround in
  `AddonStaging` (see below).
- Calling `KN5FileWriter` directly (bypassing the `ExportKN5` operator) avoids
  the failure mode this project's docs originally flagged as the biggest
  implementation risk: the operator's success/failure reporting calls
  `bpy.ops.kn5.report_message('INVOKE_DEFAULT', ...)`, which calls
  `window_manager.invoke_popup` - there is no window manager in `--background`
  mode, so that call would throw, landing in the operator's `except` block,
  which then **deletes the KN5 file it had just finished writing**. Calling the
  writer class directly sidesteps the operator (and the popup) entirely.
- The resulting file starts with the correct `sc6969` KN5 magic header followed
  by a structurally correct file_version/texture-count/material-count/material-name
  sequence (verified by inspecting the raw bytes, not just trusting the writer).
- `dotnet test` passes across all three test projects, including an integration
  test (`Ac.Export.Kn5Pipeline.Tests`) that actually shells out to Blender.

### The hyphenated-module-name problem (and its fix)

`blender-assetto-corsa-tools`'s repository root is itself the importable Python
package (its `__init__.py` lives at the repo root, not inside a subfolder). A
plain `git clone` names that folder `blender-assetto-corsa-tools` - which Python
can't `import` directly, since `import blender-assetto-corsa-tools` is a syntax
error (hyphens aren't valid in identifiers). `Ac.Export.Kn5Pipeline.AddonStaging`
works around this by copying the add-on into a temp directory under the valid
identifier `blender_assetto_corsa_tools` before generating the import script.
This is transparent to `BlenderScriptBuilder`, which only ever deals in
"parent directory + valid module name".

### Coordinate system

All of this project's own geometry code (`Geometry.*`, `Ac.Domain`,
`Ac.Export.AiLine`) works in **AC world space**: Y-up, meters, using this
project's own `Vec3`/`Vec2` types. Blender is Z-up. The swizzle is applied in
exactly one place, `BlenderScriptBuilder`'s generated `to_blender()` helper:

```
to_blender(x, y, z) = (x, -z, y)
```

This was derived from, and cross-checked against, the add-on's own
`exporter_utils.convert_vector3` (which converts the other direction, Blender
-> AC, when writing the KN5): `convert_vector3((x, y, z)) = (x, z, -y)`.
Solving for the inverse confirms the formula above.

### Mesh winding

`Geometry.MeshGen.RoadRibbonMeshBuilder` builds the road ribbon's triangles as
`(left0, left1, right1)` and `(left0, right1, right0)`, with
`Right = Cross(Up, Forward)` (see `CenterlinePoint.Right`). For a centerline
segment running along +X with `Forward = (1,0,0)`, `Right = Cross((0,1,0),
(1,0,0)) = (0,0,-1)`, which puts `right` at -Z and `left` at +Z. Working through
the cross product for both triangles confirms the face normal points along +Y
(verified in `Geometry.MeshGen.Tests.RoadRibbonMeshBuilderTests`, which checks
`Vec3.Cross(v1-v0, v2-v0).Y > 0` for every triangle in the generated oval). Since
the AC->Blender swizzle has determinant +1 (it's a pure axis permutation with one
sign flip cancelled by the handedness change, not a reflection), winding computed
in AC space carries over correctly once swizzled into Blender space.

## What this sandbox *cannot* verify

This container has no Windows, no Steam, no GPU, and no Assetto Corsa install,
so the actual goal of the spike - **does the AI car complete a lap** - has not
been confirmed in-game. That step needs to happen on your own machine:

1. Run the command above (or point `--addon-dir` / `--blender-path` at your own
   checkout/Blender install if you're not running this from inside the repo's
   `third_party` submodule, or Blender isn't on PATH).
2. Copy the resulting folder (`<output-dir>/spike_oval/`) into
   `<your AC install>/content/tracks/spike_oval/`.
3. Launch Assetto Corsa, start a Quick Race / Practice session on "spike_oval"
   with at least one AI opponent.
4. Confirm: the track loads, you can drive on the road surface (it should
   already register as a high-grip "ROAD" surface, both via the `1ROAD` mesh
   name and the bundled `data/surfaces.ini`), and the AI opponent actually
   drives around the oval rather than sitting still or driving off into the
   grass.

If the AI fails to move at all, the first things to check are whether
`ai/fast_lane.ai` was copied correctly (binary file, don't let any tooling
mangle line endings) and whether the track folder name matches the `name`
field expectations AC has internally - try renaming the KN5/folder pair to
match exactly if in doubt.

## Known gaps / simplifications in this spike

- The track is a flat, two-straight oval with no elevation, no kerbs, no
  pit lane mesh, and only a single grip-1.0 driving surface - it exists only to
  exercise the pipeline, not to be a real track.
- The AI line (`Ac.Export.AiLine.AiSplineBuilder`) follows the centerline
  exactly (no racing-line offset toward apexes) and uses a simple multi-pass
  backward braking sweep. It's deliberately not a "good" line.
- `ui/preview.png` and `ui/outline.png` are 1x1 placeholder PNGs, not real
  screenshots - track quality/UI polish is a later phase's concern.
- `pit_lane.ai` is not generated; only `fast_lane.ai` is required for this
  spike's question.
