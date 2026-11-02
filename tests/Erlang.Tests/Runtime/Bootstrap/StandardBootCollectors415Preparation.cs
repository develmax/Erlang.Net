using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class StandardBootCollectors415Preparation
{
    internal static IReadOnlyList<PreparedBeamImage> Prepare(ModuleRegistry registry, BeamDependencySelection dependencies)
    {
        var hashes = JsonSerializer.Deserialize<Dictionary<string, string>>(
            ManagedCollectors413Access.Resource(ManagedCollectors413Names.Manifest))!;
        StandardBootOriginal412Access.Require(hashes.Count == 2);
        var images = new List<PreparedBeamImage>();
        foreach (string module in new[] { ManagedCollectors413Names.Literal, ManagedCollectors413Names.Trace })
        {
            byte[] bytes = ManagedCollectors413Access.Resource(module + StandardBootOriginal412Names.Beam);
            StandardBootOriginal412Access.Require(hashes.TryGetValue(module + StandardBootOriginal412Names.Beam, out string? hash)
                && hash == Convert.ToHexString(SHA256.HashData(bytes)));
            PreparedBeamImage image = registry.PrepareBeam(bytes, dependencies);
            StandardBootOriginal412Access.Require(image.Name == module && image.NativePrimitiveExports.Count == 2);
            images.Add(image);
        }

        return images;
    }

    internal static IReadOnlyList<PreparedBeamImage> Prepare171(ModuleRegistry registry, BeamDependencySelection dependencies, global::Erlang.Tests.PreparedBeamFixtureScope171 scope)
    {
        var hashes = JsonSerializer.Deserialize<Dictionary<string, string>>(
            ManagedCollectors413Access.Resource(ManagedCollectors413Names.Manifest))!;
        StandardBootOriginal412Access.Require(hashes.Count == 2);
        var images = new List<PreparedBeamImage>();
        foreach (string module in new[] { ManagedCollectors413Names.Literal, ManagedCollectors413Names.Trace })
        {
            byte[] bytes = ManagedCollectors413Access.Resource(module + StandardBootOriginal412Names.Beam);
            StandardBootOriginal412Access.Require(hashes.TryGetValue(module + StandardBootOriginal412Names.Beam, out string? hash)
                && hash == Convert.ToHexString(SHA256.HashData(bytes)));
            PreparedBeamImage image = scope.Prepare(() => registry.PrepareBeam(bytes, dependencies));
            StandardBootOriginal412Access.Require(image.Name == module && image.NativePrimitiveExports.Count == 2);
            images.Add(image);
        }

        return images;
    }
}
