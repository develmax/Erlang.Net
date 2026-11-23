using Erlang.Compiler;

internal static class OtpWindowsBootstrapLibraryIncludePaths
{
    private const string BuildRoot = "/daily_build/otp_src/";
    private const string BuildLibrary = BuildRoot + "lib/";
    private const string BootstrapLibrary = BuildRoot + "bootstrap/lib/";
    private const string KernelApplication = "kernel-11.0.4";
    private const string StdlibApplication = "stdlib-8.1";
    private const string MegacoEngineDirectory = "lib/megaco/src/engine";

    internal static BeamIncludePathMap? Find(
        string library,
        string application,
        string source,
        BeamReleaseBuildProfile profile,
        string? sourceHeaders
    )
    {
        string name = application switch
        {
            "mnesia-4.27" => "mnesia",
            "sasl-4.4" => "sasl",
            "tools-4.2.3" => "tools",
            "dialyzer-6.0.3" => "dialyzer",
            "xmerl-2.2" => "xmerl",
            "asn1-5.5.2" => "asn1",
            "erts-17.1" => "erts",
            "eunit-2.11" => "eunit",
            "ftp-1.2.6" => "ftp",
            "megaco-4.9.2" => "megaco",
            "tftp-1.3" => "tftp",
            "runtime_tools-2.5" => "runtime_tools",
            "ssh-6.0.6" => "ssh",
            "common_test-1.31.2" => "common_test",
            "compiler-10.0.6" => "compiler",
            "diameter-2.7.2" => "diameter",
            "eldap-1.3.1" => "eldap",
            "os_mon-2.12" => "os_mon",
            "parsetools-2.8" => "parsetools",
            "ssl-11.7.7" => "ssl",
            "syntax_tools-4.1.1" => "syntax_tools",
            _ => ""
        };
        if (name.Length == 0 || (name is "common_test" or "os_mon") && sourceHeaders is null)
        {
            return null;
        }

        bool megacoEngine = name == "megaco" && profile.WorkingDirectory == BuildRoot + MegacoEngineDirectory;
        string relativeDirectory = name switch
        {
            "erts" => "erts/preloaded/src",
            "megaco" when megacoEngine => MegacoEngineDirectory,
            "megaco" => "lib/megaco/examples/meas",
            _ => "lib/" + name + "/src"
        };
        string directory = BuildRoot + relativeDirectory;
        string sourceArgument = name == "diameter" && profile.RecordedSourceFile is { } recordedSource
            ? recordedSource
            : Path.GetFileName(source);
        if (profile.WorkingDirectory != directory || profile.SourcePath != directory + "/" + sourceArgument)
        {
            return null;
        }

        string cwd = sourceHeaders is null
            ? Path.GetDirectoryName(source)!
            : Path.Combine(sourceHeaders, relativeDirectory.Replace('/', Path.DirectorySeparatorChar));
        string physicalApp = Path.Combine(library, application);
        if (name == "diameter" && sourceHeaders is null)
        {
            cwd = Path.Combine(physicalApp, "src");
        }

        string installedKernel = Path.Combine(library, KernelApplication);
        string installedStdlib = Path.Combine(library, StdlibApplication);
        string kernelSource = Path.Combine(installedKernel, "src");
        string kernelInclude = Path.Combine(installedKernel, "include");
        string localInclude = name == "os_mon"
            ? Path.Combine(cwd, "..", "include")
            : Path.Combine(physicalApp, "include");

        // Retain the order and spelling of the ordinary -I arguments from each makefile.
        BeamIncludeSearchDirectory[] includes = name switch
        {
            "sasl" => [new(Path.Combine(installedStdlib, "include"), directory + "/../../stdlib/include")],
            "compiler" =>
            [
                new(Path.Combine(installedStdlib, "include"), directory + "/../../stdlib/include"),
                new(cwd, directory + "/.")
            ],
            "xmerl" or "eunit" or "os_mon" or "syntax_tools" => [new(localInclude, directory + "/../include")],
            "diameter" =>
            [
                new(localInclude, directory + "/../include"),
                new(Path.Combine(physicalApp, "src", "gen"), directory + "/gen")
            ],
            // ELDAP's ASN.1 rule emits ELDAPv3.hrl in ebin before eldap is compiled.
            "eldap" =>
            [
                new(localInclude, directory + "/../include"),
                new(Path.Combine(physicalApp, "ebin"), directory + "/../ebin")
            ],
            "parsetools" => [new(Path.Combine(installedStdlib, "include"), BuildLibrary + "stdlib/include")],
            "asn1" when profile.IncludeDirectories.Count == 2 =>
            [
                new(installedStdlib, BuildLibrary + "stdlib"),
                new(Path.Combine(cwd, "..", "rt_templates"), directory + "/../rt_templates")
            ],
            "asn1" => [new(installedStdlib, BuildLibrary + "stdlib")],
            "erts" =>
            [
                new(kernelSource, BuildLibrary + "kernel/src"),
                new(kernelInclude, BuildLibrary + "kernel/include")
            ],
            "ftp" or "ssh" or "ssl" => [new(kernelSource, BuildLibrary + "kernel/src")],
            "runtime_tools" =>
            [
                new(localInclude, directory + "/../include"),
                new(Path.Combine(library, "et-1.8", "include"), directory + "/../../et/include"),
                new(
                    Path.Combine(
                        sourceHeaders ?? Path.Combine(library, ".."),
                        "libraries",
                        "et",
                        "include"
                    ),
                    directory + "/../../../libraries/et/include"
                )
            ],
            "common_test" =>
            [
                new(localInclude, directory + "/../include"),
                new(Path.Combine(library, "snmp-5.20.5", "include"), BuildLibrary + "snmp/include"),
                new(
                    Path.Combine(
                        cwd,
                        "..",
                        "..",
                        "xmerl",
                        "inc"
                    ),
                    directory + "/../../xmerl/inc"
                ),
                new(kernelInclude, BuildLibrary + "kernel/include")
            ],
            "megaco" when megacoEngine => [new(localInclude, directory + "/../../include")],
            "megaco" => [new(Path.Combine(cwd, "..", "include"), directory + "/../include")],
            _ => []
        };
        if (!profile.IncludeDirectories.SequenceEqual(includes.Select(include => include.RecordedDirectory)))
        {
            return null;
        }

        var applications = new Dictionary<string, BeamIncludeSearchDirectory>(StringComparer.Ordinal)
        {
            ["kernel"] = new(installedKernel, BootstrapLibrary + "kernel"),
            // EUnit's makefile prepends the relative in-tree stdlib code path.
            ["stdlib"] = new(installedStdlib, name == "eunit" ? "../../stdlib" : BootstrapLibrary + "stdlib"),
            // Makefile.in copies public_key's headers during the tertiary bootstrap.
            ["public_key"] = new(Path.Combine(library, "public_key-1.21.7"), BootstrapLibrary + "public_key")
        };
        if (name == "common_test")
        {
            applications.Add("xmerl", new(Path.Combine(library, "xmerl-2.2"), BootstrapLibrary + "xmerl"));
            applications.Add("common_test", new(Path.Combine(library, application), BootstrapLibrary + "common_test"));
        }

        if (name == "megaco")
        {
            // The Megaco makefiles use the in-tree application on the compiler code path.
            applications.Add("megaco", new(Path.Combine(library, application), BuildLibrary + "megaco"));
        }

        if (name is "diameter" or "syntax_tools")
        {
            // Both makefiles place the current application's ebin on the compiler code path.
            applications.Add(name, new(physicalApp, BuildLibrary + name));
        }

        if (megacoEngine)
        {
            // src/app/megaco.mk prepends the in-tree Event Tracer code path.
            applications.Add("et", new(Path.Combine(library, "et-1.8"), BuildLibrary + "et"));
        }

        return new(
            sourceArgument,
            new(cwd, "."),
            includes,
            applications
        )
        {
            // The release copies Os_Mon's private include headers into src after compilation.
            PhysicalSourceDirectory = name is "common_test" or "os_mon" ? cwd : null
        };
    }
}
