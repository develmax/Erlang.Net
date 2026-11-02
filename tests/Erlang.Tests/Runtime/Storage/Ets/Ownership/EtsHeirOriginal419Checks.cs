using System.Text.Json;
using Erlang;
namespace Erlang.Tests;

internal static class EtsHeirOriginal419Checks
{
    internal static async Task Run()
    {
        string output = Path.GetFullPath(Path.Combine(EtsHeirOriginal419Names.Output,
            System.Guid.NewGuid().ToString(EtsHeirOriginal419Names.Guid)));
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
                async (registry, scope) =>
            {
                await owner.InitializeModulesAsync(_ =>
                {
                    {
                        RuntimeNativePrimitiveProviders.Register(owner, registry, RuntimeNativePrimitives.EtsStorageCore);
                    }
                    {
                        EtsHeirOriginal419Preparation.Register171(owner, registry);
                    }
                });
                PreparedBeamImage facet = EtsHeirOriginal419Preparation.Prepare171(owner, registry, scope);
                var declared = new BeamDependencySelection([EtsHeirOriginal419Names.Ets,
                                EtsHeirOriginal419Names.Runtime, EtsHeirOriginal419Names.Lists, EtsHeirOriginal419Names.File]);
                PreparedBeamImage lists = scope.Prepare(() => registry.PrepareBeam(
                    EtsHeirOriginal419Access.Resource(EtsHeirOriginal419Names.ListsBeam), declared));
                PreparedBeamImage worker = scope.Prepare(() => registry.PrepareBeam(
                    EtsHeirOriginal419Access.Resource(EtsHeirOriginal419Names.WorkerBeam), declared));

                return _ =>
                {
                    EtsHeirOriginal419Access.Require(registry.PublishPreparedBeam([facet, lists, worker]).Status == BeamPublicationStatus.Published);
                    EtsHeirOriginal419Preparation.AssertPublished(registry, facet);
                    foreach (var image in new[] { lists, worker })
                        foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                            EtsHeirOriginal419Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
                };
            }
            );
            actor = new RuntimeNodeFoundationHostActor(owner);
            RuntimeNodeFoundationHostActor caller = actor;

            async Task Capture(string function, string file)
            {
                try
                {
                    Term value = await caller.InvokeAsync(async context => await owner.Modules.Call(context,
                        EtsHeirOriginal419Names.Module, function));
                    BitString wire = (BitString)await caller.InvokeAsync(async context => await owner.Modules.Call(
                        context,
                        EtsHeirOriginal419Names.Runtime,
                        EtsHeirOriginal419Names.Encode,
                        value
                    ));
                    captured.Add(file, (value, wire.ToArray()));
                    File.WriteAllBytes(Path.Combine(output, EtsHeirOriginal419Names.Actual + file), wire.ToArray());
                    File.WriteAllText(
                        Path.Combine(output, EtsHeirOriginal419Names.Actual + file + EtsHeirOriginal419Names.Json),
                        JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(value))
                    );
                }
                catch (Exception error)
                {
                    errors.Add(new { Route = file, Failure = error.ToString() });
                }
            }
            await Capture(EtsHeirOriginal419Names.Run, EtsHeirOriginal419Names.Values);
            await Capture(EtsHeirOriginal419Names.Stacks, EtsHeirOriginal419Names.StackValues);
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
        foreach (string file in new[] { EtsHeirOriginal419Names.Values, EtsHeirOriginal419Names.StackValues })
        {
            byte[] bytes = EtsHeirOriginal419Access.Resource(file);
            Term expected = OwnedFunctionOriginal407Terms.Decode(bytes);
            File.WriteAllBytes(Path.Combine(output, EtsHeirOriginal419Names.Expected + file), bytes);
            File.WriteAllText(
                Path.Combine(output, EtsHeirOriginal419Names.Expected + file + EtsHeirOriginal419Names.Json),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expected))
            );
            bool present = captured.TryGetValue(file, out var actual);
            whole[file] = present && expected.Equals(actual.Term);
            raw[file] = present && bytes.AsSpan().SequenceEqual(actual.Bytes);
            if (!present) continue;
            equal += file == EtsHeirOriginal419Names.Values
                ? IoPrintableOriginal404Comparison.Rows(
                    expected,
                    actual.Term,
                    file,
                    EtsHeirOriginal419Names.Rows,
                    differences,
                    ref examined
                )
                : IoPrintableOriginal404Comparison.Profiles(
                    expected,
                    actual.Term,
                    EtsHeirOriginal419Names.Rows,
                    differences,
                    ref examined
                );
        }
        File.WriteAllText(
            Path.Combine(output, EtsHeirOriginal419Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = EtsHeirOriginal419Names.WholeRows,
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
        EtsHeirOriginal419Access.Require(errors.Count == 0 && differences.Count == 0
            && examined == EtsHeirOriginal419Names.WholeRows && equal == examined
            && whole.Values.All(value => value) && raw.Values.All(value => value));
    }
}
