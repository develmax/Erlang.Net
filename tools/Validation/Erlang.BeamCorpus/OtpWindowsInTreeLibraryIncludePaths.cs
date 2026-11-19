using Erlang.Compiler;

internal static class OtpWindowsInTreeLibraryIncludePaths
{
    private const string BuildLibrary = "/daily_build/otp_src/lib";
    private const string EventTracer = "et-1.8";
    private const string Observer = "observer-2.19";
    private const string ReleaseTool = "reltool-1.1";

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
            EventTracer => "et",
            Observer => "observer",
            ReleaseTool => "reltool",
            _ => ""
        };
        if (app.Length == 0)
        {
            return null;
        }

        string directory = BuildLibrary + "/" + app + "/src";
        string sourceArgument = Path.GetFileName(source);
        if (profile.WorkingDirectory != directory || profile.SourcePath != directory + "/" + sourceArgument)
        {
            return null;
        }

        string physicalApp = Path.Combine(library, application);
        string cwd = sourceHeaders is null
            ? Path.GetDirectoryName(source)!
            : Path.Combine(
                sourceHeaders,
                "lib",
                app,
                "src"
            );
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
            else if (path == directory + "/../include")
            {
                includes.Add(new(Path.Combine(physicalApp, "include"), path));
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
        );
    }

    private static Dictionary<string, string> InstalledApplications(string library) =>
        Directory.EnumerateDirectories(library)
            .Select(path => (Name: Path.GetFileName(path).Split('-', 2), Path: path))
            .Where(application => application.Name.Length == 2)
            .GroupBy(application => application.Name[0], application => application.Path, StringComparer.Ordinal)
            .Where(application => application.Count() == 1)
            .ToDictionary(application => application.Key, application => application.Single(), StringComparer.Ordinal);
}
