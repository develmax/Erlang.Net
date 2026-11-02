using System.Text.Json;
using Erlang;
namespace Erlang.Tests;

internal static class DistSpawnOriginal430Checks
{
    internal static async Task Run()
    {
        string output = Path.GetFullPath(Path.Combine(DistSpawnOriginal430Names.Output,
            System.Guid.NewGuid().ToString(DistSpawnOriginal430Names.Guid)));
        Directory.CreateDirectory(output);
        var captured = new Dictionary<string, (Term Term, byte[] Bytes)>();
        var errors = new List<object>();
        ProcessRuntime? runtime = null;
        RuntimeNodeFoundationHostActor? actor = null;
        try
        {
            runtime = new ProcessRuntime(1);
            ProcessRuntime owner = runtime;
            await owner.InitializeModulesAsync(registry =>
            {
                RuntimeBootstrapDistSpawn430.Register(owner, registry);
            });
            var declared = new BeamDependencySelection([DistSpawnOriginal430Names.Runtime, DistSpawnOriginal430Names.File, DistSpawnOriginal430Names.Internal]);
            await PreparedBeamFixtureImages825.PrepareAndPublishAsync(
                owner,
                [DistSpawnOriginal430Access.Resource(DistSpawnOriginal430Names.FacetBeam), DistSpawnOriginal430Access.Resource(DistSpawnOriginal430Names.WorkerBeam)],
                declared,
                (registry, images) =>
                {
                    PreparedBeamImage facet = images[0];
                    PreparedBeamImage worker = images[1];
                    DistSpawnOriginal430Access.Require(facet.Name == DistSpawnOriginal430Names.Internal
                        && facet.NativePrimitiveExports.Count == DistSpawnOriginal430Names.NativeCount
                        && DistSpawnOriginal430Names.NativeSignatures.All(signature => facet.NativePrimitiveExports.Contains(signature)));
                    DistSpawnOriginal430Access.Require(registry.PublishPreparedBeam([facet, worker]).Status == BeamPublicationStatus.Published);
                    foreach (var image in new[] { facet, worker })
                        foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                            DistSpawnOriginal430Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
                    DistSpawnOriginal430Access.Require(registry.Exports.Count(export => export.Module == facet.Name) == DistSpawnOriginal430Names.FacetExports);
                }
            );
            actor = new RuntimeNodeFoundationHostActor(owner);
            RuntimeNodeFoundationHostActor caller = actor;

            async Task Capture(string function, string file)
            {
                try
                {
                    Term value = await caller.InvokeAsync(async context => await owner.Modules.Call(context,
                        DistSpawnOriginal430Names.Module, function));
                    BitString wire = (BitString)await caller.InvokeAsync(async context => await owner.Modules.Call(
                        context,
                        DistSpawnOriginal430Names.Runtime,
                        DistSpawnOriginal430Names.Encode,
                        value
                    ));
                    captured.Add(file, (value, wire.ToArray()));
                    File.WriteAllBytes(Path.Combine(output, DistSpawnOriginal430Names.Actual + file), wire.ToArray());
                    File.WriteAllText(
                        Path.Combine(output, DistSpawnOriginal430Names.Actual + file + DistSpawnOriginal430Names.Json),
                        JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(value))
                    );
                }
                catch (Exception error)
                {
                    errors.Add(new { Route = file, Failure = error.ToString() });
                }
            }
            await Capture(DistSpawnOriginal430Names.Run, DistSpawnOriginal430Names.Values);
            await Capture(DistSpawnOriginal430Names.Stacks, DistSpawnOriginal430Names.StackValues);
        }
        catch (Exception error)
        {
            errors.Add(new { Preparation = error.ToString() });
        }
        finally
        {
            if (actor is not null)
                try
                {
                    await actor.DisposeAsync();
                }
                catch (Exception error)
                {
                    errors.Add(new { ActorCleanup = error.ToString() });
                }
            if (runtime is not null)
                try
                {
                    await runtime.DisposeAsync();
                }
                catch (Exception error)
                {
                    errors.Add(new { RuntimeCleanup = error.ToString() });
                }
        }
        // Decode expectations only after both independent routes and real owner cleanup.
        var differences = new List<object>();
        int examined = 0, equal = 0;
        var whole = new Dictionary<string, bool>();
        var raw = new Dictionary<string, bool>();
        foreach (string file in new[] { DistSpawnOriginal430Names.Values, DistSpawnOriginal430Names.StackValues })
        {
            byte[] bytes = DistSpawnOriginal430Access.Resource(file);
            Term expected = OwnedFunctionOriginal407Terms.Decode(bytes);
            File.WriteAllBytes(Path.Combine(output, DistSpawnOriginal430Names.Expected + file), bytes);
            File.WriteAllText(
                Path.Combine(output, DistSpawnOriginal430Names.Expected + file + DistSpawnOriginal430Names.Json),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expected))
            );
            bool present = captured.TryGetValue(file, out var actual);
            whole[file] = present && expected.Equals(actual.Term);
            raw[file] = present && bytes.AsSpan().SequenceEqual(actual.Bytes);
            if (!present) continue;
            equal += file == DistSpawnOriginal430Names.Values
                ? IoPrintableOriginal404Comparison.Rows(
                    expected,
                    actual.Term,
                    file,
                    DistSpawnOriginal430Names.Rows,
                    differences,
                    ref examined
                )
                : IoPrintableOriginal404Comparison.Profiles(
                    expected,
                    actual.Term,
                    DistSpawnOriginal430Names.Rows,
                    differences,
                    ref examined
                );
        }
        File.WriteAllText(
            Path.Combine(output, DistSpawnOriginal430Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = DistSpawnOriginal430Names.WholeRows,
                Examined = examined,
                Equal = equal,
                WholeTerms = whole,
                WholeRawEtf = raw,
                Errors = errors,
                Differences = differences,
                OriginalCompiledWorkerUnchanged = true,
                SourceDerivedThreeStubFacet = true,
                ActualSourceOwnershipChecked = true,
                ActualNativeDistributionSpawnNotAliveProtocol = true,
                SourceOwnProtocolPredicates = true,
                ConnectedOrPendingSuccessOriginalClaim = false,
                WholeOriginalInternalImageClaim = false,
                PhysicalNumericEqualityClaim = false,
                ProfileArity = 3,
                Projection = false,
                ApiOrFullContractPromotion = false
            })
        );
        DistSpawnOriginal430Access.Require(errors.Count == 0 && differences.Count == 0
            && examined == DistSpawnOriginal430Names.WholeRows && equal == examined
            && whole.Values.All(value => value) && raw.Values.All(value => value));
    }
}
