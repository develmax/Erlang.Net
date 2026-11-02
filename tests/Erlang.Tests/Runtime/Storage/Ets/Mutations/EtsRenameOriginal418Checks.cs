using System.Text.Json;
using Erlang;
namespace Erlang.Tests;

internal static class EtsRenameOriginal418Checks
{
    internal static async Task Run()
    {
        string output = Path.GetFullPath(Path.Combine(EtsRenameOriginal418Names.Output,
            System.Guid.NewGuid().ToString(EtsRenameOriginal418Names.Guid)));
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
                        EtsRenameOriginal418Preparation.Register171(owner, registry);
                    }
                });
                PreparedBeamImage facet = EtsRenameOriginal418Preparation.Prepare171(owner, registry, scope);
                var declared = new BeamDependencySelection([EtsRenameOriginal418Names.Ets,
                                EtsRenameOriginal418Names.Runtime, EtsRenameOriginal418Names.Lists, EtsRenameOriginal418Names.File]);
                PreparedBeamImage lists = scope.Prepare(() => registry.PrepareBeam(
                    EtsRenameOriginal418Access.Resource(EtsRenameOriginal418Names.ListsBeam), declared));
                PreparedBeamImage worker = scope.Prepare(() => registry.PrepareBeam(
                    EtsRenameOriginal418Access.Resource(EtsRenameOriginal418Names.WorkerBeam), declared));

                return _ =>
                {
                    EtsRenameOriginal418Access.Require(registry.PublishPreparedBeam([facet, lists, worker]).Status == BeamPublicationStatus.Published);
                    EtsRenameOriginal418Preparation.AssertPublished(registry, facet);
                    foreach (var image in new[] { lists, worker })
                        foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                            EtsRenameOriginal418Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
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
                        EtsRenameOriginal418Names.Module, function));
                    BitString wire = (BitString)await caller.InvokeAsync(async context => await owner.Modules.Call(
                        context,
                        EtsRenameOriginal418Names.Runtime,
                        EtsRenameOriginal418Names.Encode,
                        value
                    ));
                    captured.Add(file, (value, wire.ToArray()));
                    File.WriteAllBytes(Path.Combine(output, EtsRenameOriginal418Names.Actual + file), wire.ToArray());
                    File.WriteAllText(
                        Path.Combine(output, EtsRenameOriginal418Names.Actual + file + EtsRenameOriginal418Names.Json),
                        JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(value))
                    );
                }
                catch (Exception error)
                {
                    errors.Add(new { Route = file, Failure = error.ToString() });
                }
            }
            await Capture(EtsRenameOriginal418Names.Run, EtsRenameOriginal418Names.Values);
            await Capture(EtsRenameOriginal418Names.Stacks, EtsRenameOriginal418Names.StackValues);
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
        foreach (string file in new[] { EtsRenameOriginal418Names.Values, EtsRenameOriginal418Names.StackValues })
        {
            byte[] bytes = EtsRenameOriginal418Access.Resource(file);
            Term expected = OwnedFunctionOriginal407Terms.Decode(bytes);
            File.WriteAllBytes(Path.Combine(output, EtsRenameOriginal418Names.Expected + file), bytes);
            File.WriteAllText(
                Path.Combine(output, EtsRenameOriginal418Names.Expected + file + EtsRenameOriginal418Names.Json),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expected))
            );
            bool present = captured.TryGetValue(file, out var actual);
            whole[file] = present && expected.Equals(actual.Term);
            raw[file] = present && bytes.AsSpan().SequenceEqual(actual.Bytes);
            if (!present) continue;
            equal += file == EtsRenameOriginal418Names.Values
                ? IoPrintableOriginal404Comparison.Rows(
                    expected,
                    actual.Term,
                    file,
                    EtsRenameOriginal418Names.Rows,
                    differences,
                    ref examined
                )
                : IoPrintableOriginal404Comparison.Profiles(
                    expected,
                    actual.Term,
                    EtsRenameOriginal418Names.Rows,
                    differences,
                    ref examined
                );
        }
        File.WriteAllText(
            Path.Combine(output, EtsRenameOriginal418Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = EtsRenameOriginal418Names.WholeRows,
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
        EtsRenameOriginal418Access.Require(errors.Count == 0 && differences.Count == 0
            && examined == EtsRenameOriginal418Names.WholeRows && equal == examined
            && whole.Values.All(value => value) && raw.Values.All(value => value));
    }
}
