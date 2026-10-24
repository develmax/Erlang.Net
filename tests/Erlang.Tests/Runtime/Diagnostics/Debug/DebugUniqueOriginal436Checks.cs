using System.Text.Json;
using Erlang;
namespace Erlang.Tests;

internal static class DebugUniqueOriginal436Checks
{
    internal static async Task Run()
    {
        string output = Path.GetFullPath(Path.Combine(DebugUniqueOriginal436Names.Output,
            System.Guid.NewGuid().ToString(DebugUniqueOriginal436Names.Guid)));
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
                var declared = new BeamDependencySelection([DebugUniqueOriginal436Names.Debug,
                    DebugUniqueOriginal436Names.Runtime, DebugUniqueOriginal436Names.Subject, DebugUniqueOriginal436Names.Module]);
                PreparedBeamImage facet = DebugUniqueOriginal436Preparation.Prepare(registry, declared);
                PreparedBeamImage worker = registry.PrepareBeam(
                    DebugUniqueOriginal436Access.Resource(DebugUniqueOriginal436Names.WorkerBeam), declared);
                PreparedBeamImage observer = registry.PrepareBeam(
                    DebugUniqueOriginal436Access.Resource(DebugUniqueOriginal436Names.ObserverBeam), declared);
                DebugUniqueOriginal436Access.Require(registry.PublishPreparedBeam([facet, worker, observer]).Status == BeamPublicationStatus.Published);
                DebugUniqueOriginal436Preparation.AssertPublished(registry, facet);
                foreach (var image in new[] { worker, observer })
                    foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                        DebugUniqueOriginal436Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
            });
            actor = new RuntimeNodeFoundationHostActor(owner);
            RuntimeNodeFoundationHostActor caller = actor;

            async Task Capture(string function, string file)
            {
                try
                {
                    Term value = await caller.InvokeAsync(async context => await owner.Modules.Call(context,
                        DebugUniqueOriginal436Names.Module, function));
                    BitString wire = (BitString)await caller.InvokeAsync(async context => await owner.Modules.Call(
                        context,
                        DebugUniqueOriginal436Names.Runtime,
                        DebugUniqueOriginal436Names.Encode,
                        value
                    ));
                    captured.Add(file, (value, wire.ToArray()));
                    File.WriteAllBytes(Path.Combine(output, DebugUniqueOriginal436Names.Actual + file), wire.ToArray());
                    File.WriteAllText(
                        Path.Combine(output, DebugUniqueOriginal436Names.Actual + file + DebugUniqueOriginal436Names.Json),
                        JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(value))
                    );
                }
                catch (Exception error)
                {
                    errors.Add(new { Route = file, Failure = error.ToString() });
                }
            }
            await Capture(DebugUniqueOriginal436Names.Run, DebugUniqueOriginal436Names.Values);
            await Capture(DebugUniqueOriginal436Names.Stacks, DebugUniqueOriginal436Names.StackValues);
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
        foreach (string file in new[] { DebugUniqueOriginal436Names.Values, DebugUniqueOriginal436Names.StackValues })
        {
            byte[] bytes = DebugUniqueOriginal436Access.Resource(file);
            Term expected = OwnedFunctionOriginal407Terms.Decode(bytes);
            File.WriteAllBytes(Path.Combine(output, DebugUniqueOriginal436Names.Expected + file), bytes);
            File.WriteAllText(
                Path.Combine(output, DebugUniqueOriginal436Names.Expected + file + DebugUniqueOriginal436Names.Json),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expected))
            );
            bool present = captured.TryGetValue(file, out var actual);
            whole[file] = present && expected.Equals(actual.Term);
            raw[file] = present && bytes.AsSpan().SequenceEqual(actual.Bytes);
            if (!present) continue;
            equal += file == DebugUniqueOriginal436Names.Values
                ? IoPrintableOriginal404Comparison.Rows(
                    expected,
                    actual.Term,
                    file,
                    DebugUniqueOriginal436Names.Rows,
                    differences,
                    ref examined
                )
                : IoPrintableOriginal404Comparison.Profiles(
                    expected,
                    actual.Term,
                    DebugUniqueOriginal436Names.Rows,
                    differences,
                    ref examined
                );
        }
        File.WriteAllText(
            Path.Combine(output, DebugUniqueOriginal436Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = DebugUniqueOriginal436Names.WholeRows,
                Examined = examined,
                Equal = equal,
                WholeTerms = whole,
                WholeRawEtf = raw,
                Errors = errors,
                Differences = differences,
                SourceDerivedTwoNativeFacet = true,
                WholeOriginalDebug20Claim = false,
                MatchedWorkerArity = 2,
                Projection = false,
                ApiOrFullContractPromotion = false
            })
        );
        DebugUniqueOriginal436Access.Require(errors.Count == 0 && differences.Count == 0
            && examined == DebugUniqueOriginal436Names.WholeRows && equal == examined
            && whole.Values.All(value => value) && raw.Values.All(value => value));
    }
}
