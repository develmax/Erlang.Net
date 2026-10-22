using System.Text.Json;
using Erlang;
namespace Erlang.Tests;

internal static class EtsMutationsOriginal414Checks
{
    internal static async Task Run()
    {
        string output = Path.GetFullPath(Path.Combine(EtsMutationsOriginal414Names.Output,
            System.Guid.NewGuid().ToString(EtsMutationsOriginal414Names.Guid)));
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
                PreparedBeamImage facet = EtsMutationsOriginal414Preparation.Prepare(owner, registry);
                var declared = new BeamDependencySelection([EtsMutationsOriginal414Names.Ets,
                    EtsMutationsOriginal414Names.Runtime, EtsMutationsOriginal414Names.Lists, EtsMutationsOriginal414Names.File]);
                PreparedBeamImage lists = registry.PrepareBeam(
                    EtsMutationsOriginal414Access.Resource(EtsMutationsOriginal414Names.ListsBeam), declared);
                PreparedBeamImage worker = registry.PrepareBeam(
                    EtsMutationsOriginal414Access.Resource(EtsMutationsOriginal414Names.WorkerBeam), declared);
                EtsMutationsOriginal414Access.Require(registry.PublishPreparedBeam([facet, lists, worker]).Status == BeamPublicationStatus.Published);
                EtsMutationsOriginal414Preparation.AssertPublished(registry, facet);
                foreach (var image in new[] { lists, worker })
                    foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                        EtsMutationsOriginal414Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
            });
            actor = new RuntimeNodeFoundationHostActor(owner);
            RuntimeNodeFoundationHostActor caller = actor;

            async Task Capture(string function, string file)
            {
                try
                {
                    Term value = await caller.InvokeAsync(async context => await owner.Modules.Call(context,
                        EtsMutationsOriginal414Names.Module, function));
                    BitString wire = (BitString)await caller.InvokeAsync(async context => await owner.Modules.Call(
                        context,
                        EtsMutationsOriginal414Names.Runtime,
                        EtsMutationsOriginal414Names.Encode,
                        value
                    ));
                    captured.Add(file, (value, wire.ToArray()));
                    File.WriteAllBytes(Path.Combine(output, EtsMutationsOriginal414Names.Actual + file), wire.ToArray());
                    File.WriteAllText(
                        Path.Combine(output, EtsMutationsOriginal414Names.Actual + file + EtsMutationsOriginal414Names.Json),
                        JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(value))
                    );
                }
                catch (Exception error)
                {
                    errors.Add(new { Route = file, Failure = error.ToString() });
                }
            }
            await Capture(EtsMutationsOriginal414Names.Run, EtsMutationsOriginal414Names.Values);
            await Capture(EtsMutationsOriginal414Names.Stacks, EtsMutationsOriginal414Names.StackValues);
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
        foreach (string file in new[] { EtsMutationsOriginal414Names.Values, EtsMutationsOriginal414Names.StackValues })
        {
            byte[] bytes = EtsMutationsOriginal414Access.Resource(file);
            Term expected = OwnedFunctionOriginal407Terms.Decode(bytes);
            File.WriteAllBytes(Path.Combine(output, EtsMutationsOriginal414Names.Expected + file), bytes);
            File.WriteAllText(
                Path.Combine(output, EtsMutationsOriginal414Names.Expected + file + EtsMutationsOriginal414Names.Json),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expected))
            );
            bool present = captured.TryGetValue(file, out var actual);
            whole[file] = present && expected.Equals(actual.Term);
            raw[file] = present && bytes.AsSpan().SequenceEqual(actual.Bytes);
            if (!present) continue;
            equal += file == EtsMutationsOriginal414Names.Values
                ? IoPrintableOriginal404Comparison.Rows(
                    expected,
                    actual.Term,
                    file,
                    EtsMutationsOriginal414Names.Rows,
                    differences,
                    ref examined
                )
                : IoPrintableOriginal404Comparison.Profiles(
                    expected,
                    actual.Term,
                    EtsMutationsOriginal414Names.Rows,
                    differences,
                    ref examined
                );
        }
        File.WriteAllText(
            Path.Combine(output, EtsMutationsOriginal414Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = EtsMutationsOriginal414Names.WholeRows,
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
        EtsMutationsOriginal414Access.Require(errors.Count == 0 && differences.Count == 0
            && examined == EtsMutationsOriginal414Names.WholeRows && equal == examined
            && whole.Values.All(value => value) && raw.Values.All(value => value));
    }
}
