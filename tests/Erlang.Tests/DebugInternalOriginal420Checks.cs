using System.Text.Json;
using Erlang;
namespace Erlang.Tests;

internal static class DebugInternalOriginal420Checks
{
    internal static async Task Run()
    {
        string output = Path.GetFullPath(Path.Combine(DebugInternalOriginal420Names.Output,
            System.Guid.NewGuid().ToString(DebugInternalOriginal420Names.Guid)));
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
                var declared = new BeamDependencySelection([DebugInternalOriginal420Names.Debug,
                    DebugInternalOriginal420Names.Runtime, DebugInternalOriginal420Names.Subject, DebugInternalOriginal420Names.Module]);
                PreparedBeamImage facet = DebugInternalOriginal420Preparation.Prepare(registry, declared);
                PreparedBeamImage worker = registry.PrepareBeam(
                    DebugInternalOriginal420Access.Resource(DebugInternalOriginal420Names.WorkerBeam), declared);
                PreparedBeamImage observer = registry.PrepareBeam(
                    DebugInternalOriginal420Access.Resource(DebugInternalOriginal420Names.ObserverBeam), declared);
                DebugInternalOriginal420Access.Require(registry.PublishPreparedBeam([facet, worker, observer]).Status == BeamPublicationStatus.Published);
                DebugInternalOriginal420Preparation.AssertPublished(registry, facet);
                foreach (var image in new[] { worker, observer })
                    foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                        DebugInternalOriginal420Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
            });
            actor = new RuntimeNodeFoundationHostActor(owner);
            RuntimeNodeFoundationHostActor caller = actor;

            async Task Capture(string function, string file)
            {
                try
                {
                    Term value = await caller.InvokeAsync(async context => await owner.Modules.Call(context,
                        DebugInternalOriginal420Names.Module, function));
                    BitString wire = (BitString)await caller.InvokeAsync(async context => await owner.Modules.Call(
                        context,
                        DebugInternalOriginal420Names.Runtime,
                        DebugInternalOriginal420Names.Encode,
                        value
                    ));
                    captured.Add(file, (value, wire.ToArray()));
                    File.WriteAllBytes(Path.Combine(output, DebugInternalOriginal420Names.Actual + file), wire.ToArray());
                    File.WriteAllText(
                        Path.Combine(output, DebugInternalOriginal420Names.Actual + file + DebugInternalOriginal420Names.Json),
                        JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(value))
                    );
                }
                catch (Exception error)
                {
                    errors.Add(new { Route = file, Failure = error.ToString() });
                }
            }
            await Capture(DebugInternalOriginal420Names.Run, DebugInternalOriginal420Names.Values);
            await Capture(DebugInternalOriginal420Names.Stacks, DebugInternalOriginal420Names.StackValues);
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
        foreach (string file in new[] { DebugInternalOriginal420Names.Values, DebugInternalOriginal420Names.StackValues })
        {
            byte[] bytes = DebugInternalOriginal420Access.Resource(file);
            Term expected = OwnedFunctionOriginal407Terms.Decode(bytes);
            File.WriteAllBytes(Path.Combine(output, DebugInternalOriginal420Names.Expected + file), bytes);
            File.WriteAllText(
                Path.Combine(output, DebugInternalOriginal420Names.Expected + file + DebugInternalOriginal420Names.Json),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expected))
            );
            bool present = captured.TryGetValue(file, out var actual);
            whole[file] = present && expected.Equals(actual.Term);
            raw[file] = present && bytes.AsSpan().SequenceEqual(actual.Bytes);
            if (!present) continue;
            equal += file == DebugInternalOriginal420Names.Values
                ? IoPrintableOriginal404Comparison.Rows(
                    expected,
                    actual.Term,
                    file,
                    DebugInternalOriginal420Names.Rows,
                    differences,
                    ref examined
                )
                : IoPrintableOriginal404Comparison.Profiles(
                    expected,
                    actual.Term,
                    DebugInternalOriginal420Names.Rows,
                    differences,
                    ref examined
                );
        }
        File.WriteAllText(
            Path.Combine(output, DebugInternalOriginal420Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = DebugInternalOriginal420Names.WholeRows,
                Examined = examined,
                Equal = equal,
                WholeTerms = whole,
                WholeRawEtf = raw,
                Errors = errors,
                Differences = differences,
                SourceDerivedTwoNativeFacet = true,
                WholeOriginalDebug20Claim = false,
                ProfileArity = 3,
                Projection = false,
                ApiOrFullContractPromotion = false
            })
        );
        DebugInternalOriginal420Access.Require(errors.Count == 0 && differences.Count == 0
            && examined == DebugInternalOriginal420Names.WholeRows && equal == examined
            && whole.Values.All(value => value) && raw.Values.All(value => value));
    }
}
