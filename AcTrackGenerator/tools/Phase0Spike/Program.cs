using AcTrackGenerator.Ac.Export.Kn5Pipeline;
using AcTrackGenerator.Pipeline.Orchestration;

var arguments = ParseArguments(args);
if (arguments.ShowHelp)
{
    PrintUsage();
    return 0;
}

string addonDirectory;
try
{
    addonDirectory = arguments.AddonDirectory ?? LocateDefaultAddonDirectory();
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Could not resolve the add-on source directory: {ex.Message}");
    Console.Error.WriteLine("Pass --addon-dir explicitly to point at a checkout of moppius/blender-assetto-corsa-tools.");
    return 1;
}

var outputDirectory = arguments.OutputDirectory ?? Path.Combine(Directory.GetCurrentDirectory(), "phase0-spike-output");

Console.WriteLine("AC Track Generator - Phase 0 spike");
Console.WriteLine($"  Add-on source: {addonDirectory}");
Console.WriteLine($"  Output dir:    {outputDirectory}");
Console.WriteLine($"  Track id:      {arguments.TrackId}");
Console.WriteLine();

try
{
    var result = Phase0SpikePipeline.Run(new Phase0SpikeOptions(
        OutputDirectory: outputDirectory,
        AddonSourceDirectory: addonDirectory,
        TrackId: arguments.TrackId,
        BlenderExecutablePathOverride: arguments.BlenderPath));

    Console.WriteLine("Export succeeded.");
    Console.WriteLine($"  Track folder:       {result.TrackDirectory}");
    Console.WriteLine($"  KN5 file:           {result.Kn5Path}");
    Console.WriteLine($"  Centerline points:  {result.CenterlinePointCount}");
    Console.WriteLine($"  Track length:       {result.TrackLengthMeters:F1} m");

    if (result.BlenderWarnings.Count > 0)
    {
        Console.WriteLine($"  Blender warnings ({result.BlenderWarnings.Count}):");
        foreach (var warning in result.BlenderWarnings)
        {
            Console.WriteLine($"    - {warning}");
        }
    }

    Console.WriteLine();
    Console.WriteLine("Next step: copy the track folder into your Assetto Corsa installation's");
    Console.WriteLine("content/tracks/ directory and try to load it - see docs/phase0-spike.md.");

    return 0;
}
catch (BlenderExportException ex)
{
    Console.Error.WriteLine("Blender export failed:");
    Console.Error.WriteLine(ex.Message);
    return 1;
}

static string LocateDefaultAddonDirectory()
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null)
    {
        var candidate = Path.Combine(directory.FullName, "third_party", "blender-assetto-corsa-tools");
        if (File.Exists(Path.Combine(candidate, "__init__.py")))
        {
            return candidate;
        }

        directory = directory.Parent;
    }

    throw new DirectoryNotFoundException(
        "Could not find 'third_party/blender-assetto-corsa-tools' above the running executable.");
}

static void PrintUsage()
{
    Console.WriteLine("""
        AC Track Generator - Phase 0 spike

        Generates a synthetic flat oval track end to end (centerline -> mesh -> KN5
        via headless Blender -> hand-rolled AI line -> minimal track package) to prove
        the export pipeline works before any OSM ingestion exists.

        Usage:
          Phase0Spike [options]

        Options:
          --output-dir <path>    Directory the track folder is written into (default: ./phase0-spike-output)
          --addon-dir <path>     Checkout of moppius/blender-assetto-corsa-tools (default: auto-detected
                                  third_party/blender-assetto-corsa-tools submodule)
          --blender-path <path>  Explicit Blender executable (default: PATH / AC_TRACK_GENERATOR_BLENDER_PATH / common install paths)
          --track-id <id>        Track folder/file name (default: spike_oval)
          -h, --help             Show this help text
        """);
}

static Arguments ParseArguments(string[] args)
{
    string? outputDirectory = null;
    string? addonDirectory = null;
    string? blenderPath = null;
    var trackId = "spike_oval";
    var showHelp = false;

    for (var i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "-h":
            case "--help":
                showHelp = true;
                break;
            case "--output-dir":
                outputDirectory = RequireValue(args, ref i, "--output-dir");
                break;
            case "--addon-dir":
                addonDirectory = RequireValue(args, ref i, "--addon-dir");
                break;
            case "--blender-path":
                blenderPath = RequireValue(args, ref i, "--blender-path");
                break;
            case "--track-id":
                trackId = RequireValue(args, ref i, "--track-id");
                break;
            default:
                throw new ArgumentException($"Unrecognized argument: '{args[i]}'");
        }
    }

    return new Arguments(showHelp, outputDirectory, addonDirectory, blenderPath, trackId);
}

static string RequireValue(string[] args, ref int index, string optionName)
{
    if (index + 1 >= args.Length)
    {
        throw new ArgumentException($"'{optionName}' requires a value.");
    }

    index++;
    return args[index];
}

internal sealed record Arguments(bool ShowHelp, string? OutputDirectory, string? AddonDirectory, string? BlenderPath, string TrackId);
