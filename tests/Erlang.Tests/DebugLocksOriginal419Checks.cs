using System.Text.Json;
using Erlang;
namespace Erlang.Tests;

internal static class DebugLocksOriginal419Checks
{
    internal static async Task Run()
    {
        string output = Path.GetFullPath(Path.Combine(DebugLocksOriginal419Names.Output,
            System.Guid.NewGuid().ToString(DebugLocksOriginal419Names.Guid)));
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
                var declared = new BeamDependencySelection([DebugLocksOriginal419Names.Debug,
                    DebugLocksOriginal419Names.Runtime, DebugLocksOriginal419Names.Subject, DebugLocksOriginal419Names.Module]);
                PreparedBeamImage facet = DebugLocksOriginal419Preparation.Prepare(registry, declared);
                PreparedBeamImage worker = registry.PrepareBeam(
                    DebugLocksOriginal419Access.Resource(DebugLocksOriginal419Names.WorkerBeam), declared);
                PreparedBeamImage observer = registry.PrepareBeam(
                    DebugLocksOriginal419Access.Resource(DebugLocksOriginal419Names.ObserverBeam), declared);
                DebugLocksOriginal419Access.Require(registry.PublishPreparedBeam([facet, worker, observer]).Status == BeamPublicationStatus.Published);
                DebugLocksOriginal419Preparation.AssertPublished(registry, facet);
                foreach (var image in new[] { worker, observer })
                    foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                        DebugLocksOriginal419Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
            });
            actor = new RuntimeNodeFoundationHostActor(owner);
            RuntimeNodeFoundationHostActor caller = actor;

            async Task Capture(string function, string file)
            {
                try
                {
                    Term value = await caller.InvokeAsync(async context => await owner.Modules.Call(context,
                        DebugLocksOriginal419Names.Module, function));
                    BitString wire = (BitString)await caller.InvokeAsync(async context => await owner.Modules.Call(
                        context,
                        DebugLocksOriginal419Names.Runtime,
                        DebugLocksOriginal419Names.Encode,
                        value
                    ));
                    captured.Add(file, (value, wire.ToArray()));
                    File.WriteAllBytes(Path.Combine(output, DebugLocksOriginal419Names.Actual + file), wire.ToArray());
                    File.WriteAllText(
                        Path.Combine(output, DebugLocksOriginal419Names.Actual + file + DebugLocksOriginal419Names.Json),
                        JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(value))
                    );
                }
                catch (Exception error)
                {
                    errors.Add(new { Route = file, Failure = error.ToString() });
                }
            }
            await Capture(DebugLocksOriginal419Names.Run, DebugLocksOriginal419Names.Values);
            await Capture(DebugLocksOriginal419Names.Stacks, DebugLocksOriginal419Names.StackValues);
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
        foreach (string file in new[] { DebugLocksOriginal419Names.Values, DebugLocksOriginal419Names.StackValues })
        {
            byte[] bytes = DebugLocksOriginal419Access.Resource(file);
            Term expected = OwnedFunctionOriginal407Terms.Decode(bytes);
            File.WriteAllBytes(Path.Combine(output, DebugLocksOriginal419Names.Expected + file), bytes);
            File.WriteAllText(
                Path.Combine(output, DebugLocksOriginal419Names.Expected + file + DebugLocksOriginal419Names.Json),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expected))
            );
            bool present = captured.TryGetValue(file, out var actual);
            whole[file] = present && expected.Equals(actual.Term);
            raw[file] = present && bytes.AsSpan().SequenceEqual(actual.Bytes);
            if (!present) continue;
            equal += file == DebugLocksOriginal419Names.Values
                ? IoPrintableOriginal404Comparison.Rows(
                    expected,
                    actual.Term,
                    file,
                    DebugLocksOriginal419Names.Rows,
                    differences,
                    ref examined
                )
                : IoPrintableOriginal404Comparison.Profiles(
                    expected,
                    actual.Term,
                    DebugLocksOriginal419Names.Rows,
                    differences,
                    ref examined
                );
        }
        File.WriteAllText(
            Path.Combine(output, DebugLocksOriginal419Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = DebugLocksOriginal419Names.WholeRows,
                Examined = examined,
                Equal = equal,
                WholeTerms = whole,
                WholeRawEtf = raw,
                Errors = errors,
                Differences = differences,
                SourceDerivedFourNativeFacet = true,
                WholeOriginalDebug20Claim = false,
                ProfileArity = 3,
                Projection = false,
                ApiOrFullContractPromotion = false
            })
        );
        DebugLocksOriginal419Access.Require(errors.Count == 0 && differences.Count == 0
            && examined == DebugLocksOriginal419Names.WholeRows && equal == examined
            && whole.Values.All(value => value) && raw.Values.All(value => value));
    }
}
