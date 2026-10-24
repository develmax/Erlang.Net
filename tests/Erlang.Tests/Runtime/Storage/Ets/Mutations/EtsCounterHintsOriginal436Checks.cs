using System.Text.Json;
using Erlang;
namespace Erlang.Tests;

internal static class EtsCounterHintsOriginal436Checks
{
    internal static async Task Run()
    {
        string output = Path.GetFullPath(Path.Combine(EtsCounterHintsOriginal436Names.Output,
            System.Guid.NewGuid().ToString(EtsCounterHintsOriginal436Names.Guid)));
        Directory.CreateDirectory(output);
        var captured = new Dictionary<string, (Term Term, byte[] Bytes)>();
        var errors = new List<object>();
        ProcessRuntime? runtime = null;
        RuntimeNodeFoundationHostActor? actor = null;
        try
        {
            runtime = new ProcessRuntime();
            ProcessRuntime owner = runtime;
            await owner.InitializeModulesAsync(registry =>
            {
                var declared = new BeamDependencySelection([EtsCounterHintsOriginal436Names.Ets,
                    EtsCounterHintsOriginal436Names.Runtime, EtsCounterHintsOriginal436Names.Subject, EtsCounterHintsOriginal436Names.Module]);
                RuntimeNativePrimitiveProviders.Register(owner, registry, RuntimeNativePrimitives.EtsStorageCore);
                PreparedBeamImage facet = EtsInfoOriginal422Preparation.Prepare(owner, registry);
                PreparedBeamImage worker = registry.PrepareBeam(
                    EtsCounterHintsOriginal436Access.Resource(EtsCounterHintsOriginal436Names.WorkerBeam), declared);
                PreparedBeamImage observer = registry.PrepareBeam(
                    EtsCounterHintsOriginal436Access.Resource(EtsCounterHintsOriginal436Names.ObserverBeam), declared);
                EtsCounterHintsOriginal436Access.Require(registry.PublishPreparedBeam([facet, worker, observer]).Status == BeamPublicationStatus.Published);
                EtsInfoOriginal422Preparation.AssertPublished(registry, facet);
                foreach (var image in new[] { worker, observer })
                    foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                        EtsCounterHintsOriginal436Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
            });
            actor = new RuntimeNodeFoundationHostActor(owner);
            RuntimeNodeFoundationHostActor caller = actor;

            async Task Capture(string function, string file)
            {
                try
                {
                    Term value = await caller.InvokeAsync(async context => await owner.Modules.Call(context,
                        EtsCounterHintsOriginal436Names.Module, function));
                    BitString wire = (BitString)await caller.InvokeAsync(async context => await owner.Modules.Call(
                        context,
                        EtsCounterHintsOriginal436Names.Runtime,
                        EtsCounterHintsOriginal436Names.Encode,
                        value
                    ));
                    captured.Add(file, (value, wire.ToArray()));
                    File.WriteAllBytes(Path.Combine(output, EtsCounterHintsOriginal436Names.Actual + file), wire.ToArray());
                    File.WriteAllText(
                        Path.Combine(output, EtsCounterHintsOriginal436Names.Actual + file + EtsCounterHintsOriginal436Names.Json),
                        JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(value))
                    );
                }
                catch (Exception error)
                {
                    errors.Add(new { Route = file, Failure = error.ToString() });
                }
            }
            await Capture(EtsCounterHintsOriginal436Names.Run, EtsCounterHintsOriginal436Names.Values);
            await Capture(EtsCounterHintsOriginal436Names.Stacks, EtsCounterHintsOriginal436Names.StackValues);
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
        foreach (string file in new[] { EtsCounterHintsOriginal436Names.Values, EtsCounterHintsOriginal436Names.StackValues })
        {
            byte[] bytes = EtsCounterHintsOriginal436Access.Resource(file);
            Term expected = OwnedFunctionOriginal407Terms.Decode(bytes);
            File.WriteAllBytes(Path.Combine(output, EtsCounterHintsOriginal436Names.Expected + file), bytes);
            File.WriteAllText(
                Path.Combine(output, EtsCounterHintsOriginal436Names.Expected + file + EtsCounterHintsOriginal436Names.Json),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expected))
            );
            bool present = captured.TryGetValue(file, out var actual);
            whole[file] = present && expected.Equals(actual.Term);
            raw[file] = present && bytes.AsSpan().SequenceEqual(actual.Bytes);
            if (!present) continue;
            equal += file == EtsCounterHintsOriginal436Names.Values
                ? IoPrintableOriginal404Comparison.Rows(
                    expected,
                    actual.Term,
                    file,
                    EtsCounterHintsOriginal436Names.Rows,
                    differences,
                    ref examined
                )
                : IoPrintableOriginal404Comparison.Profiles(
                    expected,
                    actual.Term,
                    EtsCounterHintsOriginal436Names.Rows,
                    differences,
                    ref examined
                );
        }
        File.WriteAllText(
            Path.Combine(output, EtsCounterHintsOriginal436Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = EtsCounterHintsOriginal436Names.WholeRows,
                Examined = examined,
                Equal = equal,
                WholeTerms = whole,
                WholeRawEtf = raw,
                Errors = errors,
                Differences = differences,
                SourceDerivedFiftyOneNativeFacet = true,
                WholeOriginalEts51Claim = false,
                OwnPidArgumentSourcePredicate = true,
                PriorNativePidRawDifferencesRetained = true,
                MatchedWorkerArity = 2,
                Projection = false,
                ApiOrFullContractPromotion = false
            })
        );
        EtsCounterHintsOriginal436Access.Require(errors.Count == 0 && differences.Count == 0
            && examined == EtsCounterHintsOriginal436Names.WholeRows && equal == examined
            && whole.Values.All(value => value) && raw.Values.All(value => value));
    }
}
