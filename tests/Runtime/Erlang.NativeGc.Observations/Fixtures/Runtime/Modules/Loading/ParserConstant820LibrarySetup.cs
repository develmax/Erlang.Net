using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text.Json;
using static Erlang.Tests.ParserConstant820LibraryNames;

namespace Erlang.Tests;

internal static class ParserConstant820LibrarySetup
{
    internal static void RequireDebugOriginal(ModuleRegistry registry)
    {
        object? owner = registry.SourceOwner(Debug, Size, 1);
        if (owner is not PreparedBeamImage image || image.Name != Debug) throw new InvalidOperationException(Owner);
        foreach (var entry in new[] { (Same, 2), (Flat, 1), (Shared, 1) })
            if (!ReferenceEquals(registry.SourceOwner(Debug, entry.Item1, entry.Item2), owner))
                throw new InvalidOperationException(Owner);
    }

    internal static async Task Load(ProcessRuntime runtime)
    {
        string metadata = Environment.GetEnvironmentVariable(MetadataEnvironment) ?? throw new InvalidOperationException(MissingMetadata);
        byte[] metadataBytes = File.ReadAllBytes(metadata);
        if (Convert.ToHexString(SHA256.HashData(metadataBytes)) != MetadataSha256) throw new InvalidDataException(Changed);
        using JsonDocument document = JsonDocument.Parse(metadataBytes);
        var selection = new BeamDependencySelection(document.RootElement.GetProperty(DeclaredModules).EnumerateArray().Select(value => value.GetString()!).ToArray());
        await runtime.InitializeModulesAsync(registry =>
        {
            RuntimeBootstrapDebugSharing425.Register(runtime, registry);
            RuntimeBootstrapAtomics.Register(runtime, registry);
        });
        var images = new List<PreparedBeamImage>();
        var cleanup = new List<Exception>();
        Exception? primary = null;
        try
        {
            foreach (JsonElement asset in document.RootElement.GetProperty(Assets).EnumerateArray())
            {
                byte[] bytes = File.ReadAllBytes(asset.GetProperty(ParserConstant820LibraryNames.Path).GetString()!);
                if (!string.Equals(Convert.ToHexString(SHA256.HashData(bytes)), asset.GetProperty(Sha256).GetString(), StringComparison.Ordinal))
                    throw new InvalidDataException(Changed);
                images.EnsureCapacity(checked(images.Count + 1));
                cleanup.EnsureCapacity(checked(images.Count + 2));
                images.Add(runtime.Modules.PrepareBeam(bytes, selection));
            }
            if (!images.Any(image => image.Name == Debug) || !images.Any(image => image.Name == Trees))
                throw new InvalidDataException(MissingImage);
            await runtime.InitializeModulesAsync(registry =>
            {
                if (registry.PublishPreparedBeam(images.ToArray()).Status != BeamPublicationStatus.Published)
                    throw new InvalidOperationException(Publication);
                foreach (PreparedBeamImage image in images)
                    foreach (var export in registry.Exports.Where(value => value.Module == image.Name))
                        if (!ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image))
                            throw new InvalidOperationException(Owner);
                foreach (var entry in new[] { (Size, 1), (Same, 2), (Flat, 1), (Shared, 1) })
                    if (!registry.HasExport(Debug, entry.Item1, entry.Item2)) throw new InvalidOperationException(MissingImage);
            });
        }
        catch (Exception failure)
        {
            primary = failure;
        }
        foreach (PreparedBeamImage image in images)
            try
            {
                await image.DisposeAsync();
            }
            catch (Exception failure)
            {
                cleanup.Add(failure);
            }
        if (cleanup.Count != 0)
        {
            if (primary is not null) cleanup.Insert(0, primary);
            throw new AggregateException(cleanup);
        }
        if (primary is not null) ExceptionDispatchInfo.Capture(primary).Throw();
    }
}
