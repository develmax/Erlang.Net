using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Erlang.BeamBuild;

public sealed class BeamLibraryBuildTask : Microsoft.Build.Utilities.Task
{
    public ITaskItem[] Libraries { get; set; } = [];
    [Required] public string ProjectDirectory { get; set; } = string.Empty;
    [Required] public string OutputDirectory { get; set; } = string.Empty;
    [Output] public ITaskItem[] Files { get; private set; } = [];

    public override bool Execute()
    {
        Files = [];
        try
        {
            if (!Path.IsPathFullyQualified(ProjectDirectory) || !Path.IsPathFullyQualified(OutputDirectory))
                throw new InvalidDataException(BeamLibraryBuildNames.Invalid);
            var modules = new List<RuntimeLibraryManifestModule>();
            var choices = new List<RuntimeModuleSelection>();
            var images = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            foreach (ITaskItem item in Libraries)
            {
                string backend = item.GetMetadata(BeamLibraryBuildNames.Backend);
                string image = item.GetMetadata(BeamLibraryBuildNames.Image);
                string hash = item.GetMetadata(BeamLibraryBuildNames.Sha256);
                string[] dependencies = item.GetMetadata(BeamLibraryBuildNames.Dependencies)
                    .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (backend == nameof(RuntimeModuleBackend.Managed))
                {
                    if (image.Length != 0 || hash.Length != 0 || dependencies.Length != 0)
                        throw new InvalidDataException(BeamLibraryBuildNames.Invalid);
                    choices.Add(RuntimeModuleSelection.Managed(item.ItemSpec));
                    modules.Add(new(
                        item.ItemSpec,
                        RuntimeModuleBackend.Managed,
                        null,
                        null,
                        []
                    ));
                }
                else if (backend == nameof(RuntimeModuleBackend.Beam))
                {
                    if (string.IsNullOrWhiteSpace(image) || hash.Length != SHA256.HashSizeInBytes * 2 || !hash.All(char.IsAsciiHexDigit))
                        throw new InvalidDataException(BeamLibraryBuildNames.Invalid);
                    byte[] bytes = File.ReadAllBytes(Path.GetFullPath(image, ProjectDirectory));
                    string actualHash = Convert.ToHexString(SHA256.HashData(bytes));
                    if (!actualHash.Equals(hash, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException(BeamLibraryBuildNames.HashMismatch);
                    choices.Add(RuntimeModuleSelection.Beam(item.ItemSpec, bytes, dependencies));
                    string relative = BeamLibraryBuildNames.ImageDirectory + "/" + item.ItemSpec + "-" + actualHash + BeamLibraryBuildNames.Extension;
                    images.Add(relative, bytes);
                    modules.Add(new(
                        item.ItemSpec,
                        RuntimeModuleBackend.Beam,
                        relative,
                        actualHash,
                        dependencies
                    ));
                }
                else
                    throw new InvalidDataException(BeamLibraryBuildNames.Invalid);
            }
            _ = new RuntimeLibrarySelection(choices);
            byte[] manifest = JsonSerializer.SerializeToUtf8Bytes(
                new RuntimeLibraryManifestDocument(RuntimeLibraryManifest.CurrentVersion, modules.OrderBy(row => row.Module, StringComparer.Ordinal).ToArray()),
                RuntimeLibraryManifest.SerializationOptions
            );
            var files = new List<ITaskItem>();
            foreach (var image in images)
                Write(image.Key, image.Value, files);
            Write(RuntimeLibraryManifest.FileName, manifest, files);
            Files = files.ToArray();

            return true;
        }
        catch (Exception error)
        {
            Log.LogErrorFromException(error, showStackTrace: false);

            return false;
        }
    }

    private void Write(string relative, byte[] bytes, List<ITaskItem> files)
    {
        string path = Path.Combine(OutputDirectory, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        if (!File.Exists(path) || !File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes))
        {
            string temporary = path + Guid.NewGuid().ToString("N") + BeamLibraryBuildNames.TemporaryExtension;
            try
            {
                File.WriteAllBytes(temporary, bytes);
                File.Move(temporary, path, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporary))
                    File.Delete(temporary);
            }
        }
        var item = new TaskItem(path);
        item.SetMetadata(BeamLibraryBuildNames.Link, relative);
        item.SetMetadata(BeamLibraryBuildNames.TargetPath, relative);
        files.Add(item);
    }
}
