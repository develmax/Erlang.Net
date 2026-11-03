using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class ManagedCaptureAuthority520
{
    internal static async Task WriteAsync(
        string directory,
        string name,
        byte[] selectedCatalog,
        string[] selectedLibraries,
        ModuleRegistry registry,
        IReadOnlyList<ManagedBatchImage513> explicitImages
    )
    {
        // Sample every actual export owner, including autoloaded modules outside
        // explicit imageRows. No metadata access grants loader authority.
        var sampled = registry.Exports.Select(export => new
        {
            export.Module,
            export.Function,
            export.Arity,
            Owner = registry.SourceOwner(export.Module, export.Function, export.Arity)
        }).ToArray();
        var dependencySets = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var owners = sampled.GroupBy(row => row.Owner, ReferenceEqualityComparer.Instance).Select(group =>
        {
            PreparedBeamImage? prepared = group.Key as PreparedBeamImage;
            string? dependencyHash = null;
            if (prepared is not null)
            {
                string[] actual = prepared.RegistryModuleDependencies.OrderBy(value => value, StringComparer.Ordinal).ToArray();
                dependencyHash = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(actual)));
                dependencySets.TryAdd(dependencyHash, actual);
            }

            return new
            {
                SourceOwnerType = group.Key?.GetType().FullName,
                PreparedModule = prepared?.Name,
                OriginalBeamSha256 = prepared?.OriginalBeamSha256,
                RegistryModuleDependenciesSha256 = dependencyHash,
                OutsideExplicitImageRows = prepared is not null && !explicitImages.Any(image => image.Module == prepared.Name),
                MatchedExplicitRawImage = prepared is not null && explicitImages.Any(image =>
                    image.Module == prepared.Name && image.Sha256 == prepared.OriginalBeamSha256),
                OriginalReferenceComparisonPerformed = false,
                ExportBindings = group.Select(row => new { row.Module, row.Function, row.Arity })
                    .OrderBy(row => row.Module, StringComparer.Ordinal).ThenBy(row => row.Function, StringComparer.Ordinal)
                    .ThenBy(row => row.Arity).ToArray()
            };
        }).ToArray();
        var receipt = new
        {
            SnapshotKind = ManagedCaptureAuthority520Names.SnapshotKind,
            SelectedCatalog = new
            {
                Resource = selectedCatalog.Length == 0 ? null : StandardBootOriginal412Names.Prefix + StandardBootOriginal412Names.Dependencies,
                RawSha256 = selectedCatalog.Length == 0 ? null : Convert.ToHexString(SHA256.HashData(selectedCatalog)),
                Count = selectedLibraries.Length,
                Names = selectedLibraries
            },
            DependencySetHashEncoding = ManagedCaptureAuthority520Names.Encoding,
            RegistryModuleDependencySets = dependencySets,
            ActualSourceOwners = owners,
            AtomicRegistrySnapshotClaimed = false,
            OriginalCompatibilityClaimed = false,
            WholeContractsClosed = 0
        };
        await File.WriteAllTextAsync(Path.Combine(directory, name),
            JsonSerializer.Serialize(receipt, new JsonSerializerOptions { WriteIndented = true }));
    }
}
