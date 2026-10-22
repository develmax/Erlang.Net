using System.Text.Json;

namespace Erlang.BeamBuild;

internal sealed class BeamDeclaredInputSnapshot
{
    private readonly Dictionary<string, byte[]> bytes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> livePaths = new(StringComparer.Ordinal);
    private readonly HashSet<string> directories = new(StringComparer.Ordinal);
    private readonly string[] liveDirectories;
    internal BeamDeclaredFileIdentity[] Inventory { get; }
    internal BeamDeclaredIncludeEdge[] Includes { get; }
    internal string ConfigurationSha256 { get; }

    internal BeamDeclaredInputSnapshot(BeamBuildRequest request)
    {
        request = BeamDeclaredRequest.Validate(request);
        if (request.SourcePolicy != BeamDeclaredInputNames.Policy)
            throw new InvalidDataException(BeamBuildNames.Invalid);
        string root = request.SourceRoot!;
        liveDirectories = request.IncludeDirectories!.Select(directory => Path.Combine(root, directory)).ToArray();
        foreach (string path in request.Sources.Select(source => source.Path).Concat(request.Headers!.Select(header => header.Path)))
        {
            string logical = BeamDeclaredPaths.Relative(root, path);
            byte[] owned = File.ReadAllBytes(path);
            BeamDeclaredPaths.RequireNoReparse(path);
            bytes.Add(logical, owned);
            livePaths.Add(logical, Path.GetFullPath(path));
        }
        Inventory = bytes.OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => new BeamDeclaredFileIdentity(pair.Key, pair.Value.Length, BeamBuildStorage.Hash(pair.Value))).ToArray();
        foreach (string directory in request.IncludeDirectories!.Concat(Inventory.Select(file => BeamDeclaredPaths.DirectoryName(file.LogicalPath))))
        {
            string parent = directory;
            while (directories.Add(parent) && parent != ".")
                parent = BeamDeclaredPaths.DirectoryName(parent);
        }
        var declared = bytes.Keys.ToHashSet(StringComparer.Ordinal);
        Includes = Inventory.SelectMany(file => BeamDeclaredRawPolicy.Inspect(
            file.LogicalPath,
            bytes[file.LogicalPath],
            declared,
            request.IncludeDirectories!
        )).ToArray();
        ConfigurationSha256 = BeamBuildStorage.Hash(JsonSerializer.SerializeToUtf8Bytes(new
        {
            Schema = BeamDeclaredInputNames.Schema,
            SourcePolicy = BeamDeclaredInputNames.Policy,
            FilenamePolicy = BeamDeclaredInputNames.FilenamePolicy,
            SourceRoot = root,
            Sources = request.Sources.OrderBy(source => source.Module, StringComparer.Ordinal)
                .Select(source => new { source.Module, LogicalPath = BeamDeclaredPaths.Relative(root, source.Path) }).ToArray(),
            IncludeDirectories = request.IncludeDirectories,
            Defines = request.Defines,
            Inventory
        }));
    }

    internal void WriteOwnedTree(string ownedRoot)
    {
        if (!Path.IsPathFullyQualified(ownedRoot))
            throw new InvalidDataException(BeamBuildNames.Invalid);
        BeamDeclaredPaths.RequireNoReparse(ownedRoot);
        if (Directory.Exists(ownedRoot) && Directory.EnumerateFileSystemEntries(ownedRoot).Any())
            throw new InvalidDataException(BeamBuildNames.Invalid);
        Directory.CreateDirectory(ownedRoot);
        foreach (string directory in directories.OrderBy(value => value.Length).ThenBy(value => value, StringComparer.Ordinal))
            Directory.CreateDirectory(Path.Combine(ownedRoot, directory));
        foreach (BeamDeclaredFileIdentity file in Inventory)
        {
            string destination = Path.Combine(ownedRoot, file.LogicalPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.WriteAllBytes(destination, bytes[file.LogicalPath]);
        }
        ValidateOwnedTree(ownedRoot);
    }

    internal void ValidateOwnedTree(string ownedRoot)
    {
        BeamDeclaredPaths.RequireNoReparse(ownedRoot);
        var actual = new Dictionary<string, string>(StringComparer.Ordinal);
        var actualDirectories = new HashSet<string>(StringComparer.Ordinal) { "." };
        Visit(
            ownedRoot,
            ownedRoot,
            actual,
            actualDirectories
        );
        if (!directories.SetEquals(actualDirectories) || actual.Count != Inventory.Length || Inventory.Any(file =>
            !actual.TryGetValue(file.LogicalPath, out string? hash) || hash != file.Sha256))
            throw new InvalidDataException(BeamBuildNames.Invalid);
    }

    internal void ValidateLiveInputs()
    {
        foreach (string directory in liveDirectories)
        {
            BeamDeclaredPaths.RequireNoReparse(directory);
            if (!Directory.Exists(directory))
                throw new InvalidDataException(BeamBuildNames.Invalid);
        }
        foreach (BeamDeclaredFileIdentity file in Inventory)
        {
            string path = livePaths[file.LogicalPath];
            BeamDeclaredPaths.RequireNoReparse(path);
            if (!File.Exists(path) || BeamBuildStorage.HashFile(path) != file.Sha256)
                throw new InvalidDataException(BeamBuildNames.Invalid);
        }
    }

    private static void Visit(
        string root,
        string directory,
        Dictionary<string, string> actual,
        HashSet<string> actualDirectories
    )
    {
        foreach (string path in Directory.EnumerateFileSystemEntries(directory))
        {
            BeamDeclaredPaths.RequireNoReparse(path);
            if (Directory.Exists(path))
            {
                actualDirectories.Add(BeamDeclaredPaths.Relative(root, path));
                Visit(
                    root,
                    path,
                    actual,
                    actualDirectories
                );
            }
            else
                actual.Add(BeamDeclaredPaths.Relative(root, path), BeamBuildStorage.HashFile(path));
        }
    }
}
