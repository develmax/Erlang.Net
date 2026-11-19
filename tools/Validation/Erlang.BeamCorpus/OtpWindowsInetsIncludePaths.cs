using Erlang.Compiler;

internal static class OtpWindowsInetsIncludePaths
{
    private const string Application = "inets-9.8";
    private const string BuildLibrary = "/daily_build/otp_src/lib/inets";
    private const string BootstrapKernel = "/daily_build/otp_src/bootstrap/lib/kernel";

    internal static BeamIncludePathMap? Find(
        string library,
        string application,
        string source,
        BeamReleaseBuildProfile profile,
        string? sourceHeaders
    )
    {
        if (application != Application)
        {
            return null;
        }

        string sourceDirectory = Path.GetDirectoryName(source)!;
        string subdirectory = Path.GetFileName(sourceDirectory);
        if (subdirectory is not ("inets_app" or "http_client" or "http_lib" or "http_server"))
        {
            return null;
        }

        string directory = BuildLibrary + "/src/" + subdirectory;
        string sourceArgument = Path.GetFileName(source);
        if (profile.WorkingDirectory != directory || profile.SourcePath != directory + "/" + sourceArgument)
        {
            return null;
        }

        string installed = Path.Combine(library, application);
        var includes = new List<BeamIncludeSearchDirectory>();
        foreach (string path in profile.IncludeDirectories)
        {
            if (!path.StartsWith(BuildLibrary + "/", StringComparison.Ordinal))
            {
                return null;
            }

            string physical = Path.GetFullPath(path[(BuildLibrary.Length + 1)..], installed);
            includes.Add(new(physical, path));
        }

        string cwd = sourceHeaders is null
            ? sourceDirectory
            : Path.Combine(
                sourceHeaders,
                "lib",
                "inets",
                "src",
                subdirectory
            );

        // inets_app/inets.mk adds the in-tree inets ebin to the bootstrap code path.
        var applications = new Dictionary<string, BeamIncludeSearchDirectory>(StringComparer.Ordinal)
        {
            ["inets"] = new(installed, BuildLibrary),
            ["kernel"] = new(Path.Combine(library, "kernel-11.0.4"), BootstrapKernel)
        };

        return new(
            sourceArgument,
            new(cwd, "."),
            includes,
            applications
        );
    }
}
