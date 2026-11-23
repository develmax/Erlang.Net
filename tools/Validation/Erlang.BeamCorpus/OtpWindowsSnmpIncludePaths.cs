using Erlang.Compiler;

internal static class OtpWindowsSnmpIncludePaths
{
    private const string BuildDirectory = "/daily_build/otp_src/";
    private const string BuildLibrary = BuildDirectory + "lib/";
    private const string BuildSnmp = BuildLibrary + "snmp";
    private const string BootstrapLibrary = BuildDirectory + "bootstrap/lib/";

    internal static BeamIncludePathMap? Find(
        string library,
        string application,
        string source,
        BeamReleaseBuildProfile profile
    )
    {
        if (!application.StartsWith("snmp-", StringComparison.Ordinal) || !profile.SourcePath.StartsWith(BuildSnmp + "/", StringComparison.Ordinal))
        {
            return null;
        }

        string installedSnmp = Path.Combine(library, application);
        string sourceDirectory = Path.GetDirectoryName(source)!;
        string recordedCwd = profile.WorkingDirectory.TrimEnd('/') + "/";
        if (!profile.SourcePath.StartsWith(recordedCwd, StringComparison.Ordinal))
        {
            return null;
        }

        // OTP erlc uses the source path relative to the working directory.
        string sourceArgument = profile.SourcePath[recordedCwd.Length..];
        if (!string.Equals(sourceArgument, Path.GetFileName(source), StringComparison.Ordinal))
        {
            return null;
        }

        var installedKernel = new BeamIncludeSearchDirectory(Path.Combine(library, "kernel-11.0.4"), BootstrapLibrary + "kernel");
        var installedStdlib = new BeamIncludeSearchDirectory(Path.Combine(library, "stdlib-8.1"), BootstrapLibrary + "stdlib");
        var applications = new Dictionary<string, BeamIncludeSearchDirectory>(StringComparer.Ordinal)
        {
            ["kernel"] = installedKernel,
            ["stdlib"] = installedStdlib,
            ["snmp"] = new(installedSnmp, BuildSnmp)
        };

        // The SNMP makefiles use -pa for the in-tree SNMP application. The other
        // applications used by these headers come from the boot compiler.
        var includeDirectories = profile.IncludeDirectories.Select(path =>
            MapIncludeDirectory(
                path,
                installedSnmp,
                installedStdlib.PhysicalDirectory,
                sourceDirectory
            )).ToArray();
        if (includeDirectories.Any(path => path is null))
        {
            return null;
        }

        return new(
            sourceArgument,
            new(sourceDirectory, "."),
            includeDirectories.Select(path => path!).ToArray(),
            applications
        );
    }

    private static BeamIncludeSearchDirectory? MapIncludeDirectory(
        string path,
        string installedSnmp,
        string installedStdlib,
        string sourceDirectory
    )
    {
        if (path == ".")
        {
            return new(sourceDirectory, path);
        }
        if (path == BuildLibrary + "stdlib")
        {
            return new(installedStdlib, path);
        }
        if (!path.StartsWith(BuildSnmp + "/", StringComparison.Ordinal))
        {
            return null;
        }

        string physical = Path.GetFullPath(path[(BuildSnmp.Length + 1)..], installedSnmp);
        string relative = Path.GetRelativePath(installedSnmp, physical);
        string[] parts = relative.Split(Path.DirectorySeparatorChar);
        if (parts is ["src", "compile", ..])
        {
            parts[1] = "compiler";
            physical = Path.Combine(installedSnmp, Path.Combine(parts));
        }
        // Release example sources and their sibling directories are under doc/.
        if (parts is ["examples", ..])
        {
            physical = Path.Combine(installedSnmp, "doc", Path.Combine(parts));
        }

        return new(physical, path);
    }
}
