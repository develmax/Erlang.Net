using Erlang.Compiler;

internal sealed class OtpWindowsPublicKeyIncludePaths(string library, DirectoryInfo generatedSource) : IDisposable
{
    internal const string Application = "public_key-1.21.7";
    private const string SourceDirectory = "/daily_build/otp_src/lib/public_key/src";
    private const string BootstrapKernel = "/daily_build/otp_src/bootstrap/lib/kernel";

    internal static OtpWindowsPublicKeyIncludePaths Create(string library, string generatedHeaders)
    {
        DirectoryInfo overlay = Directory.CreateTempSubdirectory("erlang-public-key-source-");
        try
        {
            // asn1/Makefile generates .hrl files in ../src, before the ordinary -I directories.
            foreach (string header in Directory.EnumerateFiles(generatedHeaders, "*.hrl"))
            {
                File.Copy(header, Path.Combine(overlay.FullName, Path.GetFileName(header)));
            }

            // The same makefile installs a copy of this generated source header in include.
            const string installedHeader = "PKCS-FRAME.hrl";
            string sourceCopy = Path.Combine(overlay.FullName, installedHeader);
            string installedCopy = Path.Combine(
                library,
                Application,
                "include",
                installedHeader
            );
            if (!File.Exists(sourceCopy) && File.Exists(installedCopy))
            {
                File.Copy(installedCopy, sourceCopy);
            }

            return new(library, overlay);
        }
        catch
        {
            overlay.Delete(recursive: true);
            throw;
        }
    }

    internal BeamIncludePathMap? Find(
        string application,
        string source,
        BeamReleaseBuildProfile profile,
        string? sourceHeaders
    )
    {
        string sourceArgument = Path.GetFileName(source);
        string localInclude = SourceDirectory + "/../include";
        string asn1Include = SourceDirectory + "/../asn1";
        if (application != Application || profile.WorkingDirectory != SourceDirectory
            || profile.SourcePath != SourceDirectory + "/" + sourceArgument
            || !profile.IncludeDirectories.SequenceEqual([localInclude, asn1Include]))
        {
            return null;
        }

        string cwd = sourceHeaders is null
            ? Path.GetDirectoryName(source)!
            : Path.Combine(
                sourceHeaders,
                "lib",
                "public_key",
                "src"
            );
        string installedApplication = Path.Combine(library, Application);

        return new(
            sourceArgument,
            new(cwd, "."),
            [
                new(generatedSource.FullName, "."),
                new(Path.Combine(installedApplication, "include"), localInclude),
                new(Path.Combine(installedApplication, "asn1"), asn1Include)
            ],
            new Dictionary<string, BeamIncludeSearchDirectory>(StringComparer.Ordinal)
            {
                ["kernel"] = new(Path.Combine(library, "kernel-11.0.4"), BootstrapKernel)
            }
        );
    }

    public void Dispose() => generatedSource.Delete(recursive: true);
}
