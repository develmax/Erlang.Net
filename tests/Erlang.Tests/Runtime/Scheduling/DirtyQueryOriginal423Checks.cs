using System.Text.Json;
using Erlang;
namespace Erlang.Tests;

internal static class DirtyQueryOriginal423Checks
{
    internal static async Task Run()
    {
        string output = Path.GetFullPath(Path.Combine(DirtyQueryOriginal423Names.Output,
            System.Guid.NewGuid().ToString(DirtyQueryOriginal423Names.Guid)));
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
                RuntimeBootstrapDirtyQuery423.Register(owner, registry);
                var declared = new BeamDependencySelection([DirtyQueryOriginal423Names.Runtime, DirtyQueryOriginal423Names.File, DirtyQueryOriginal423Names.Internal]);
                PreparedBeamImage facet = registry.PrepareBeam(DirtyQueryOriginal423Access.Resource(DirtyQueryOriginal423Names.FacetBeam), declared);
                DirtyQueryOriginal423Access.Require(facet.Name == DirtyQueryOriginal423Names.Internal && facet.NativePrimitiveExports.Count == 1
                    && facet.NativePrimitiveExports.Single().Name == DirtyQueryOriginal423Names.Function && facet.NativePrimitiveExports.Single().Arity == 1);
                PreparedBeamImage worker = registry.PrepareBeam(DirtyQueryOriginal423Access.Resource(DirtyQueryOriginal423Names.WorkerBeam), declared);
                DirtyQueryOriginal423Access.Require(registry.PublishPreparedBeam([facet, worker]).Status == BeamPublicationStatus.Published);
                DirtyQueryOriginal423Access.Require(registry.Exports.Count(export => export.Module == DirtyQueryOriginal423Names.Internal) == 3);
                foreach (var image in new[] { facet, worker })
                    foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                        DirtyQueryOriginal423Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
            });
            actor = new RuntimeNodeFoundationHostActor(owner);
            RuntimeNodeFoundationHostActor caller = actor;

            async Task Capture(string function, string file)
            {
                try
                {
                    Term value = await caller.InvokeAsync(async context => await owner.Modules.Call(context,
                        DirtyQueryOriginal423Names.Module, function));
                    BitString wire = (BitString)await caller.InvokeAsync(async context => await owner.Modules.Call(
                        context,
                        DirtyQueryOriginal423Names.Runtime,
                        DirtyQueryOriginal423Names.Encode,
                        value
                    ));
                    captured.Add(file, (value, wire.ToArray()));
                    File.WriteAllBytes(Path.Combine(output, DirtyQueryOriginal423Names.Actual + file), wire.ToArray());
                    File.WriteAllText(
                        Path.Combine(output, DirtyQueryOriginal423Names.Actual + file + DirtyQueryOriginal423Names.Json),
                        JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(value))
                    );
                }
                catch (Exception error)
                {
                    errors.Add(new { Route = file, Failure = error.ToString() });
                }
            }
            await Capture(DirtyQueryOriginal423Names.Run, DirtyQueryOriginal423Names.Values);
            await Capture(DirtyQueryOriginal423Names.Stacks, DirtyQueryOriginal423Names.StackValues);
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
        foreach (string file in new[] { DirtyQueryOriginal423Names.Values, DirtyQueryOriginal423Names.StackValues })
        {
            byte[] bytes = DirtyQueryOriginal423Access.Resource(file);
            Term expected = OwnedFunctionOriginal407Terms.Decode(bytes);
            File.WriteAllBytes(Path.Combine(output, DirtyQueryOriginal423Names.Expected + file), bytes);
            File.WriteAllText(
                Path.Combine(output, DirtyQueryOriginal423Names.Expected + file + DirtyQueryOriginal423Names.Json),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expected))
            );
            bool present = captured.TryGetValue(file, out var actual);
            whole[file] = present && expected.Equals(actual.Term);
            raw[file] = present && bytes.AsSpan().SequenceEqual(actual.Bytes);
            if (!present) continue;
            equal += file == DirtyQueryOriginal423Names.Values
                ? IoPrintableOriginal404Comparison.Rows(
                    expected,
                    actual.Term,
                    file,
                    DirtyQueryOriginal423Names.Rows,
                    differences,
                    ref examined
                )
                : IoPrintableOriginal404Comparison.Profiles(
                    expected,
                    actual.Term,
                    DirtyQueryOriginal423Names.Rows,
                    differences,
                    ref examined
                );
        }
        File.WriteAllText(
            Path.Combine(output, DirtyQueryOriginal423Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = DirtyQueryOriginal423Names.WholeRows,
                Examined = examined,
                Equal = equal,
                WholeTerms = whole,
                WholeRawEtf = raw,
                Errors = errors,
                Differences = differences,
                SourceDerivedPartialInternal = true,
                WholeOriginalInternalClaim = false,
                PhysicalNumericEqualityClaim = false,
                OriginalFailedReceiptPreserved = true,
                AcceptedPassiveGateSuccessor = true,
                ActualRunningStateComparedBySeparateHostControls = true,
                ProfileArity = 3,
                Projection = false,
                ApiOrFullContractPromotion = false
            })
        );
        DirtyQueryOriginal423Access.Require(errors.Count == 0 && differences.Count == 0
            && examined == DirtyQueryOriginal423Names.WholeRows && equal == examined
            && whole.Values.All(value => value) && raw.Values.All(value => value));
    }
}
