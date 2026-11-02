using System.Text.Json;
using Erlang;
namespace Erlang.Tests;

internal static class SpawnMonitorOriginal431Checks
{
    internal static async Task Run()
    {
        string output = Path.GetFullPath(Path.Combine(SpawnMonitorOriginal431Names.Output,
            System.Guid.NewGuid().ToString(SpawnMonitorOriginal431Names.Guid)));
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
                RuntimeBootstrapDistributionProfile429.Register(owner, registry);
                RuntimeBootstrapDistChannel429.Register(owner, registry);
                RuntimeBootstrapDistSpawn430.Register(owner, registry);
            });
            var declared = new BeamDependencySelection([SpawnMonitorOriginal431Names.Runtime, SpawnMonitorOriginal431Names.File, SpawnMonitorOriginal431Names.Internal]);
            await PreparedBeamFixtureImages825.PrepareAndPublishAsync(
                owner,
                [SpawnMonitorOriginal431Access.Resource(SpawnMonitorOriginal431Names.FacetBeam), SpawnMonitorOriginal431Access.Resource(SpawnMonitorOriginal431Names.WorkerBeam)],
                declared,
                (registry, images) =>
                {
                    PreparedBeamImage facet = images[0];
                    PreparedBeamImage worker = images[1];
                    SpawnMonitorOriginal431Access.Require(facet.Name == SpawnMonitorOriginal431Names.Internal
                        && facet.NativePrimitiveExports.Count == SpawnMonitorOriginal431Names.NativeCount
                        && SpawnMonitorOriginal431Names.NativeSignatures.All(signature => facet.NativePrimitiveExports.Contains(signature)));
                    SpawnMonitorOriginal431Access.Require(registry.PublishPreparedBeam([facet, worker]).Status == BeamPublicationStatus.Published);
                    foreach (var image in new[] { facet, worker })
                        foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                            SpawnMonitorOriginal431Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
                    SpawnMonitorOriginal431Access.Require(registry.Exports.Count(export => export.Module == facet.Name) == SpawnMonitorOriginal431Names.FacetExports);
                }
            );
            actor = new RuntimeNodeFoundationHostActor(owner);
            RuntimeNodeFoundationHostActor caller = actor;

            async Task Capture(string function, string file)
            {
                try
                {
                    Term value = await caller.InvokeAsync(async context => await owner.Modules.Call(context,
                        SpawnMonitorOriginal431Names.Module, function));
                    BitString wire = (BitString)await caller.InvokeAsync(async context => await owner.Modules.Call(
                        context,
                        SpawnMonitorOriginal431Names.Runtime,
                        SpawnMonitorOriginal431Names.Encode,
                        value
                    ));
                    captured.Add(file, (value, wire.ToArray()));
                    File.WriteAllBytes(Path.Combine(output, SpawnMonitorOriginal431Names.Actual + file), wire.ToArray());
                    File.WriteAllText(
                        Path.Combine(output, SpawnMonitorOriginal431Names.Actual + file + SpawnMonitorOriginal431Names.Json),
                        JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(value))
                    );
                }
                catch (Exception error)
                {
                    errors.Add(new { Route = file, Failure = error.ToString() });
                }
            }
            await Capture(SpawnMonitorOriginal431Names.Run, SpawnMonitorOriginal431Names.Values);
            await Capture(SpawnMonitorOriginal431Names.Stacks, SpawnMonitorOriginal431Names.StackValues);
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
        foreach (string file in new[] { SpawnMonitorOriginal431Names.Values, SpawnMonitorOriginal431Names.StackValues })
        {
            byte[] bytes = SpawnMonitorOriginal431Access.Resource(file);
            Term expected = OwnedFunctionOriginal407Terms.Decode(bytes);
            File.WriteAllBytes(Path.Combine(output, SpawnMonitorOriginal431Names.Expected + file), bytes);
            File.WriteAllText(
                Path.Combine(output, SpawnMonitorOriginal431Names.Expected + file + SpawnMonitorOriginal431Names.Json),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expected))
            );
            bool present = captured.TryGetValue(file, out var actual);
            whole[file] = present && expected.Equals(actual.Term);
            raw[file] = present && bytes.AsSpan().SequenceEqual(actual.Bytes);
            if (!present) continue;
            equal += file == SpawnMonitorOriginal431Names.Values
                ? IoPrintableOriginal404Comparison.Rows(
                    expected,
                    actual.Term,
                    file,
                    SpawnMonitorOriginal431Names.Rows,
                    differences,
                    ref examined
                )
                : IoPrintableOriginal404Comparison.Profiles(
                    expected,
                    actual.Term,
                    SpawnMonitorOriginal431Names.Rows,
                    differences,
                    ref examined
                );
        }
        File.WriteAllText(
            Path.Combine(output, SpawnMonitorOriginal431Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = SpawnMonitorOriginal431Names.WholeRows,
                Examined = examined,
                Equal = equal,
                WholeTerms = whole,
                WholeRawEtf = raw,
                Errors = errors,
                Differences = differences,
                OriginalCompiledWorkerUnchanged = true,
                SourceDerivedThreeStubFacet = true,
                ActualSourceOwnershipChecked = true,
                ActualNativeSpawnMonitorMissingFlagsProtocol = true,
                SourceOwnProtocolPredicates = true,
                ConnectedOrPendingSuccessOriginalClaim = false,
                WholeOriginalInternalImageClaim = false,
                PhysicalNumericEqualityClaim = false,
                ProfileArity = 3,
                Projection = false,
                ApiOrFullContractPromotion = false
            })
        );
        SpawnMonitorOriginal431Access.Require(errors.Count == 0 && differences.Count == 0
            && examined == SpawnMonitorOriginal431Names.WholeRows && equal == examined
            && whole.Values.All(value => value) && raw.Values.All(value => value));
    }
}
