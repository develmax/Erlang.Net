using System.Text.Json;
using Erlang;
namespace Erlang.Tests;

internal static class EtsReplaceOriginal420Checks
{
    internal static async Task Run()
    {
        string output = Path.GetFullPath(Path.Combine(EtsReplaceOriginal420Names.Output,
            System.Guid.NewGuid().ToString(EtsReplaceOriginal420Names.Guid)));
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
                RuntimeNativePrimitiveProviders.Register(owner, registry, RuntimeNativePrimitives.EtsStorageCore);
                PreparedBeamImage facet = EtsReplaceOriginal420Preparation.Prepare(owner, registry);
                var declared = new BeamDependencySelection([EtsReplaceOriginal420Names.Ets,
                    EtsReplaceOriginal420Names.Runtime, EtsReplaceOriginal420Names.Lists, EtsReplaceOriginal420Names.File]);
                PreparedBeamImage lists = registry.PrepareBeam(
                    EtsReplaceOriginal420Access.Resource(EtsReplaceOriginal420Names.ListsBeam), declared);
                PreparedBeamImage worker = registry.PrepareBeam(
                    EtsReplaceOriginal420Access.Resource(EtsReplaceOriginal420Names.WorkerBeam), declared);
                EtsReplaceOriginal420Access.Require(registry.PublishPreparedBeam([facet, lists, worker]).Status == BeamPublicationStatus.Published);
                EtsReplaceOriginal420Preparation.AssertPublished(registry, facet);
                foreach (var image in new[] { lists, worker })
                    foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                        EtsReplaceOriginal420Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
            });
            actor = new RuntimeNodeFoundationHostActor(owner);
            RuntimeNodeFoundationHostActor caller = actor;

            async Task Capture(string function, string file)
            {
                try
                {
                    Term value = await caller.InvokeAsync(async context => await owner.Modules.Call(context,
                        EtsReplaceOriginal420Names.Module, function));
                    BitString wire = (BitString)await caller.InvokeAsync(async context => await owner.Modules.Call(
                        context,
                        EtsReplaceOriginal420Names.Runtime,
                        EtsReplaceOriginal420Names.Encode,
                        value
                    ));
                    captured.Add(file, (value, wire.ToArray()));
                    File.WriteAllBytes(Path.Combine(output, EtsReplaceOriginal420Names.Actual + file), wire.ToArray());
                    File.WriteAllText(
                        Path.Combine(output, EtsReplaceOriginal420Names.Actual + file + EtsReplaceOriginal420Names.Json),
                        JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(value))
                    );
                }
                catch (Exception error)
                {
                    errors.Add(new { Route = file, Failure = error.ToString() });
                }
            }
            await Capture(EtsReplaceOriginal420Names.Run, EtsReplaceOriginal420Names.Values);
            await Capture(EtsReplaceOriginal420Names.Stacks, EtsReplaceOriginal420Names.StackValues);
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
        foreach (string file in new[] { EtsReplaceOriginal420Names.Values, EtsReplaceOriginal420Names.StackValues })
        {
            byte[] bytes = EtsReplaceOriginal420Access.Resource(file);
            Term expected = OwnedFunctionOriginal407Terms.Decode(bytes);
            File.WriteAllBytes(Path.Combine(output, EtsReplaceOriginal420Names.Expected + file), bytes);
            File.WriteAllText(
                Path.Combine(output, EtsReplaceOriginal420Names.Expected + file + EtsReplaceOriginal420Names.Json),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expected))
            );
            bool present = captured.TryGetValue(file, out var actual);
            whole[file] = present && expected.Equals(actual.Term);
            raw[file] = present && bytes.AsSpan().SequenceEqual(actual.Bytes);
            if (!present) continue;
            equal += file == EtsReplaceOriginal420Names.Values
                ? IoPrintableOriginal404Comparison.Rows(
                    expected,
                    actual.Term,
                    file,
                    EtsReplaceOriginal420Names.Rows,
                    differences,
                    ref examined
                )
                : IoPrintableOriginal404Comparison.Profiles(
                    expected,
                    actual.Term,
                    EtsReplaceOriginal420Names.Rows,
                    differences,
                    ref examined
                );
        }
        File.WriteAllText(
            Path.Combine(output, EtsReplaceOriginal420Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = EtsReplaceOriginal420Names.WholeRows,
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
        EtsReplaceOriginal420Access.Require(errors.Count == 0 && differences.Count == 0
            && examined == EtsReplaceOriginal420Names.WholeRows && equal == examined
            && whole.Values.All(value => value) && raw.Values.All(value => value));
    }
}
