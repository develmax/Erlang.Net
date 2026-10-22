using System.Text.Json;
using Erlang;
namespace Erlang.Tests;

internal static class InternalTraceOriginal427Checks
{
    internal static async Task Run()
    {
        string output = Path.GetFullPath(Path.Combine(InternalTraceOriginal427Names.Output,
            System.Guid.NewGuid().ToString(InternalTraceOriginal427Names.Guid)));
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
                RuntimeBootstrapInternalTrace425.Register(owner, registry);
                var declared = new BeamDependencySelection([InternalTraceOriginal427Names.Runtime, InternalTraceOriginal427Names.File, InternalTraceOriginal427Names.Internal]);
                PreparedBeamImage facet = registry.PrepareBeam(InternalTraceOriginal427Access.Resource(InternalTraceOriginal427Names.FacetBeam), declared);
                InternalTraceOriginal427Access.Require(facet.Name == InternalTraceOriginal427Names.Internal
                    && facet.NativePrimitiveExports.Count == InternalTraceOriginal427Names.NativeCount
                    && InternalTraceOriginal427Names.NativeSignatures.All(signature => facet.NativePrimitiveExports.Contains(signature)));
                PreparedBeamImage worker = registry.PrepareBeam(InternalTraceOriginal427Access.Resource(InternalTraceOriginal427Names.WorkerBeam), declared);
                InternalTraceOriginal427Access.Require(registry.PublishPreparedBeam([facet, worker]).Status == BeamPublicationStatus.Published);
                foreach (var image in new[] { facet, worker })
                    foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                        InternalTraceOriginal427Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
                InternalTraceOriginal427Access.Require(registry.Exports.Count(export => export.Module == facet.Name) == InternalTraceOriginal427Names.FacetExports);
            });
            actor = new RuntimeNodeFoundationHostActor(owner);
            RuntimeNodeFoundationHostActor caller = actor;

            async Task Capture(string function, string file)
            {
                try
                {
                    Term value = await caller.InvokeAsync(async context => await owner.Modules.Call(context,
                        InternalTraceOriginal427Names.Module, function));
                    BitString wire = (BitString)await caller.InvokeAsync(async context => await owner.Modules.Call(
                        context,
                        InternalTraceOriginal427Names.Runtime,
                        InternalTraceOriginal427Names.Encode,
                        value
                    ));
                    captured.Add(file, (value, wire.ToArray()));
                    File.WriteAllBytes(Path.Combine(output, InternalTraceOriginal427Names.Actual + file), wire.ToArray());
                    File.WriteAllText(
                        Path.Combine(output, InternalTraceOriginal427Names.Actual + file + InternalTraceOriginal427Names.Json),
                        JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(value))
                    );
                }
                catch (Exception error)
                {
                    errors.Add(new { Route = file, Failure = error.ToString() });
                }
            }
            await Capture(InternalTraceOriginal427Names.Run, InternalTraceOriginal427Names.Values);
            await Capture(InternalTraceOriginal427Names.Stacks, InternalTraceOriginal427Names.StackValues);
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
        foreach (string file in new[] { InternalTraceOriginal427Names.Values, InternalTraceOriginal427Names.StackValues })
        {
            byte[] bytes = InternalTraceOriginal427Access.Resource(file);
            Term expected = OwnedFunctionOriginal407Terms.Decode(bytes);
            File.WriteAllBytes(Path.Combine(output, InternalTraceOriginal427Names.Expected + file), bytes);
            File.WriteAllText(
                Path.Combine(output, InternalTraceOriginal427Names.Expected + file + InternalTraceOriginal427Names.Json),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expected))
            );
            bool present = captured.TryGetValue(file, out var actual);
            whole[file] = present && expected.Equals(actual.Term);
            raw[file] = present && bytes.AsSpan().SequenceEqual(actual.Bytes);
            if (!present) continue;
            equal += file == InternalTraceOriginal427Names.Values
                ? IoPrintableOriginal404Comparison.Rows(
                    expected,
                    actual.Term,
                    file,
                    InternalTraceOriginal427Names.Rows,
                    differences,
                    ref examined
                )
                : IoPrintableOriginal404Comparison.Profiles(
                    expected,
                    actual.Term,
                    InternalTraceOriginal427Names.Rows,
                    differences,
                    ref examined
                );
        }
        File.WriteAllText(
            Path.Combine(output, InternalTraceOriginal427Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = InternalTraceOriginal427Names.WholeRows,
                Examined = examined,
                Equal = equal,
                WholeTerms = whole,
                WholeRawEtf = raw,
                Errors = errors,
                Differences = differences,
                OriginalCompiledWorkerUnchanged = true,
                SourceDerivedSevenStubFacet = true,
                ActualSourceOwnershipChecked = true,
                ExpectedSessionResourcePredicatesUnchanged = true,
                WholeOriginalInternalImageClaim = false,
                PhysicalNumericEqualityClaim = false,
                ProfileArity = 3,
                Projection = false,
                ApiOrFullContractPromotion = false
            })
        );
        InternalTraceOriginal427Access.Require(errors.Count == 0 && differences.Count == 0
            && examined == InternalTraceOriginal427Names.WholeRows && equal == examined
            && whole.Values.All(value => value) && raw.Values.All(value => value));
    }
}
