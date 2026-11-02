using System.Text.Json;
using Erlang;
namespace Erlang.Tests;

internal static class DebugSameOriginal420Checks
{
    internal static async Task Run()
    {
        string output = Path.GetFullPath(Path.Combine(DebugSameOriginal420Names.Output,
            System.Guid.NewGuid().ToString(DebugSameOriginal420Names.Guid)));
        Directory.CreateDirectory(output);
        var captured = new Dictionary<string, (Term Term, byte[] Bytes)>();
        var errors = new List<object>();
        ProcessRuntime? runtime = null;
        RuntimeNodeFoundationHostActor? actor = null;
        try
        {
            runtime = new ProcessRuntime();
            ProcessRuntime owner = runtime;
            await global::Erlang.Tests.PreparedBeamFixtureScope171.RunAsync(
                owner,
                (registry, scope) =>
            {
                var declared = new BeamDependencySelection([DebugSameOriginal420Names.Debug,
                                DebugSameOriginal420Names.Runtime, DebugSameOriginal420Names.Subject, DebugSameOriginal420Names.Module]);
                PreparedBeamImage facet = DebugSameOriginal420Preparation.Prepare171(registry, declared, scope);
                PreparedBeamImage worker = scope.Prepare(() => registry.PrepareBeam(
                    DebugSameOriginal420Access.Resource(DebugSameOriginal420Names.WorkerBeam), declared));
                PreparedBeamImage observer = scope.Prepare(() => registry.PrepareBeam(
                    DebugSameOriginal420Access.Resource(DebugSameOriginal420Names.ObserverBeam), declared));

                return ValueTask.FromResult<Action<ModuleRegistry>>(_ =>
                {
                    DebugSameOriginal420Access.Require(registry.PublishPreparedBeam([facet, worker, observer]).Status == BeamPublicationStatus.Published);
                    DebugSameOriginal420Preparation.AssertPublished(registry, facet);
                    foreach (var image in new[] { worker, observer })
                        foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                            DebugSameOriginal420Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
                });
            }
            );
            actor = new RuntimeNodeFoundationHostActor(owner);
            RuntimeNodeFoundationHostActor caller = actor;

            async Task Capture(string function, string file)
            {
                try
                {
                    Term value = await caller.InvokeAsync(async context => await owner.Modules.Call(context,
                        DebugSameOriginal420Names.Module, function));
                    BitString wire = (BitString)await caller.InvokeAsync(async context => await owner.Modules.Call(
                        context,
                        DebugSameOriginal420Names.Runtime,
                        DebugSameOriginal420Names.Encode,
                        value
                    ));
                    captured.Add(file, (value, wire.ToArray()));
                    File.WriteAllBytes(Path.Combine(output, DebugSameOriginal420Names.Actual + file), wire.ToArray());
                    File.WriteAllText(
                        Path.Combine(output, DebugSameOriginal420Names.Actual + file + DebugSameOriginal420Names.Json),
                        JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(value))
                    );
                }
                catch (Exception error)
                {
                    errors.Add(new { Route = file, Failure = error.ToString() });
                }
            }
            await Capture(DebugSameOriginal420Names.Run, DebugSameOriginal420Names.Values);
            await Capture(DebugSameOriginal420Names.Stacks, DebugSameOriginal420Names.StackValues);
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
        foreach (string file in new[] { DebugSameOriginal420Names.Values, DebugSameOriginal420Names.StackValues })
        {
            byte[] bytes = DebugSameOriginal420Access.Resource(file);
            Term expected = OwnedFunctionOriginal407Terms.Decode(bytes);
            File.WriteAllBytes(Path.Combine(output, DebugSameOriginal420Names.Expected + file), bytes);
            File.WriteAllText(
                Path.Combine(output, DebugSameOriginal420Names.Expected + file + DebugSameOriginal420Names.Json),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expected))
            );
            bool present = captured.TryGetValue(file, out var actual);
            whole[file] = present && expected.Equals(actual.Term);
            raw[file] = present && bytes.AsSpan().SequenceEqual(actual.Bytes);
            if (!present) continue;
            equal += file == DebugSameOriginal420Names.Values
                ? IoPrintableOriginal404Comparison.Rows(
                    expected,
                    actual.Term,
                    file,
                    DebugSameOriginal420Names.Rows,
                    differences,
                    ref examined
                )
                : IoPrintableOriginal404Comparison.Profiles(
                    expected,
                    actual.Term,
                    DebugSameOriginal420Names.Rows,
                    differences,
                    ref examined
                );
        }
        File.WriteAllText(
            Path.Combine(output, DebugSameOriginal420Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = DebugSameOriginal420Names.WholeRows,
                Examined = examined,
                Equal = equal,
                WholeTerms = whole,
                WholeRawEtf = raw,
                Errors = errors,
                Differences = differences,
                SourceDerivedOneNativeFacet = true,
                WholeOriginalDebug20Claim = false,
                ProfileArity = 3,
                Projection = false,
                ApiOrFullContractPromotion = false
            })
        );
        DebugSameOriginal420Access.Require(errors.Count == 0 && differences.Count == 0
            && examined == DebugSameOriginal420Names.WholeRows && equal == examined
            && whole.Values.All(value => value) && raw.Values.All(value => value));
    }
}
