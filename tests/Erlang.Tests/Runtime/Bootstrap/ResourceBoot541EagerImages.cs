using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class ResourceBoot541EagerImages
{
    internal static async Task Prepare(
        ProcessRuntime runtime,
        JsonElement plan,
        string[] dependencies,
        string directory
    )
    {
        string stockPath = plan.GetProperty("StockPlanPath").GetString()!;
        StandardBootOriginal412Access.Require(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(stockPath))) == ResourceBoot541Names.StockPlanSha256
            && plan.GetProperty("StockPlanSha256").GetString() == ResourceBoot541Names.StockPlanSha256);
        using var stock = JsonDocument.Parse(File.ReadAllBytes(stockPath));
        var sourceRows = stock.RootElement.GetProperty("OriginalLibraries").EnumerateArray().ToDictionary(row => row.GetProperty("Module").GetString()!, StringComparer.Ordinal);
        string[] selected = stock.RootElement.GetProperty("Jobs").EnumerateArray().Single(job => job.GetProperty("Job").GetString() == "portable-errors")
            .GetProperty("LibraryModules").EnumerateArray().Select(value => value.GetString()!).ToArray();
        var images = JsonSerializer.Deserialize<ManagedBatchImage513[]>(plan.GetProperty("EagerImages").GetRawText())!;
        StandardBootOriginal412Access.Require(images.Length == ResourceBoot541Names.StockImageCount
            && images.Select(image => image.Module).Distinct(StringComparer.Ordinal).Count() == images.Length);
        StandardBootOriginal412Access.Require(selected.ToHashSet(StringComparer.Ordinal).SetEquals(images.Select(image => image.Module)));
        foreach (var image in images)
            StandardBootOriginal412Access.Require(sourceRows[image.Module].GetProperty("Sha256").GetString() == image.Sha256
                && Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(image.Path))) == image.Sha256);
        await runtime.InitializeModulesAsync(registry =>
        {
            var publication = new List<PreparedBeamImage>();
            var failures = new List<Exception>();
            var diagnostics = new List<object>();
            var preparedRows = new List<object>();
            foreach (var image in images)
            {
                try
                {
                    // This is the same source-owner equality gate and preparation
                    // as ManagedContractBatch513 before its successful original boot.
                    PreparedBeamImage prepared = registry.PrepareBeam(File.ReadAllBytes(image.Path), new BeamDependencySelection(dependencies));
                    StandardBootOriginal412Access.Require(prepared.Name == image.Module && prepared.OriginalBeamSha256 == image.Sha256);
                    object? existing = registry.SourceOwner(image.Module, ResourceBoot540Names.ModuleInfo, 0);
                    if (existing is PreparedBeamImage original)
                    {
                        StandardBootOriginal412Access.Require(original.OriginalBeamSha256 == image.Sha256
                            && original.NativeMd5.Equals(prepared.NativeMd5)
                            && original.Attributes.Equals(prepared.Attributes)
                            && original.Compilation.Equals(prepared.Compilation)
                            && original.AttributeProvenance == prepared.AttributeProvenance
                            && original.CompilationProvenance == prepared.CompilationProvenance);
                    }
                    else publication.Add(prepared);
                    preparedRows.Add(new { image.Module, image.Path, image.Sha256, prepared.HasOnLoad, AlreadyPublished = existing is PreparedBeamImage });
                }
                catch (Exception error)
                {
                    diagnostics.Add(new { image.Module, image.Path, image.Sha256, Error = error.ToString() });
                    failures.Add(new InvalidOperationException(ResourceBoot541Names.PrepareFailure + image.Module, error));
                }
            }
            File.WriteAllText(
                Path.Combine(directory, ResourceBoot541Names.Preparation),
                JsonSerializer.Serialize(new { Images = preparedRows, Failures = diagnostics })
            );
            if (failures.Count != 0) throw new AggregateException(failures);
            // Successful 520 boot publishes all ordinary images before boot and
            // defers the crypto on_load until AFTER boot. Do not run crypto here.
            string[] deferred = publication.Where(image => image.HasOnLoad).Select(image => image.Name).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            string[] admitted = plan.GetProperty("DeferredOnLoad").EnumerateArray().Select(value => value.GetString()!).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            StandardBootOriginal412Access.Require(deferred.SequenceEqual(admitted));
            PreparedBeamImage[] ordinary = publication.Where(image => !image.HasOnLoad).ToArray();
            if (ordinary.Length != 0)
                StandardBootOriginal412Access.Require(registry.PublishPreparedBeam(ordinary).Status == BeamPublicationStatus.Published);
            foreach (var image in images.Where(image => !admitted.Contains(image.Module, StringComparer.Ordinal)))
            {
                StandardBootOriginal412Access.Require(registry.SourceOwner(image.Module, ResourceBoot540Names.ModuleInfo, 0) is PreparedBeamImage owner && owner.OriginalBeamSha256 == image.Sha256);
                foreach (var export in registry.Exports.Where(export => export.Module == image.Module))
                    StandardBootOriginal412Access.Require(registry.SourceOwner(export.Module, export.Function, export.Arity) is PreparedBeamImage original && original.OriginalBeamSha256 == image.Sha256);
            }
        });
    }
}
