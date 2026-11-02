using System.Text.Json;
using Erlang;
namespace Erlang.Tests;

internal static class DebugDirtyOriginal424Checks
{
    internal static async Task Run()
    {
        string output = Path.GetFullPath(Path.Combine(DebugDirtyOriginal424Names.Output,
            System.Guid.NewGuid().ToString(DebugDirtyOriginal424Names.Guid)));
        Directory.CreateDirectory(output);
        var captured = new Dictionary<string, (Term Term, byte[] Bytes)>();
        var errors = new List<object>();
        ProcessRuntime? runtime = null;
        RuntimeNodeFoundationHostActor? actor = null;
        try
        {
            runtime = new ProcessRuntime(1);
            ProcessRuntime owner = runtime;
            await global::Erlang.Tests.PreparedBeamFixtureScope171.RunAsync(
                owner,
                async (registry, scope) =>
            {
                await owner.InitializeModulesAsync(_ =>
                {
                    {
                        RuntimeBootstrapDebugDirty424.Register(owner, registry);
                    }
                    {
                        RuntimeBootstrapAtomics.Register(owner, registry);
                    }
                });
                var declared = new BeamDependencySelection(["erlang", "prim_file", "erts_internal", "erts_debug", "atomics", "lists", "maps", "binary"]);
                var helpers = new[] { "atomics", "lists", "maps" }.Select(module => scope.Prepare(() => registry.PrepareBeam(AtomicsHost413Access.Resource(module + ".beam"), declared))).ToArray();
                PreparedBeamImage facet = scope.Prepare(() => registry.PrepareBeam(DebugDirtyOriginal424Access.Resource(DebugDirtyOriginal424Names.FacetBeam), declared));
                DebugDirtyOriginal424Access.Require(facet.Name == "erts_debug" && facet.NativePrimitiveExports.Count == 4);
                foreach (var signature in new[] { new BeamFunctionSignature("dirty_cpu", 2), new BeamFunctionSignature("dirty_io", 2), new BeamFunctionSignature("dirty", 3), new BeamFunctionSignature("same", 2) })
                    DebugDirtyOriginal424Access.Require(facet.NativePrimitiveExports.Contains(signature));
                PreparedBeamImage worker = scope.Prepare(() => registry.PrepareBeam(DebugDirtyOriginal424Access.Resource(DebugDirtyOriginal424Names.WorkerBeam), declared));

                return _ =>
                {
                    DebugDirtyOriginal424Access.Require(registry.PublishPreparedBeam(helpers.Concat(new[] { facet, worker }).ToArray()).Status == BeamPublicationStatus.Published);
                    DebugDirtyOriginal424Access.Require(registry.Exports.Count(export => export.Module == DebugDirtyOriginal424Names.Internal) == 6);
                    foreach (var image in new[] { facet, worker })
                        foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                            DebugDirtyOriginal424Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
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
                        DebugDirtyOriginal424Names.Module, function));
                    BitString wire = (BitString)await caller.InvokeAsync(async context => await owner.Modules.Call(
                        context,
                        DebugDirtyOriginal424Names.Runtime,
                        DebugDirtyOriginal424Names.Encode,
                        value
                    ));
                    captured.Add(file, (value, wire.ToArray()));
                    File.WriteAllBytes(Path.Combine(output, DebugDirtyOriginal424Names.Actual + file), wire.ToArray());
                    File.WriteAllText(
                        Path.Combine(output, DebugDirtyOriginal424Names.Actual + file + DebugDirtyOriginal424Names.Json),
                        JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(value))
                    );
                }
                catch (Exception error)
                {
                    errors.Add(new { Route = file, Failure = error.ToString() });
                }
            }
            await Capture(DebugDirtyOriginal424Names.Run, DebugDirtyOriginal424Names.Values);
            await Capture(DebugDirtyOriginal424Names.Stacks, DebugDirtyOriginal424Names.StackValues);
            var own = new List<Term>();

            async Task Own(string kind, string function, params Term[] arguments)
            {
                try
                {
                    Term value = await caller.InvokeAsync(async context => await owner.Modules.Call(
                        context,
                        DebugDirtyOriginal424Names.Module,
                        function,
                        arguments
                    ));
                    own.Add(new TupleTerm(new Term[] { Term.A(kind) }.Concat(arguments).Append(value).ToArray()));
                }
                catch (Exception error)
                {
                    errors.Add(new { OwnState = function, Failure = error.ToString() });
                }
            }
            foreach (string route in new[] { "cpu", "io", "normal" }) await Own("send", "send_self", Term.A(route));
            foreach (string route in new[] { "cpu", "io", "normal" }) await Own("ready", "ready", Term.A(route));
            foreach (string route in new[] { "cpu", "io" })
                foreach (string operation in new[] { "alive_waitexiting", "alive_waitexitingonly" }) await Own(
                    "alive",
                    "alive",
                    Term.A(route),
                    Term.A(operation)
                );
            await Own("magic_copy", "magic_copy");
            await Own("heap_copy_sharing", "heap_copy_sharing");
            Term ownTerm = Cons.From(own);
            BitString ownWire = (BitString)await caller.InvokeAsync(async context => await owner.Modules.Call(
                context,
                "erlang",
                "term_to_binary",
                ownTerm
            ));
            captured.Add("own-state.etf", (ownTerm, ownWire.ToArray()));
            File.WriteAllBytes(Path.Combine(output, "actual-own-state.etf"), ownWire.ToArray());
            File.WriteAllText(
                Path.Combine(output, "actual-own-state.etf.json"),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(ownTerm))
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
        try
        {
            DebugDirtyOriginal424Provenance.Check();
        }
        catch (Exception error)
        {
            errors.Add(new { OriginalProvenance = error.ToString() });
        }
        // Decode expectations only after both independent routes and real owner cleanup.
        var differences = new List<object>();
        int examined = 0, equal = 0;
        var whole = new Dictionary<string, bool>();
        var raw = new Dictionary<string, bool>();
        foreach (string file in new[] { DebugDirtyOriginal424Names.Values, DebugDirtyOriginal424Names.StackValues })
        {
            byte[] bytes = DebugDirtyOriginal424Access.Resource(file);
            Term expected = OwnedFunctionOriginal407Terms.Decode(bytes);
            File.WriteAllBytes(Path.Combine(output, DebugDirtyOriginal424Names.Expected + file), bytes);
            File.WriteAllText(
                Path.Combine(output, DebugDirtyOriginal424Names.Expected + file + DebugDirtyOriginal424Names.Json),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expected))
            );
            bool present = captured.TryGetValue(file, out var actual);
            whole[file] = present && expected.Equals(actual.Term);
            raw[file] = present && bytes.AsSpan().SequenceEqual(actual.Bytes);
            if (!present) continue;
            equal += file == DebugDirtyOriginal424Names.Values
                ? IoPrintableOriginal404Comparison.Rows(
                    expected,
                    actual.Term,
                    file,
                    DebugDirtyOriginal424Names.Rows,
                    differences,
                    ref examined
                )
                : IoPrintableOriginal404Comparison.Profiles(
                    expected,
                    actual.Term,
                    DebugDirtyOriginal424Names.Rows,
                    differences,
                    ref examined
                );
        }
        bool ownState = false;
        try
        {
            byte[] expectedOwn = DebugDirtyOriginal424Access.Resource("own-state.etf");
            File.WriteAllBytes(Path.Combine(output, "expected-own-state.etf"), expectedOwn);
            Term source = OwnedFunctionOriginal407Terms.Decode(expectedOwn);
            File.WriteAllText(
                Path.Combine(output, "expected-own-state.etf.json"),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(source))
            );
            DebugDirtyOriginal424OwnState.Check(source);
            DebugDirtyOriginal424OwnState.Check(captured["own-state.etf"].Term);
            ownState = true;
        }
        catch (Exception error)
        {
            errors.Add(new { OwnStateProtocol = error.ToString() });
        }
        File.WriteAllText(
            Path.Combine(output, DebugDirtyOriginal424Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = DebugDirtyOriginal424Names.WholeRows,
                Examined = examined,
                Equal = equal,
                WholeTerms = whole,
                WholeRawEtf = raw,
                Errors = errors,
                Differences = differences,
                OwnStateProtocol = ownState,
                OwnStateRows = 12,
                RawOwnStateRetained = true,
                OwnStateRawCrossVmEquality = false,
                SourceDerivedPartialDebug = true,
                WholeOriginalDebugClaim = false,
                PhysicalNumericEqualityClaim = false,
                OriginalFailedReceiptPreserved = true,
                AcceptedPassiveGateSuccessor = true,
                ActualRunningStateComparedBySeparateHostControls = true,
                ProfileArity = 3,
                Projection = false,
                ApiOrFullContractPromotion = false
            })
        );
        DebugDirtyOriginal424Access.Require(ownState && errors.Count == 0 && differences.Count == 0
            && examined == DebugDirtyOriginal424Names.WholeRows && equal == examined
            && whole.Values.All(value => value) && raw.Values.All(value => value));
    }
}
