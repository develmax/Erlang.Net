using System.Text.Json;
using Erlang;
namespace Erlang.Tests;

internal static class DebugCodeOriginal427Checks
{
    internal static async Task Run()
    {
        string output = Path.GetFullPath(Path.Combine(DebugCodeOriginal427Names.Output,
            System.Guid.NewGuid().ToString(DebugCodeOriginal427Names.Guid)));
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
                RuntimeBootstrapDebugCode427.Register(owner, registry);
                var declared = new BeamDependencySelection([DebugCodeOriginal427Names.Runtime, DebugCodeOriginal427Names.File, DebugCodeOriginal427Names.Internal]);
                PreparedBeamImage facet = registry.PrepareBeam(DebugCodeOriginal427Access.Resource(DebugCodeOriginal427Names.FacetBeam), declared);
                DebugCodeOriginal427Access.Require(facet.Name == DebugCodeOriginal427Names.Internal && facet.NativePrimitiveExports.Count == 2
                    && facet.NativePrimitiveExports.Contains(new BeamFunctionSignature("disassemble", 1)) && facet.NativePrimitiveExports.Contains(new BeamFunctionSignature("interpreter_size", 0)));
                PreparedBeamImage worker = registry.PrepareBeam(DebugCodeOriginal427Access.Resource(DebugCodeOriginal427Names.WorkerBeam), declared);
                DebugCodeOriginal427Access.Require(registry.PublishPreparedBeam([facet, worker]).Status == BeamPublicationStatus.Published);
                DebugCodeOriginal427Access.Require(registry.Exports.Count(export => export.Module == DebugCodeOriginal427Names.Internal) == 4);
                foreach (var image in new[] { facet, worker })
                    foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                        DebugCodeOriginal427Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
            });
            actor = new RuntimeNodeFoundationHostActor(owner);
            RuntimeNodeFoundationHostActor caller = actor;

            async Task Capture(string function, string file)
            {
                try
                {
                    Term value = await caller.InvokeAsync(async context => await owner.Modules.Call(context,
                        DebugCodeOriginal427Names.Module, function));
                    BitString wire = (BitString)await caller.InvokeAsync(async context => await owner.Modules.Call(
                        context,
                        DebugCodeOriginal427Names.Runtime,
                        DebugCodeOriginal427Names.Encode,
                        value
                    ));
                    captured.Add(file, (value, wire.ToArray()));
                    File.WriteAllBytes(Path.Combine(output, DebugCodeOriginal427Names.Actual + file), wire.ToArray());
                    File.WriteAllText(
                        Path.Combine(output, DebugCodeOriginal427Names.Actual + file + DebugCodeOriginal427Names.Json),
                        JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(value))
                    );
                }
                catch (Exception error)
                {
                    errors.Add(new { Route = file, Failure = error.ToString() });
                }
            }
            Term witness = await caller.InvokeAsync(async context => await owner.Modules.Call(context, DebugCodeOriginal427Names.Module, "witness"));
            DebugCodeOriginal427Access.Require(witness.Equals(Term.Tuple(Term.A("managed_execution_witness"), Term.I(13))));
            await Capture(DebugCodeOriginal427Names.Run, DebugCodeOriginal427Names.Values);
            await Capture(DebugCodeOriginal427Names.Stacks, DebugCodeOriginal427Names.StackValues);
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
        bool originalJit = false;
        try
        {
            using JsonDocument receipt = JsonDocument.Parse(DebugCodeOriginal427Access.Resource("original-completion.json"));
            JsonElement proof = receipt.RootElement;
            originalJit = proof.GetProperty("Passed").GetBoolean() && proof.GetProperty("OriginalJitProfilePassed").GetBoolean()
                && proof.GetProperty("WholeRows").GetInt32() == 126 && !proof.GetProperty("ExpectedRewritten").GetBoolean();
            Term profile = OwnedFunctionOriginal407Terms.Decode(DebugCodeOriginal427Access.Resource("profile.etf"));
            originalJit &= profile is TupleTerm { Items.Count: 3 } p && p.Items[0].Equals(Term.A("jit"));
            File.WriteAllText(
                Path.Combine(output, "original-profile.etf.json"),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(profile))
            );
        }
        catch (Exception error)
        {
            errors.Add(new { OriginalBackendProfile = error.ToString() });
        }
        // Decode expectations only after both independent routes and real owner cleanup.
        var differences = new List<object>();
        int examined = 0, equal = 0;
        var whole = new Dictionary<string, bool>();
        var raw = new Dictionary<string, bool>();
        foreach (string file in new[] { DebugCodeOriginal427Names.Values, DebugCodeOriginal427Names.StackValues })
        {
            byte[] bytes = DebugCodeOriginal427Access.Resource(file);
            Term expected = OwnedFunctionOriginal407Terms.Decode(bytes);
            File.WriteAllBytes(Path.Combine(output, DebugCodeOriginal427Names.Expected + file), bytes);
            File.WriteAllText(
                Path.Combine(output, DebugCodeOriginal427Names.Expected + file + DebugCodeOriginal427Names.Json),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expected))
            );
            bool present = captured.TryGetValue(file, out var actual);
            whole[file] = present && expected.Equals(actual.Term);
            raw[file] = present && bytes.AsSpan().SequenceEqual(actual.Bytes);
            if (!present) continue;
            equal += file == DebugCodeOriginal427Names.Values
                ? IoPrintableOriginal404Comparison.Rows(
                    expected,
                    actual.Term,
                    file,
                    DebugCodeOriginal427Names.Rows,
                    differences,
                    ref examined
                )
                : IoPrintableOriginal404Comparison.Profiles(
                    expected,
                    actual.Term,
                    DebugCodeOriginal427Names.Rows,
                    differences,
                    ref examined
                );
        }
        File.WriteAllText(
            Path.Combine(output, DebugCodeOriginal427Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = DebugCodeOriginal427Names.WholeRows,
                Examined = examined,
                Equal = equal,
                WholeTerms = whole,
                WholeRawEtf = raw,
                Errors = errors,
                Differences = differences,
                ActualOriginalJitProfile = originalJit,
                ActualManagedWitnessExecuted = true,
                SourceDerivedPartialDebug = true,
                WholeOriginalDebugClaim = false,
                PhysicalNumericEqualityClaim = false,
                OriginalCompilerVmRerunFromConsumer = false,
                ActualBackendOwnerComparedBySeparateHostControls = true,
                ProfileArity = 3,
                Projection = false,
                ApiOrFullContractPromotion = false
            })
        );
        DebugCodeOriginal427Access.Require(originalJit && errors.Count == 0 && differences.Count == 0
            && examined == DebugCodeOriginal427Names.WholeRows && equal == examined
            && whole.Values.All(value => value) && raw.Values.All(value => value));
    }
}
