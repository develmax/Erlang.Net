using System.Text.Json;
using Erlang;
namespace Erlang.Tests;

internal static class EtsCompressedMutations442Checks
{
    private static void ValidateOwn(Term portable, Term own, bool stacked)
    {
        using JsonDocument profile = JsonDocument.Parse(EtsCompressedMutations442Access.Resource(EtsCompressedMutations442Names.Profile));
        string[] cases = profile.RootElement.GetProperty(EtsCompressedMutations442Names.Cases).EnumerateArray().Select(item => item.GetString()!).ToArray();
        var ownCases = profile.RootElement.GetProperty(EtsCompressedMutations442Names.OwnCases).EnumerateArray()
            .Select(item => item.GetString()!).ToHashSet(StringComparer.Ordinal);

        void Rows(Term expectedRows, Term observedRows)
        {
            Term[] main = Cons.Items(expectedRows).ToArray(), details = Cons.Items(observedRows).ToArray();
            EtsCompressedMutations442Access.Require(main.Length == cases.Length && details.Length == cases.Length);
            for (int index = 0; index < cases.Length; index++)
            {
                var primary = (TupleTerm)main[index];
                var detail = (TupleTerm)details[index];
                bool owned = ownCases.Contains(cases[index]);
                string scope = owned ? EtsCompressedMutations442Names.OwnScope : EtsCompressedMutations442Names.PortableScope;
                EtsCompressedMutations442Access.Require(detail.Items.Count == 3 && primary.Items[0].Equals(Term.A(cases[index]))
                    && detail.Items[0].Equals(primary.Items[0]) && detail.Items[1].Equals(Term.A(scope)));
                Term[] observations = Cons.Items(detail.Items[2]).ToArray();
                EtsCompressedMutations442Access.Require(!owned || observations.Length != 0);
            }
        }

        if (!stacked)
        {
            Rows(portable, own);

            return;
        }
        Term[] mainProfiles = Cons.Items(portable).ToArray(), ownProfiles = Cons.Items(own).ToArray();
        EtsCompressedMutations442Access.Require(mainProfiles.Length == 5 && ownProfiles.Length == mainProfiles.Length);
        for (int index = 0; index < mainProfiles.Length; index++)
        {
            var main = (TupleTerm)mainProfiles[index];
            var detail = (TupleTerm)ownProfiles[index];
            EtsCompressedMutations442Access.Require(main.Items.Count == 3 && detail.Items.Count == 3
                && main.Items[0].Equals(detail.Items[0]) && main.Items[1].Equals(detail.Items[1]));
            Rows(main.Items[2], detail.Items[2]);
        }
    }

    internal static async Task Run()
    {
        string output = Path.GetFullPath(Path.Combine(EtsCompressedMutations442Names.Output,
            System.Guid.NewGuid().ToString(EtsCompressedMutations442Names.Guid)));
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
                var declared = new BeamDependencySelection([EtsCompressedMutations442Names.Ets,
                    EtsCompressedMutations442Names.Runtime, EtsCompressedMutations442Names.Subject,
                    EtsCompressedMutations442Names.Module, EtsCompressedMutations442Names.File]);
                RuntimeNativePrimitiveProviders.Register(owner, registry, RuntimeNativePrimitives.EtsStorageCore);
                PreparedBeamImage facet = EtsInfoOriginal422Preparation.Prepare(owner, registry);
                PreparedBeamImage worker = registry.PrepareBeam(
                    EtsCompressedMutations442Access.Resource(EtsCompressedMutations442Names.WorkerBeam), declared);
                PreparedBeamImage observer = registry.PrepareBeam(
                    EtsCompressedMutations442Access.Resource(EtsCompressedMutations442Names.ObserverBeam), declared);
                EtsCompressedMutations442Access.Require(registry.PublishPreparedBeam([facet, worker, observer]).Status == BeamPublicationStatus.Published);
                EtsInfoOriginal422Preparation.AssertPublished(registry, facet);
                foreach (var image in new[] { worker, observer })
                    foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                        EtsCompressedMutations442Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
            });
            actor = new RuntimeNodeFoundationHostActor(owner);
            RuntimeNodeFoundationHostActor caller = actor;

            async Task Capture(string function, string file)
            {
                try
                {
                    Term value = await caller.InvokeAsync(async context => await owner.Modules.Call(context,
                        EtsCompressedMutations442Names.Module, function));
                    var pair = (TupleTerm)value;
                    EtsCompressedMutations442Access.Require(pair.Items.Count == 2);
                    value = pair.Items[0];
                    BitString ownWire = (BitString)await caller.InvokeAsync(async context => await owner.Modules.Call(
                        context,
                        EtsCompressedMutations442Names.Runtime,
                        EtsCompressedMutations442Names.Encode,
                        pair.Items[1]
                    ));
                    File.WriteAllBytes(Path.Combine(output, EtsCompressedMutations442Names.Actual + EtsCompressedMutations442Names.Own + file), ownWire.ToArray());
                    File.WriteAllText(
                        Path.Combine(
                            output,
                            EtsCompressedMutations442Names.Actual + EtsCompressedMutations442Names.Own + file + EtsCompressedMutations442Names.Json
                        ),
                        JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(pair.Items[1]))
                    );
                    File.WriteAllBytes(
                        Path.Combine(output, EtsCompressedMutations442Names.Expected + EtsCompressedMutations442Names.Own + file),
                        EtsCompressedMutations442Access.Resource(EtsCompressedMutations442Names.Own + file)
                    );
                    ValidateOwn(value, pair.Items[1], file == EtsCompressedMutations442Names.StackValues);
                    BitString wire = (BitString)await caller.InvokeAsync(async context => await owner.Modules.Call(
                        context,
                        EtsCompressedMutations442Names.Runtime,
                        EtsCompressedMutations442Names.Encode,
                        value
                    ));
                    captured.Add(file, (value, wire.ToArray()));
                    File.WriteAllBytes(Path.Combine(output, EtsCompressedMutations442Names.Actual + file), wire.ToArray());
                    File.WriteAllText(
                        Path.Combine(output, EtsCompressedMutations442Names.Actual + file + EtsCompressedMutations442Names.Json),
                        JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(value))
                    );
                }
                catch (Exception error)
                {
                    errors.Add(new { Route = file, Failure = error.ToString() });
                }
            }
            await Capture(EtsCompressedMutations442Names.Run, EtsCompressedMutations442Names.Values);
            await Capture(EtsCompressedMutations442Names.Stacks, EtsCompressedMutations442Names.StackValues);
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
        foreach (string file in new[] { EtsCompressedMutations442Names.Values, EtsCompressedMutations442Names.StackValues })
        {
            byte[] bytes = EtsCompressedMutations442Access.Resource(file);
            Term expected = OwnedFunctionOriginal407Terms.Decode(bytes);
            File.WriteAllBytes(Path.Combine(output, EtsCompressedMutations442Names.Expected + file), bytes);
            File.WriteAllText(
                Path.Combine(output, EtsCompressedMutations442Names.Expected + file + EtsCompressedMutations442Names.Json),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expected))
            );
            bool present = captured.TryGetValue(file, out var actual);
            whole[file] = present && expected.Equals(actual.Term);
            raw[file] = present && bytes.AsSpan().SequenceEqual(actual.Bytes);
            if (!present) continue;
            equal += file == EtsCompressedMutations442Names.Values
                ? IoPrintableOriginal404Comparison.Rows(
                    expected,
                    actual.Term,
                    file,
                    EtsCompressedMutations442Names.Rows,
                    differences,
                    ref examined
                )
                : IoPrintableOriginal404Comparison.Profiles(
                    expected,
                    actual.Term,
                    EtsCompressedMutations442Names.Rows,
                    differences,
                    ref examined
                );
        }
        File.WriteAllText(
            Path.Combine(output, EtsCompressedMutations442Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = EtsCompressedMutations442Names.WholeRows,
                Examined = examined,
                Equal = equal,
                WholeTerms = whole,
                WholeRawEtf = raw,
                Errors = errors,
                Differences = differences,
                SourceDerivedFiftyOneNativeFacet = true,
                WholeOriginalEts51Claim = false,
                OwnPidArgumentSourcePredicate = false,
                PriorNativePidRawDifferencesRetained = false,
                MatchedWorkerArity = 2,
                PortableFullTermRows = EtsCompressedMutations442Names.FullTermRows,
                SourceOwnedCursorProtocolRows = EtsCompressedMutations442Names.OwnProtocolRows,
                OpaqueCursorRepresentationEquality = false,
                OwnInputsAndOutcomesRetainedWithoutNormalization = true,
                Projection = false,
                ApiOrFullContractPromotion = false
            })
        );
        EtsCompressedMutations442Access.Require(errors.Count == 0 && differences.Count == 0
            && examined == EtsCompressedMutations442Names.WholeRows && equal == examined
            && whole.Values.All(value => value) && raw.Values.All(value => value));
    }
}
