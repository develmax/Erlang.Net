using Erlang.Compiler;

internal static class OtpWindowsCoreIncludePaths
{
    private const string KernelApplication = "kernel-11.0.4";
    private const string StdlibApplication = "stdlib-8.1";
    private const string BuildLibrary = "/daily_build/otp_src/lib/";
    private const string BootstrapLibrary = "/daily_build/otp_src/bootstrap/lib/";

    internal static BeamIncludePathMap? Find(
        string library,
        string application,
        string source,
        BeamReleaseBuildProfile profile,
        string? sourceHeaders
    )
    {
        string family = application switch
        {
            KernelApplication => "kernel",
            StdlibApplication => "stdlib",
            _ => ""
        };
        if (family.Length == 0)
        {
            return null;
        }

        string directory = BuildLibrary + family + "/src";
        string sourceArgument = Path.GetFileName(source);
        if (profile.WorkingDirectory != directory || profile.SourcePath != directory + "/" + sourceArgument)
        {
            return null;
        }

        // Both Makefiles build into BOOTSTRAP_TOP and pass -I../include.
        // Stdlib also passes -I../../kernel/include after its own directory.
        string local = directory + "/../include";
        string kernelFromStdlib = directory + "/../../kernel/include";
        string[] recordedDirectories = application == KernelApplication
            ? [local]
            : [local, kernelFromStdlib];
        if (!profile.IncludeDirectories.SequenceEqual(recordedDirectories))
        {
            return null;
        }

        string installedKernel = Path.Combine(library, KernelApplication);
        string installedStdlib = Path.Combine(library, StdlibApplication);
        var includes = new List<BeamIncludeSearchDirectory>
        {
            new(Path.Combine(library, application, "include"), local)
        };
        if (application == StdlibApplication)
        {
            includes.Add(new(Path.Combine(installedKernel, "include"), kernelFromStdlib));
        }

        // The installed sources omit some local headers. The pinned source
        // header input mirrors the same logical working directory.
        string currentDirectory = sourceHeaders is null
            ? Path.GetDirectoryName(source)!
            : Path.Combine(
                sourceHeaders,
                "lib",
                family,
                "src"
            );

        // Referenced bootstrap and source headers are identical in the pin.
        var bootstrap = new Dictionary<string, BeamIncludeSearchDirectory>(StringComparer.Ordinal)
        {
            ["kernel"] = new(installedKernel, BootstrapLibrary + "kernel"),
            ["stdlib"] = new(installedStdlib, BootstrapLibrary + "stdlib")
        };

        return new(
            sourceArgument,
            new(currentDirectory, "."),
            includes,
            bootstrap
        );
    }
}
