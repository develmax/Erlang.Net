using System.Text.Json;
using Erlang;
namespace Erlang.Tests;

internal static class EtsQueriesOriginal414Checks
{
    internal static async Task Run()
    {
        string output = Path.GetFullPath(Path.Combine(EtsQueriesOriginal414Names.Output,
            System.Guid.NewGuid().ToString(EtsQueriesOriginal414Names.Guid)));
        Directory.CreateDirectory(output);
        var captured = new Dictionary<string, (Term Term, byte[] Bytes)>();
        var errors = new List<object>();
        ProcessRuntime? runtime = null;
        RuntimeNodeFoundationHostActor? actor = null;
        try
        {
            runtime = new ProcessRuntime(2);
            ProcessRuntime owner = runtime;
            await owner.InitializeModulesAsync(registry =>
            {
                RuntimeNativePrimitiveProviders.Register(owner, registry, RuntimeNativePrimitives.EtsStorageCore);
                PreparedBeamImage facet = EtsQueriesOriginal414Preparation.Prepare(owner, registry);
                var declared = new BeamDependencySelection([EtsQueriesOriginal414Names.Ets,
                    EtsQueriesOriginal414Names.Runtime, EtsQueriesOriginal414Names.Lists, EtsQueriesOriginal414Names.File]);
                PreparedBeamImage lists = registry.PrepareBeam(
                    EtsQueriesOriginal414Access.Resource(EtsQueriesOriginal414Names.ListsBeam), declared);
                PreparedBeamImage worker = registry.PrepareBeam(
                    EtsQueriesOriginal414Access.Resource(EtsQueriesOriginal414Names.WorkerBeam), declared);
                EtsQueriesOriginal414Access.Require(registry.PublishPreparedBeam([facet, lists, worker]).Status == BeamPublicationStatus.Published);
                EtsQueriesOriginal414Preparation.AssertPublished(registry, facet);
                foreach (var image in new[] { lists, worker })
                    foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                        EtsQueriesOriginal414Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
            });
            actor = new RuntimeNodeFoundationHostActor(owner);
            RuntimeNodeFoundationHostActor caller = actor;

            async Task Capture(string function, string file)
            {
                try
                {
                    Term value = await caller.InvokeAsync(async context => await owner.Modules.Call(context,
                        EtsQueriesOriginal414Names.Module, function));
                    BitString wire = (BitString)await caller.InvokeAsync(async context => await owner.Modules.Call(
                        context,
                        EtsQueriesOriginal414Names.Runtime,
                        EtsQueriesOriginal414Names.Encode,
                        value
                    ));
                    captured.Add(file, (value, wire.ToArray()));
                    File.WriteAllBytes(Path.Combine(output, EtsQueriesOriginal414Names.Actual + file), wire.ToArray());
                    File.WriteAllText(
                        Path.Combine(output, EtsQueriesOriginal414Names.Actual + file + EtsQueriesOriginal414Names.Json),
                        JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(value))
                    );
                }
                catch (Exception error)
                {
                    errors.Add(new { Route = file, Failure = error.ToString() });
                }
            }
            await Capture(EtsQueriesOriginal414Names.Run, EtsQueriesOriginal414Names.Values);
            await Capture(EtsQueriesOriginal414Names.Stacks, EtsQueriesOriginal414Names.StackValues);
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
        foreach (string file in new[] { EtsQueriesOriginal414Names.Values, EtsQueriesOriginal414Names.StackValues })
        {
            byte[] bytes = EtsQueriesOriginal414Access.Resource(file);
            Term expected = OwnedFunctionOriginal407Terms.Decode(bytes);
            File.WriteAllBytes(Path.Combine(output, EtsQueriesOriginal414Names.Expected + file), bytes);
            File.WriteAllText(
                Path.Combine(output, EtsQueriesOriginal414Names.Expected + file + EtsQueriesOriginal414Names.Json),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expected))
            );
            bool present = captured.TryGetValue(file, out var actual);
            whole[file] = present && expected.Equals(actual.Term);
            raw[file] = present && bytes.AsSpan().SequenceEqual(actual.Bytes);
            if (!present) continue;
            equal += file == EtsQueriesOriginal414Names.Values
                ? IoPrintableOriginal404Comparison.Rows(
                    expected,
                    actual.Term,
                    file,
                    EtsQueriesOriginal414Names.Rows,
                    differences,
                    ref examined
                )
                : IoPrintableOriginal404Comparison.Profiles(
                    expected,
                    actual.Term,
                    EtsQueriesOriginal414Names.Rows,
                    differences,
                    ref examined
                );
        }
        File.WriteAllText(
            Path.Combine(output, EtsQueriesOriginal414Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = EtsQueriesOriginal414Names.WholeRows,
                Examined = examined,
                Equal = equal,
                WholeTerms = whole,
                WholeRawEtf = raw,
                Errors = errors,
                Differences = differences,
                SourceDerivedPartialEts = true,
                OriginalReceiptAuthorAssumptionFieldUnchanged = true,
                NativeCaptureResourcesUnchanged = true,
                WholeOriginalEts51Claim = false,
                ProfileArity = 3,
                Projection = false,
                ApiOrFullContractPromotion = false
            })
        );
        EtsQueriesOriginal414Access.Require(errors.Count == 0 && differences.Count == 0
            && examined == EtsQueriesOriginal414Names.WholeRows && equal == examined
            && whole.Values.All(value => value) && raw.Values.All(value => value));
    }
}
