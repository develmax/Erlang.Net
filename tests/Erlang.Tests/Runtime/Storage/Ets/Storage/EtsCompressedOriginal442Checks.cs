using System.Text.Json;
using Erlang;
namespace Erlang.Tests;

internal static class EtsCompressedOriginal442Checks
{
    internal static async Task Run()
    {
        string output = Path.GetFullPath(Path.Combine(EtsCompressedOriginal442Names.Output,
            System.Guid.NewGuid().ToString(EtsCompressedOriginal442Names.Guid)));
        Directory.CreateDirectory(output);
        var captured = new Dictionary<string, (Term Term, byte[] Bytes)>();
        var errors = new List<object>();
        var ownRaw = new Dictionary<string, bool>();
        var ownOrdinals = new Dictionary<string, Dictionary<string, int>>();
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
                var declared = new BeamDependencySelection([EtsCompressedOriginal442Names.Ets,
                                EtsCompressedOriginal442Names.Runtime, EtsCompressedOriginal442Names.Subject, EtsCompressedOriginal442Names.Module]);
                await owner.InitializeModulesAsync(_ =>
                {
                    {
                        RuntimeNativePrimitiveProviders.Register(owner, registry, RuntimeNativePrimitives.EtsStorageCore);
                    }
                    {
                        EtsInfoOriginal422Preparation.Register171(owner, registry);
                    }
                });
                PreparedBeamImage facet = EtsInfoOriginal422Preparation.Prepare171(owner, registry, scope);
                PreparedBeamImage worker = scope.Prepare(() => registry.PrepareBeam(
                    EtsCompressedOriginal442Access.Resource(EtsCompressedOriginal442Names.WorkerBeam), declared));
                PreparedBeamImage observer = scope.Prepare(() => registry.PrepareBeam(
                    EtsCompressedOriginal442Access.Resource(EtsCompressedOriginal442Names.ObserverBeam), declared));

                return _ =>
                {
                    EtsCompressedOriginal442Access.Require(registry.PublishPreparedBeam([facet, worker, observer]).Status == BeamPublicationStatus.Published);
                    EtsInfoOriginal422Preparation.AssertPublished(registry, facet);
                    foreach (var image in new[] { worker, observer })
                        foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                            EtsCompressedOriginal442Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
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
                        EtsCompressedOriginal442Names.Module, function));
                    BitString wire = (BitString)await caller.InvokeAsync(async context => await owner.Modules.Call(
                        context,
                        EtsCompressedOriginal442Names.Runtime,
                        EtsCompressedOriginal442Names.Encode,
                        value
                    ));
                    Term originalExpected = OwnedFunctionOriginal407Terms.Decode(EtsCompressedOriginal442Access.Resource(file));
                    var ownExpected = await caller.InvokeAsync(_ => Task.FromResult(AtomMapWire494Expected.Encode(originalExpected)));
                    ownRaw[file] = ownExpected.Bytes.AsSpan().SequenceEqual(wire.ToArray());
                    ownOrdinals[file] = ownExpected.Indices;
                    File.WriteAllBytes(Path.Combine(output, AtomMapWire494Names.OwnPrefix + file), ownExpected.Bytes);
                    captured.Add(file, (value, wire.ToArray()));
                    File.WriteAllBytes(Path.Combine(output, EtsCompressedOriginal442Names.Actual + file), wire.ToArray());
                    File.WriteAllText(
                        Path.Combine(output, EtsCompressedOriginal442Names.Actual + file + EtsCompressedOriginal442Names.Json),
                        JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(value))
                    );
                }
                catch (Exception error)
                {
                    errors.Add(new { Route = file, Failure = error.ToString() });
                }
            }
            await Capture(EtsCompressedOriginal442Names.Run, EtsCompressedOriginal442Names.Values);
            await Capture(EtsCompressedOriginal442Names.Stacks, EtsCompressedOriginal442Names.StackValues);
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
        foreach (string file in new[] { EtsCompressedOriginal442Names.Values, EtsCompressedOriginal442Names.StackValues })
        {
            byte[] bytes = EtsCompressedOriginal442Access.Resource(file);
            Term expected = OwnedFunctionOriginal407Terms.Decode(bytes);
            File.WriteAllBytes(Path.Combine(output, EtsCompressedOriginal442Names.Expected + file), bytes);
            File.WriteAllText(
                Path.Combine(output, EtsCompressedOriginal442Names.Expected + file + EtsCompressedOriginal442Names.Json),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expected))
            );
            bool present = captured.TryGetValue(file, out var actual);
            whole[file] = present && expected.Equals(actual.Term);
            raw[file] = present && bytes.AsSpan().SequenceEqual(actual.Bytes);
            if (!present) continue;
            equal += file == EtsCompressedOriginal442Names.Values
                ? IoPrintableOriginal404Comparison.Rows(
                    expected,
                    actual.Term,
                    file,
                    EtsCompressedOriginal442Names.Rows,
                    differences,
                    ref examined
                )
                : IoPrintableOriginal404Comparison.Profiles(
                    expected,
                    actual.Term,
                    EtsCompressedOriginal442Names.Rows,
                    differences,
                    ref examined
                );
        }
        File.WriteAllText(
            Path.Combine(output, EtsCompressedOriginal442Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = EtsCompressedOriginal442Names.WholeRows,
                Examined = examined,
                Equal = equal,
                WholeTerms = whole,
                WholeRawEtf = raw,
                OwnAtomOrdinalWholeRawEtf = ownRaw,
                OwnAtomOrdinals = ownOrdinals,
                OriginalRawMapOrderDifferencesRetained = true,
                Errors = errors,
                Differences = differences,
                SourceDerivedFiftyOneNativeFacet = true,
                WholeOriginalEts51Claim = false,
                OwnPidArgumentSourcePredicate = false,
                PriorNativePidRawDifferencesRetained = false,
                MatchedWorkerArity = 2,
                Projection = false,
                ApiOrFullContractPromotion = false
            })
        );
        EtsCompressedOriginal442Access.Require(errors.Count == 0 && differences.Count == 0
            && examined == EtsCompressedOriginal442Names.WholeRows && equal == examined
            && whole.Values.All(value => value) && ownRaw.Count >= raw.Count && ownRaw.Values.All(value => value));
    }
}
