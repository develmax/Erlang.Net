using System.Text.Json;
using Erlang;

namespace Erlang.BeamBuild;

internal sealed class BeamApplicationSnapshot
{
    private readonly Dictionary<string, byte[]> captured = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> livePaths = new(StringComparer.Ordinal);
    private readonly HashSet<string> ownedDirectories = new(StringComparer.Ordinal)
    {
        ".",
        BeamApplicationNames.SourceTree,
        BeamApplicationNames.BeamTree
    };
    private readonly string outputDirectory;
    internal IReadOnlyList<BeamApplicationInputIdentity> Inventory { get; }
    internal IReadOnlyList<BeamApplicationInputIdentity> Configuration { get; }
    internal string InputSha256 { get; }

    internal BeamApplicationSnapshot(BeamApplicationRequest request)
    {
        request = BeamApplicationRequest.Validate(request);
        outputDirectory = request.OutputDirectory;
        var inventory = new List<BeamApplicationInputIdentity>();
        var configuration = new List<BeamApplicationInputIdentity>();
        long aggregate = 0;

        void Capture(
            string kind,
            string sourceLogical,
            string source,
            string owned,
            string output,
            string? expectedSha = null,
            string? module = null
        )
        {
            byte[] bytes = BeamApplicationRead.Bytes(source, BeamApplicationNames.MaximumFileBytes);
            aggregate = checked(aggregate + bytes.Length);
            if (aggregate > BeamApplicationNames.MaximumAggregateBytes)
                throw new InvalidDataException(BeamApplicationNames.Invalid);
            string sha = BeamBuildStorage.Hash(bytes);
            if (expectedSha is not null && sha != expectedSha)
                throw new InvalidDataException(BeamApplicationNames.Invalid);
            if (module is not null)
            {
                BeamArtifactInfo prepared = BeamArtifactInspector.Inspect(bytes);
                if (prepared.Name != module || prepared.HasOnLoad)
                    throw new InvalidDataException(BeamApplicationNames.Invalid);
            }
            captured.Add(owned, bytes);
            livePaths.Add(owned, source);
            string directory = BeamDeclaredPaths.DirectoryName(owned);
            while (directory != "." && ownedDirectories.Add(directory))
                directory = BeamDeclaredPaths.DirectoryName(directory);
            var identity = new BeamApplicationInputIdentity(
                kind,
                sourceLogical,
                output,
                bytes.Length,
                sha
            );
            inventory.Add(identity);
            if (kind == BeamApplicationNames.ConfigurationKind)
                configuration.Add(identity);
        }
        string app = request.Layout;
        Capture(
            BeamApplicationNames.DescriptorKind,
            request.Descriptor,
            BeamApplicationPaths.ContainedFile(request.SourceRoot, request.Descriptor),
            BeamApplicationNames.SourceTree + BeamApplicationNames.LogicalSeparator + request.Descriptor,
            app + BeamApplicationNames.LogicalSeparator + BeamApplicationNames.EbinDirectory + BeamApplicationNames.LogicalSeparator + request.ApplicationName + BeamApplicationNames.AppExtension
        );
        foreach (BeamApplicationModule module in request.Modules)
        {
            string logical = module.Module + BeamBuildNames.BeamExtension;
            Capture(
                BeamApplicationNames.ModuleKind,
                logical,
                module.Path,
                BeamApplicationNames.BeamTree + BeamApplicationNames.LogicalSeparator + logical,
                app + BeamApplicationNames.LogicalSeparator + BeamApplicationNames.EbinDirectory + BeamApplicationNames.LogicalSeparator + logical,
                module.Sha256,
                module.Module
            );
        }
        foreach (var group in new[]
        {
            (request.Priv, BeamApplicationNames.PrivKind, BeamApplicationNames.PrivDirectory),
            (request.Configuration, BeamApplicationNames.ConfigurationKind, BeamApplicationNames.ConfigurationDirectory)
        })
            foreach (BeamApplicationFile file in group.Item1)
                Capture(
                    group.Item2,
                    file.Source,
                    BeamApplicationPaths.ContainedFile(request.SourceRoot, file.Source),
                    BeamApplicationNames.SourceTree + BeamApplicationNames.LogicalSeparator + file.Source,
                    app + BeamApplicationNames.LogicalSeparator + group.Item3 + BeamApplicationNames.LogicalSeparator + file.LogicalPath
                );
        Inventory = Array.AsReadOnly(inventory.OrderBy(value => value.OutputPath, StringComparer.Ordinal).ToArray());
        Configuration = Array.AsReadOnly(configuration.ToArray());
        InputSha256 = BeamBuildStorage.Hash(JsonSerializer.SerializeToUtf8Bytes(new
        {
            Schema = BeamApplicationNames.Schema,
            request.ApplicationName,
            request.Version,
            request.Layout,
            request.SourceRoot,
            request.RequestSha256,
            Inventory,
            ConfigurationOrder = Configuration.Select(value => value.OutputPath).ToArray(),
            ReservedOutputPaths = request.ReservedOutputPaths.Order(StringComparer.Ordinal).ToArray()
        }));
    }

    internal void WriteOwnedTree(string root)
    {
        RequireOwnedRoot(root);
        if (Directory.Exists(root) && Directory.EnumerateFileSystemEntries(root).Any())
            throw new InvalidDataException(BeamApplicationNames.Invalid);
        Directory.CreateDirectory(root);
        foreach (string directory in ownedDirectories.OrderBy(value => value.Length).ThenBy(value => value, StringComparer.Ordinal))
            Directory.CreateDirectory(Path.Combine(root, directory));
        foreach (var file in captured)
            File.WriteAllBytes(Path.Combine(root, file.Key), file.Value);
        VerifyOwned(root);
    }

    internal void VerifyLive()
    {
        foreach (var file in captured)
        {
            string path = livePaths[file.Key];
            BeamApplicationPaths.RequireActualCase(path);
            if (BeamBuildStorage.Hash(BeamApplicationRead.Bytes(path, BeamApplicationNames.MaximumFileBytes)) != BeamBuildStorage.Hash(file.Value))
                throw new InvalidDataException(BeamApplicationNames.Invalid);
        }
    }

    internal void VerifyOwned(string root)
    {
        RequireOwnedRoot(root);
        if (!Directory.Exists(root))
            throw new InvalidDataException(BeamApplicationNames.Invalid);
        var files = new Dictionary<string, string>(StringComparer.Ordinal);
        var directories = new HashSet<string>(StringComparer.Ordinal) { "." };
        Visit(
            root,
            root,
            files,
            directories
        );
        if (!directories.SetEquals(ownedDirectories) || files.Count != captured.Count
            || captured.Any(file => !files.TryGetValue(file.Key, out string? sha) || sha != BeamBuildStorage.Hash(file.Value)))
            throw new InvalidDataException(BeamApplicationNames.Invalid);
    }

    internal string OwnedPath(string root, BeamApplicationInputIdentity input)
    {
        RequireOwnedRoot(root);
        if (!Inventory.Contains(input))
            throw new InvalidDataException(BeamApplicationNames.Invalid);
        string relative = (input.Kind == BeamApplicationNames.ModuleKind ? BeamApplicationNames.BeamTree : BeamApplicationNames.SourceTree)
            + BeamApplicationNames.LogicalSeparator + input.SourceLogicalPath;

        return Path.Combine(root, relative);
    }

    private void RequireOwnedRoot(string root)
    {
        if (!Path.IsPathFullyQualified(root) || !BeamApplicationPaths.Within(outputDirectory, root)
            || Path.TrimEndingDirectorySeparator(Path.GetFullPath(root)).Equals(
                Path.TrimEndingDirectorySeparator(outputDirectory),
                OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal
            ))
            throw new InvalidDataException(BeamApplicationNames.Invalid);
        BeamApplicationPaths.RequireCreationPath(root);
    }

    private void Visit(
        string root,
        string directory,
        Dictionary<string, string> files,
        HashSet<string> directories
    )
    {
        foreach (string path in Directory.EnumerateFileSystemEntries(directory))
        {
            BeamApplicationPaths.RequireActualCase(path);
            string relative = BeamDeclaredPaths.Relative(root, path);
            if (Directory.Exists(path))
            {
                if (!ownedDirectories.Contains(relative))
                    throw new InvalidDataException(BeamApplicationNames.Invalid);
                directories.Add(relative);
                Visit(
                    root,
                    path,
                    files,
                    directories
                );
            }
            else
            {
                if (!captured.ContainsKey(relative))
                    throw new InvalidDataException(BeamApplicationNames.Invalid);
                files.Add(relative, BeamBuildStorage.Hash(BeamApplicationRead.Bytes(path, BeamApplicationNames.MaximumFileBytes)));
            }
        }
    }
}
