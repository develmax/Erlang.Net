using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

// Fresh pinned BEAM inputs only; historical425 resources/expectations unchanged.
internal static class AckOriginal425Replay
{
    internal static async Task Run(
        string inputDirectory,
        string outputDirectory,
        string profile,
        string inputSeal
    )
    {
        bool owned = profile switch
        {
            AckOriginal425ReplayNames.Ordinary => false,
            AckOriginal425ReplayNames.Private => true,
            _ => throw new ArgumentException(nameof(profile))
        };
        string input = Path.GetFullPath(inputDirectory), output = Path.GetFullPath(outputDirectory);
        if (Directory.Exists(output)) throw new IOException(AckOriginal425ReplayNames.OutputExists);
        Directory.CreateDirectory(output);
        string manifestPath = Path.Combine(input, AckOriginal425ReplayNames.Inputs);
        byte[] manifestBytes = File.ReadAllBytes(manifestPath);
        Require(Convert.ToHexString(SHA256.HashData(manifestBytes)) == inputSeal.ToUpperInvariant());
        using var manifest = JsonDocument.Parse(manifestBytes);
        var images = new Dictionary<string, byte[]>();
        foreach (var row in manifest.RootElement.GetProperty(AckOriginal425ReplayNames.Files).EnumerateArray())
        {
            string name = row.GetProperty(AckOriginal425ReplayNames.Name).GetString()!;
            Require(name is AckOriginal425ReplayNames.Facet or AckOriginal425ReplayNames.Worker);
            byte[] bytes = File.ReadAllBytes(Path.Combine(input, name));
            Require(bytes.LongLength == row.GetProperty(AckOriginal425ReplayNames.Bytes).GetInt64()
                && Convert.ToHexString(SHA256.HashData(bytes)) == row.GetProperty(AckOriginal425ReplayNames.Sha).GetString());
            images.Add(name, bytes);
        }
        Require(images.Count == AckOriginal425ReplayNames.ImageCount);
        var errors = new List<string>();
        var captured = new List<string>();
        ProcessRuntime? runtime = null;
        RuntimeNodeFoundationHostActor? actor = null;
        PreparedBeamImage? facet = null, worker = null;
        try
        {
            runtime = OwnedGuest506RuntimeFactory.Create(owned);
            ProcessRuntime owner = runtime;
            var dependencies = new BeamDependencySelection(AckOriginal425ReplayNames.Dependencies);
            facet = owner.Modules.PrepareBeam(images[AckOriginal425ReplayNames.Facet], dependencies);
            worker = owner.Modules.PrepareBeam(images[AckOriginal425ReplayNames.Worker], dependencies);
            PreparedBeamImage facetImage = facet, workerImage = worker;
            await owner.InitializeModulesAsync(registry =>
            {
                Require(registry.PublishPreparedBeam([facetImage, workerImage]).Status == BeamPublicationStatus.Published);
                Require(facetImage.NativePrimitiveExports.Count == AckOriginal425ReplayNames.NativeCount
                    && AckOriginal425ReplayNames.NativeSignatures.All(signature => facetImage.NativePrimitiveExports.Contains(signature)));
                Require(registry.Exports.Count(export => export.Module == facetImage.Name) == AckOriginal425ReplayNames.FacetExports);
                foreach (var image in new[] { facetImage, workerImage })
                    foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                        Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
            });
            actor = new RuntimeNodeFoundationHostActor(owner);
            RuntimeNodeFoundationHostActor caller = actor;
            foreach (var route in AckOriginal425ReplayNames.Routes)
                try
                {
                    Term value = await caller.InvokeAsync(async context => await owner.Modules.Call(context,
                        AckOriginal425ReplayNames.Module, route.Function));
                    BitString encoded = (BitString)await caller.InvokeAsync(async context => await owner.Modules.Call(
                        context,
                        ErlangModuleNames.Module,
                        AckOriginal425ReplayNames.Encode,
                        value,
                        Term.List(Term.Tuple(Term.A(AckOriginal425ReplayNames.MinorVersion), Term.I(AckOriginal425ReplayNames.MinorVersionValue)))
                    ));
                    File.WriteAllBytes(Path.Combine(output, route.File), encoded.ToArray());
                    captured.Add(route.File);
                }
                catch (Exception error)
                {
                    errors.Add(route.File + Environment.NewLine + error);
                }
        }
        catch (Exception error)
        {
            errors.Add(AckOriginal425ReplayNames.Preparation + Environment.NewLine + error);
        }
        finally
        {
            try
            {
                if (actor is not null) await actor.DisposeAsync();
            }
            catch (Exception error)
            {
                errors.Add(AckOriginal425ReplayNames.ActorCleanup + Environment.NewLine + error);
            }
            try
            {
                if (runtime is not null) await runtime.DisposeAsync();
            }
            catch (Exception error)
            {
                errors.Add(AckOriginal425ReplayNames.RuntimeCleanup + Environment.NewLine + error);
            }
            try
            {
                if (worker is not null) await worker.DisposeAsync();
            }
            catch (Exception error)
            {
                errors.Add(AckOriginal425ReplayNames.WorkerCleanup + Environment.NewLine + error);
            }
            try
            {
                if (facet is not null) await facet.DisposeAsync();
            }
            catch (Exception error)
            {
                errors.Add(AckOriginal425ReplayNames.FacetCleanup + Environment.NewLine + error);
            }
        }
        File.WriteAllText(
            Path.Combine(output, AckOriginal425ReplayNames.Receipt),
            JsonSerializer.Serialize(
                new
                {
                    Passed = errors.Count == 0 && captured.Count == AckOriginal425ReplayNames.RouteCount,
                    Profile = profile,
                    ManagedInputSha256 = inputSeal,
                    Captured = captured,
                    Errors = errors,
                    WholeValueRows = AckOriginal425ReplayNames.Rows,
                    StackProfileCount = AckOriginal425ReplayNames.StackProfiles,
                    WholeOutcomeRows = AckOriginal425ReplayNames.WholeRows,
                    Projection = false,
                    HistoricalExpectationsChanged = false,
                    ComparisonPerformedByCollector = true
                },
                new JsonSerializerOptions { WriteIndented = true }
            )
        );
        Require(errors.Count == 0 && captured.Count == AckOriginal425ReplayNames.RouteCount);
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(AckOriginal425ReplayNames.Failure);
    }
}
