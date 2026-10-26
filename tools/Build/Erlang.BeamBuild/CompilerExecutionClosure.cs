using System.Text.Json;

namespace Erlang.BeamBuild;

internal static class CompilerExecutionClosure
{
    internal static void Validate(AdmittedCompiler compiler)
        => Validate(compiler.InstalledRoot, compiler.ExecutionManifestPath, compiler.ExecutionManifestSha256);

    internal static void Validate(string installedRoot, string manifestPath, string expectedSha)
    {
        if (!Path.IsPathFullyQualified(manifestPath))
            throw new InvalidDataException(BeamBuildNames.Admission);
        byte[] bytes = File.ReadAllBytes(manifestPath);
        if (!BeamBuildStorage.Hash(bytes).Equals(expectedSha, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException(BeamBuildNames.Admission);
        CompilerExecutionFile[] declared = JsonSerializer.Deserialize<CompilerExecutionFile[]>(bytes)
            ?? throw new InvalidDataException(BeamBuildNames.Admission);
        StringComparer paths = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var expected = new Dictionary<string, string>(paths);
        foreach (CompilerExecutionFile file in declared)
        {
            if (file is null || string.IsNullOrWhiteSpace(file.Path) || Path.IsPathRooted(file.Path)
                || file.Path.Split('/', '\\').Any(part => part is ".." or "." or "")
                || !expected.TryAdd(file.Path.Replace('\\', '/'), file.Sha256))
                throw new InvalidDataException(BeamBuildNames.Admission);
        }
        string root = Path.GetFullPath(installedRoot);
        var found = new Dictionary<string, string>(paths);
        Visit(root, root, found);
        if (found.Count != expected.Count || expected.Any(pair => !found.TryGetValue(pair.Key, out string? sha)
            || !sha.Equals(pair.Value, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidDataException(BeamBuildNames.Admission);
    }

    private static void Visit(string root, string directory, Dictionary<string, string> found)
    {
        if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException(BeamBuildNames.Admission);
        foreach (string path in Directory.EnumerateFileSystemEntries(directory))
        {
            FileAttributes attributes = File.GetAttributes(path);
            if ((attributes & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException(BeamBuildNames.Admission);
            if ((attributes & FileAttributes.Directory) != 0)
                Visit(root, path, found);
            else
                found.Add(Path.GetRelativePath(root, path).Replace('\\', '/'), BeamBuildStorage.HashFile(path));
        }
    }
}
