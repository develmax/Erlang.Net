using System.Text.Json;
using Erlang;
namespace Erlang.Tests;

internal static class EtsCompiledOriginal419Checks
{
    internal static Task Strict() => Run(false);

    internal static Task Own() => Run(true);

    private static async Task Run(bool own)
    {
        string output = Path.GetFullPath(Path.Combine(EtsCompiledOriginal419Names.Output,
            System.Guid.NewGuid().ToString(EtsCompiledOriginal419Names.Guid)));
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
                var declared = new BeamDependencySelection([EtsCompiledOriginal419Names.Ets,
                    EtsCompiledOriginal419Names.Runtime, EtsCompiledOriginal419Names.Subject, EtsCompiledOriginal419Names.Module]);
                PreparedBeamImage facet = EtsCompiledOriginal419Preparation.Prepare(owner, registry, declared);
                PreparedBeamImage worker = registry.PrepareBeam(
                    EtsCompiledOriginal419Access.Resource(EtsCompiledOriginal419Names.WorkerBeam), declared);
                PreparedBeamImage observer = registry.PrepareBeam(
                    EtsCompiledOriginal419Access.Resource(EtsCompiledOriginal419Names.ObserverBeam), declared);
                EtsCompiledOriginal419Access.Require(registry.PublishPreparedBeam([facet, worker, observer]).Status == BeamPublicationStatus.Published);
                EtsCompiledOriginal419Preparation.AssertPublished(registry, facet);
                foreach (var image in new[] { worker, observer })
                    foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                        EtsCompiledOriginal419Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
            });
            actor = new RuntimeNodeFoundationHostActor(owner);
            RuntimeNodeFoundationHostActor caller = actor;

            async Task Capture(string function, string file)
            {
                try
                {
                    Term value = await caller.InvokeAsync(async context => await owner.Modules.Call(context,
                        EtsCompiledOriginal419Names.Module, function));
                    BitString wire = (BitString)await caller.InvokeAsync(async context => await owner.Modules.Call(
                        context,
                        EtsCompiledOriginal419Names.Runtime,
                        EtsCompiledOriginal419Names.Encode,
                        value
                    ));
                    captured.Add(file, (value, wire.ToArray()));
                    File.WriteAllBytes(Path.Combine(output, EtsCompiledOriginal419Names.Actual + file), wire.ToArray());
                    File.WriteAllText(
                        Path.Combine(output, EtsCompiledOriginal419Names.Actual + file + EtsCompiledOriginal419Names.Json),
                        JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(value))
                    );
                    if (own)
                    {
                        var rows = file == EtsCompiledOriginal419Names.OwnValues ? new[] { value }
                            : Cons.Items(value).Select(profile => ((TupleTerm)profile).Items[2]).ToArray();
                        foreach (Term rowsTerm in rows)
                            _ = await caller.InvokeAsync(async context => await owner.Modules.Call(
                                context,
                                EtsCompiledOriginal419Names.Module,
                                EtsCompiledOriginal419Names.Verify,
                                rowsTerm
                            ));
                    }

                }
                catch (Exception error)
                {
                    errors.Add(new { Route = file, Failure = error.ToString() });
                }
            }
            await Capture(
                own ? EtsCompiledOriginal419Names.OwnRun : EtsCompiledOriginal419Names.Run,
                own ? EtsCompiledOriginal419Names.OwnValues : EtsCompiledOriginal419Names.Values
            );
            await Capture(
                own ? EtsCompiledOriginal419Names.OwnStacks : EtsCompiledOriginal419Names.Stacks,
                own ? EtsCompiledOriginal419Names.OwnStackValues : EtsCompiledOriginal419Names.StackValues
            );
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
        foreach (string file in own ? new[] { EtsCompiledOriginal419Names.OwnValues, EtsCompiledOriginal419Names.OwnStackValues } : new[] { EtsCompiledOriginal419Names.Values, EtsCompiledOriginal419Names.StackValues })
        {
            byte[] bytes = EtsCompiledOriginal419Access.Resource(file);
            Term expected = OwnedFunctionOriginal407Terms.Decode(bytes);
            File.WriteAllBytes(Path.Combine(output, EtsCompiledOriginal419Names.Expected + file), bytes);
            File.WriteAllText(
                Path.Combine(output, EtsCompiledOriginal419Names.Expected + file + EtsCompiledOriginal419Names.Json),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expected))
            );
            bool present = captured.TryGetValue(file, out var actual);
            whole[file] = present && expected.Equals(actual.Term);
            raw[file] = present && bytes.AsSpan().SequenceEqual(actual.Bytes);
            if (!present) continue;
            if (own)
                equal += EtsCompiledOriginal419Protocol.Compare(
                    expected,
                    actual.Term,
                    file == EtsCompiledOriginal419Names.OwnStackValues,
                    differences,
                    ref examined
                );
            else equal += file == EtsCompiledOriginal419Names.Values
                ? IoPrintableOriginal404Comparison.Rows(
                    expected,
                    actual.Term,
                    file,
                    EtsCompiledOriginal419Names.Rows,
                    differences,
                    ref examined
                )
                : IoPrintableOriginal404Comparison.Profiles(
                    expected,
                    actual.Term,
                    EtsCompiledOriginal419Names.Rows,
                    differences,
                    ref examined
                );
        }
        File.WriteAllText(
            Path.Combine(output, EtsCompiledOriginal419Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = own ? EtsCompiledOriginal419Names.OwnWholeRows : EtsCompiledOriginal419Names.WholeRows,
                Examined = examined,
                Equal = equal,
                WholeTerms = whole,
                WholeRawEtf = raw,
                Errors = errors,
                Differences = differences,
                SourceDerivedThreeNativeFacet = true,
                OwnStateProtocol = own,
                RawCrossVmEqualityRequired = !own,
                WholeOriginalEts51Claim = false,
                ProfileArity = 3,
                Projection = false,
                ApiOrFullContractPromotion = false
            })
        );
        EtsCompiledOriginal419Access.Require(errors.Count == 0 && differences.Count == 0
            && examined == (own ? EtsCompiledOriginal419Names.OwnWholeRows : EtsCompiledOriginal419Names.WholeRows) && equal == examined
            && (own || whole.Values.All(value => value) && raw.Values.All(value => value)));
    }
}
