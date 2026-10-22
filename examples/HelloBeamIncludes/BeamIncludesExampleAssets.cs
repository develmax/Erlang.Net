using System.Security.Cryptography;
using System.Text.Json;
using Erlang;

namespace HelloBeamIncludes;

internal static class BeamIncludesExampleAssets
{
    internal static void Load(ProcessRuntime runtime)
    {
        string directory = Path.Combine(AppContext.BaseDirectory, BeamIncludesExampleNames.ModulesDirectory);
        var manifest = JsonSerializer.Deserialize<BeamIncludesExampleManifest>(File.ReadAllBytes(Path.Combine(
            directory,
            BeamIncludesExampleNames.ManifestFile
        )));
        if (manifest?.Outputs is not { Length: 1 } || manifest.Outputs[0] is not { } output
            || output.Module != BeamIncludesExampleNames.Module)
            throw new InvalidDataException(BeamIncludesExampleNames.InvalidAssets);
        byte[] bytes = File.ReadAllBytes(Path.Combine(
            directory,
            BeamIncludesExampleNames.Module + BeamIncludesExampleNames.BeamExtension
        ));
        if (Convert.ToHexString(SHA256.HashData(bytes)) != output.OutputSha256)
            throw new InvalidDataException(BeamIncludesExampleNames.InvalidAssets);
        PreparedBeamImage image = runtime.Modules.PrepareBeam(bytes);
        if (image.Name != BeamIncludesExampleNames.Module
            || runtime.Modules.PublishPreparedBeam([image]).Status != BeamPublicationStatus.Published)
            throw new InvalidDataException(BeamIncludesExampleNames.InvalidAssets);
        if (!ReferenceEquals(runtime.Modules.SourceOwner(
            BeamIncludesExampleNames.Module,
            BeamIncludesExampleNames.HelloFunction,
            0
        ), image))
            throw new InvalidDataException(BeamIncludesExampleNames.InvalidOwner);
    }
}
