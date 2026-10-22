using System.Text.Json;
using Erlang;
namespace Erlang.Tests;

internal static class EtsTraversalOriginal416Checks
{
    internal static async Task Run()
    {
        string output = Path.GetFullPath(Path.Combine(EtsTraversalOriginal416Names.Output,
            System.Guid.NewGuid().ToString(EtsTraversalOriginal416Names.Guid)));
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
                RuntimeNativePrimitiveProviders.Register(owner, registry, RuntimeNativePrimitives.EtsStorageCore);
                PreparedBeamImage facet = EtsTraversalOriginal416Preparation.Prepare(owner, registry);
                var declared = new BeamDependencySelection([EtsTraversalOriginal416Names.Ets,
                    EtsTraversalOriginal416Names.Runtime, EtsTraversalOriginal416Names.Lists, EtsTraversalOriginal416Names.File]);
                PreparedBeamImage lists = registry.PrepareBeam(
                    EtsTraversalOriginal416Access.Resource(EtsTraversalOriginal416Names.ListsBeam), declared);
                PreparedBeamImage worker = registry.PrepareBeam(
                    EtsTraversalOriginal416Access.Resource(EtsTraversalOriginal416Names.WorkerBeam), declared);
                EtsTraversalOriginal416Access.Require(registry.PublishPreparedBeam([facet, lists, worker]).Status == BeamPublicationStatus.Published);
                EtsTraversalOriginal416Preparation.AssertPublished(registry, facet);
                foreach (var image in new[] { lists, worker })
                    foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                        EtsTraversalOriginal416Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
            });
            actor = new RuntimeNodeFoundationHostActor(owner);
            RuntimeNodeFoundationHostActor caller = actor;

            async Task Capture(string function, string file)
            {
                try
                {
                    Term value = await caller.InvokeAsync(async context => await owner.Modules.Call(context,
                        EtsTraversalOriginal416Names.Module, function));
                    BitString wire = (BitString)await caller.InvokeAsync(async context => await owner.Modules.Call(
                        context,
                        EtsTraversalOriginal416Names.Runtime,
                        EtsTraversalOriginal416Names.Encode,
                        value
                    ));
                    captured.Add(file, (value, wire.ToArray()));
                    File.WriteAllBytes(Path.Combine(output, EtsTraversalOriginal416Names.Actual + file), wire.ToArray());
                    File.WriteAllText(
                        Path.Combine(output, EtsTraversalOriginal416Names.Actual + file + EtsTraversalOriginal416Names.Json),
                        JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(value))
                    );
                }
                catch (Exception error)
                {
                    errors.Add(new { Route = file, Failure = error.ToString() });
                }
            }
            await Capture(EtsTraversalOriginal416Names.Run, EtsTraversalOriginal416Names.Values);
            await Capture(EtsTraversalOriginal416Names.Stacks, EtsTraversalOriginal416Names.StackValues);
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
        foreach (string file in new[] { EtsTraversalOriginal416Names.Values, EtsTraversalOriginal416Names.StackValues })
        {
            byte[] bytes = EtsTraversalOriginal416Access.Resource(file);
            Term expected = OwnedFunctionOriginal407Terms.Decode(bytes);
            File.WriteAllBytes(Path.Combine(output, EtsTraversalOriginal416Names.Expected + file), bytes);
            File.WriteAllText(
                Path.Combine(output, EtsTraversalOriginal416Names.Expected + file + EtsTraversalOriginal416Names.Json),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expected))
            );
            bool present = captured.TryGetValue(file, out var actual);
            whole[file] = present && expected.Equals(actual.Term);
            raw[file] = present && bytes.AsSpan().SequenceEqual(actual.Bytes);
            if (!present) continue;
            equal += file == EtsTraversalOriginal416Names.Values
                ? IoPrintableOriginal404Comparison.Rows(
                    expected,
                    actual.Term,
                    file,
                    EtsTraversalOriginal416Names.Rows,
                    differences,
                    ref examined
                )
                : IoPrintableOriginal404Comparison.Profiles(
                    expected,
                    actual.Term,
                    EtsTraversalOriginal416Names.Rows,
                    differences,
                    ref examined
                );
        }
        File.WriteAllText(
            Path.Combine(output, EtsTraversalOriginal416Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = EtsTraversalOriginal416Names.WholeRows,
                Examined = examined,
                Equal = equal,
                WholeTerms = whole,
                WholeRawEtf = raw,
                Errors = errors,
                Differences = differences,
                SourceDerivedPartialEts = true,
                WholeOriginalEts51Claim = false,
                ProfileArity = 3,
                Projection = false,
                ApiOrFullContractPromotion = false
            })
        );
        EtsTraversalOriginal416Access.Require(errors.Count == 0 && differences.Count == 0
            && examined == EtsTraversalOriginal416Names.WholeRows && equal == examined
            && whole.Values.All(value => value) && raw.Values.All(value => value));
    }
}
