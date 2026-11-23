using Erlang.Compiler;

internal static class OtpWindowsInTreeLibraryIncludePaths
{
    private const string BuildLibrary = "/daily_build/otp_src/lib";
    private const string Edoc = "edoc-1.5";
    private const string Xmerl = "xmerl-2.2";
    private const string EventTracer = "et-1.8";
    private const string Observer = "observer-2.19";
    private const string ReleaseTool = "reltool-1.1";
    private const string Wx = "wx-2.7";

    internal static BeamIncludePathMap? Find(
        string library,
        string application,
        string source,
        BeamReleaseBuildProfile profile,
        string? sourceHeaders
    )
    {
        string app = application switch
        {
            Edoc => "edoc",
            EventTracer => "et",
            Observer => "observer",
            ReleaseTool => "reltool",
            Wx => "wx",
            _ => ""
        };
        if (app.Length == 0)
        {
            return null;
        }

        string directory = BuildLibrary + "/" + app + "/src";
        string sourceArgument = application == Wx && profile.RecordedSourceFile is { } recordedSource
            ? recordedSource
            : Path.GetFileName(source);
        if (profile.WorkingDirectory != directory || profile.SourcePath != directory + "/" + sourceArgument)
        {
            return null;
        }

        string physicalApp = Path.Combine(library, application);
        string buildInclude = directory + "/../include";
        if (application == Wx && !profile.IncludeDirectories.SequenceEqual([buildInclude, BuildLibrary]))
        {
            return null;
        }

        string installedSource = Path.GetDirectoryName(source)!;
        string cwd = sourceHeaders is null
            ? (application == Wx && sourceArgument.StartsWith("gen/", StringComparison.Ordinal)
                ? Path.GetDirectoryName(installedSource)!
                : installedSource)
            : Path.Combine(
                sourceHeaders,
                "lib",
                app,
                "src"
            );
        if (application == Edoc && (sourceHeaders is null || !Directory.Exists(cwd)))
        {
            return null;
        }

        var inTree = new BeamIncludeSearchDirectory(library, BuildLibrary)
        {
            PhysicalChildDirectories = InstalledApplications(library)
        };

        var includes = new List<BeamIncludeSearchDirectory>();
        foreach (string path in profile.IncludeDirectories)
        {
            if (path == BuildLibrary)
            {
                includes.Add(inTree);
            }
            else if (path == buildInclude)
            {
                includes.Add(new(Path.Combine(physicalApp, "include"), path));
            }
            else if (application == Edoc && path == directory + "/../../xmerl/include")
            {
                includes.Add(new(Path.Combine(library, Xmerl, "include"), path));
            }
            else if (app == "observer" && path == directory + "/../../et/include")
            {
                includes.Add(new(Path.Combine(library, EventTracer, "include"), path));
            }
            else if (app == "observer" && path == directory + "/../../../libraries/et/include")
            {
                // This makefile also searches an external source tree. The
                // pinned installed corpus supplies no second Et source tree.
            }
            else
            {
                return null;
            }
        }

        return new(
            sourceArgument,
            new(cwd, "."),
            includes,
            new Dictionary<string, BeamIncludeSearchDirectory>()
        )
        {
            // The Edoc installation also puts public headers next to its source
            // files. The release build searches the pinned source directory.
            PhysicalSourceDirectory = application == Edoc ? cwd : null
        };
    }

    private static Dictionary<string, string> InstalledApplications(string library) =>
        Directory.EnumerateDirectories(library)
            .Select(path => (Name: Path.GetFileName(path).Split('-', 2), Path: path))
            .Where(application => application.Name.Length == 2)
            .GroupBy(application => application.Name[0], application => application.Path, StringComparer.Ordinal)
            .Where(application => application.Count() == 1)
            .ToDictionary(application => application.Key, application => application.Single(), StringComparer.Ordinal);
}
