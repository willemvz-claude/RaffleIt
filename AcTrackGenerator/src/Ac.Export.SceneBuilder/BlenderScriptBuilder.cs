using System.Globalization;
using System.Text;
using AcTrackGenerator.Ac.Domain;
using AcTrackGenerator.Common;

namespace AcTrackGenerator.Ac.Export.SceneBuilder;

public sealed record BlenderExportScriptOptions(
    string AddonParentDirectory,
    string AddonModuleName,
    string OutputKn5Path);

/// <summary>
/// Turns a <see cref="TrackScene"/> into a self-contained Blender Python
/// script that, run via <c>blender --background --python &lt;script&gt;</c>,
/// builds the scene with <c>bpy</c> and writes a KN5 file.
///
/// The script drives the moppius/blender-assetto-corsa-tools add-on's
/// <c>KN5FileWriter</c> class directly instead of invoking its
/// <c>exporter.kn5</c> operator. The operator's success/failure path calls
/// <c>bpy.ops.kn5.report_message('INVOKE_DEFAULT', ...)</c>, which opens a
/// popup via <c>window_manager.invoke_popup</c> - that throws in
/// <c>--background</c> mode (no window manager), and the operator's except
/// block would then delete the KN5 file it had just finished writing. Calling
/// the writer class directly sidesteps that popup entirely. See
/// docs/phase0-spike.md for how this was found.
/// </summary>
public static class BlenderScriptBuilder
{
    public static string Build(TrackScene scene, BlenderExportScriptOptions options)
    {
        var sb = new StringBuilder();

        sb.AppendLine("import sys");
        sb.AppendLine("import bpy");
        sb.AppendLine();
        sb.AppendLine("# --- start from a truly empty scene, regardless of Blender's startup file ---");
        sb.AppendLine("for obj in list(bpy.data.objects):");
        sb.AppendLine("    bpy.data.objects.remove(obj, do_unlink=True)");
        sb.AppendLine("for mesh in list(bpy.data.meshes):");
        sb.AppendLine("    bpy.data.meshes.remove(mesh)");
        sb.AppendLine("for material in list(bpy.data.materials):");
        sb.AppendLine("    bpy.data.materials.remove(material)");
        sb.AppendLine();
        sb.AppendLine("# --- load the KN5 export add-on as a plain module (no addon-preferences UI) ---");
        sb.AppendLine(PyStringAssignment("_addon_parent_dir", options.AddonParentDirectory));
        sb.AppendLine("sys.path.insert(0, _addon_parent_dir)");
        sb.AppendLine($"import {options.AddonModuleName} as kn5_addon");
        sb.AppendLine("kn5_addon.register()");
        sb.AppendLine($"from {options.AddonModuleName}.exporter import KN5FileWriter, read_settings");
        sb.AppendLine();
        sb.AppendLine("# AC world space (Y-up) -> Blender space (Z-up). Applied here, once, rather than");
        sb.AppendLine("# wherever a position happens to be embedded below.");
        sb.AppendLine("def to_blender(ac_xyz):");
        sb.AppendLine("    x, y, z = ac_xyz");
        sb.AppendLine("    return (x, -z, y)");
        sb.AppendLine();
        sb.AppendLine("def add_mesh(name, material_name, vertices, uvs, triangles):");
        sb.AppendLine("    mesh = bpy.data.meshes.new(name)");
        sb.AppendLine("    mesh.from_pydata([to_blender(v) for v in vertices], [], triangles)");
        sb.AppendLine("    mesh.update()");
        sb.AppendLine("    uv_layer = mesh.uv_layers.new(name=\"UVMap\")");
        sb.AppendLine("    for loop in mesh.loops:");
        sb.AppendLine("        uv_layer.data[loop.index].uv = uvs[loop.vertex_index]");
        sb.AppendLine("    obj = bpy.data.objects.new(name, mesh)");
        sb.AppendLine("    bpy.context.scene.collection.objects.link(obj)");
        sb.AppendLine("    obj.data.materials.append(bpy.data.materials[material_name])");
        sb.AppendLine("    return obj");
        sb.AppendLine();
        sb.AppendLine("def add_marker(name, location, yaw_radians):");
        sb.AppendLine("    obj = bpy.data.objects.new(name, None)");
        sb.AppendLine("    obj.location = to_blender(location)");
        sb.AppendLine("    obj.rotation_euler = (0.0, 0.0, -yaw_radians)");
        sb.AppendLine("    bpy.context.scene.collection.objects.link(obj)");
        sb.AppendLine("    return obj");
        sb.AppendLine();

        sb.AppendLine("# --- materials ---");
        foreach (var material in scene.Materials)
        {
            var varName = PySafeIdentifier(material.Name);
            sb.AppendLine($"{varName} = bpy.data.materials.new({PyString(material.Name)})");
            sb.AppendLine($"{varName}.assettoCorsa.shaderName = {PyString(material.ShaderName)}");
        }
        sb.AppendLine();

        sb.AppendLine("# --- meshes ---");
        foreach (var mesh in scene.Meshes)
        {
            AppendMesh(sb, mesh);
        }
        sb.AppendLine();

        sb.AppendLine("# --- marker nodes ---");
        foreach (var marker in scene.MarkerNodes)
        {
            var location = PyVec3Literal(marker.Position);
            sb.AppendLine($"add_marker({PyString(marker.Name)}, {location}, {marker.YawRadians.ToString(CultureInfo.InvariantCulture)})");
        }
        sb.AppendLine();

        sb.AppendLine("# --- export ---");
        sb.AppendLine(PyStringAssignment("output_path", options.OutputKn5Path));
        sb.AppendLine("warnings = []");
        sb.AppendLine("with open(output_path, \"wb\") as f:");
        sb.AppendLine("    settings = read_settings(output_path)");
        sb.AppendLine("    writer = KN5FileWriter(f, bpy.context, settings, warnings)");
        sb.AppendLine("    writer.write()");
        sb.AppendLine("for warning in warnings:");
        sb.AppendLine("    print(\"KN5_EXPORT_WARNING: \" + warning, file=sys.stderr)");
        sb.AppendLine("print(\"KN5_EXPORT_OK \" + output_path)");

        return sb.ToString();
    }

    private static void AppendMesh(StringBuilder sb, AcMesh mesh)
    {
        var verticesLiteral = string.Join(", ", mesh.Vertices.Select(PyVec3Literal));
        var uvsLiteral = string.Join(", ", mesh.Uvs.Select(uv => $"({uv.X.ToString(CultureInfo.InvariantCulture)}, {uv.Y.ToString(CultureInfo.InvariantCulture)})"));

        var triangleTuples = new List<string>(mesh.TriangleIndices.Count / 3);
        for (var i = 0; i < mesh.TriangleIndices.Count; i += 3)
        {
            triangleTuples.Add($"({mesh.TriangleIndices[i]}, {mesh.TriangleIndices[i + 1]}, {mesh.TriangleIndices[i + 2]})");
        }
        var trianglesLiteral = string.Join(", ", triangleTuples);

        sb.AppendLine($"add_mesh({PyString(mesh.Name)}, {PyString(mesh.MaterialName)}, [{verticesLiteral}], [{uvsLiteral}], [{trianglesLiteral}])");
    }

    private static string PyVec3Literal(Vec3 v) =>
        $"({v.X.ToString(CultureInfo.InvariantCulture)}, {v.Y.ToString(CultureInfo.InvariantCulture)}, {v.Z.ToString(CultureInfo.InvariantCulture)})";

    private static string PyStringAssignment(string variableName, string value) => $"{variableName} = {PyString(value)}";

    private static string PyString(string value)
    {
        // Triple-quoted raw-ish string: simple escaping is enough since these values
        // are filesystem paths and identifiers we generate ourselves, never user-controlled text.
        var escaped = value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        return $"\"{escaped}\"";
    }

    private static string PySafeIdentifier(string name) =>
        "mat_" + new string(name.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
}
