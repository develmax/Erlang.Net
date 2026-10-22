using System.Security.Cryptography;
using System.Text.Json;
using Erlang;

namespace HelloBeamSource;

internal static class BeamSourceExampleAssets
{
    internal static void Load(ProcessRuntime runtime)
    {
        string directory = Path.Combine(AppContext.BaseDirectory, BeamSourceExampleNames.Modules);
        var manifest = JsonSerializer.Deserialize<BeamSourceExampleManifest>(File.ReadAllBytes(Path.Combine(directory, BeamSourceExampleNames.Manifest)));
        var expected = BeamSourceExampleNames.ExpectedModules.ToHashSet(StringComparer.Ordinal);
        if (manifest?.Outputs is null || manifest.Outputs.Length != expected.Count
            || !expected.SetEquals(manifest.Outputs.Select(value => value.Module)))
            throw new InvalidDataException(BeamSourceExampleNames.Invalid);
        var images = new List<PreparedBeamImage>();
        var dependencies = new BeamDependencySelection(expected.Append(BeamSourceExampleNames.Host));
        foreach (BeamSourceExampleOutput module in manifest.Outputs)
        {
            byte[] bytes = File.ReadAllBytes(Path.Combine(directory, module.Module + BeamSourceExampleNames.BeamExtension));
            if (Convert.ToHexString(SHA256.HashData(bytes)) != module.OutputSha256)
                throw new InvalidDataException(BeamSourceExampleNames.Invalid);
            PreparedBeamImage image = runtime.Modules.PrepareBeam(bytes, dependencies);
            if (image.Name != module.Module)
                throw new InvalidDataException(BeamSourceExampleNames.Invalid);
            images.Add(image);
        }
        if (runtime.Modules.PublishPreparedBeam(images.ToArray()).Status != BeamPublicationStatus.Published)
            throw new InvalidDataException(BeamSourceExampleNames.Invalid);
        foreach (var export in runtime.Modules.Exports.Where(value => expected.Contains(value.Module)))
            if (!ReferenceEquals(
                runtime.Modules.SourceOwner(export.Module, export.Function, export.Arity),
                images.Single(value => value.Name == export.Module)
            ))
                throw new InvalidDataException(BeamSourceExampleNames.Invalid);
    }
}
