using System.Text.Json;
using Erlang;
namespace Erlang.Tests;

internal static class AllocatorGatherOriginal434Checks
{
    internal static async Task Run()
    {
        string output = Path.GetFullPath(Path.Combine(AllocatorGatherOriginal434Names.Output,
            System.Guid.NewGuid().ToString(AllocatorGatherOriginal434Names.Guid)));
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
                var declared = new BeamDependencySelection(new[] { AllocatorGatherOriginal434Names.Runtime, AllocatorGatherOriginal434Names.File, AllocatorGatherOriginal434Names.Internal, AllocatorGatherOriginal434Names.Module });
                PreparedBeamImage facet = registry.PrepareBeam(AllocatorGatherOriginal434Access.Resource(AllocatorGatherOriginal434Names.FacetBeam), declared);
                PreparedBeamImage worker = registry.PrepareBeam(AllocatorGatherOriginal434Access.Resource(AllocatorGatherOriginal434Names.WorkerBeam), declared);
                AllocatorGatherOriginal434Access.Require(registry.PublishPreparedBeam(new[] { facet, worker }).Status == BeamPublicationStatus.Published);
                AllocatorGatherOriginal434Access.Require(facet.NativePrimitiveExports.Count == AllocatorGatherOriginal434Names.NativeCount
                    && facet.NativePrimitiveExports.Contains(new BeamFunctionSignature(AllocatorGatherOriginal434Names.NativeFunction, 1)));
                AllocatorGatherOriginal434Access.Require(registry.Exports.Count(export => export.Module == facet.Name) == AllocatorGatherOriginal434Names.TotalExports);
                foreach (var image in new[] { facet, worker })
                    foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                        AllocatorGatherOriginal434Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
            });
            actor = new RuntimeNodeFoundationHostActor(owner);
            RuntimeNodeFoundationHostActor caller = actor;

            async Task Capture(string function, string file)
            {
                try
                {
                    Term value = await caller.InvokeAsync(async context => await owner.Modules.Call(context,
                        AllocatorGatherOriginal434Names.Module, function));
                    BitString wire = (BitString)await caller.InvokeAsync(async context => await owner.Modules.Call(
                        context,
                        AllocatorGatherOriginal434Names.Runtime,
                        AllocatorGatherOriginal434Names.Encode,
                        value
                    ));
                    captured.Add(file, (value, wire.ToArray()));
                    File.WriteAllBytes(Path.Combine(output, AllocatorGatherOriginal434Names.Actual + file), wire.ToArray());
                    File.WriteAllText(
                        Path.Combine(output, AllocatorGatherOriginal434Names.Actual + file + AllocatorGatherOriginal434Names.Json),
                        JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(value))
                    );
                }
                catch (Exception error)
                {
                    errors.Add(new { Route = file, Failure = error.ToString() });
                }
            }
            await Capture(AllocatorGatherOriginal434Names.Run, AllocatorGatherOriginal434Names.Values);
            await Capture(AllocatorGatherOriginal434Names.Stacks, AllocatorGatherOriginal434Names.StackValues);
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
        foreach (string file in new[] { AllocatorGatherOriginal434Names.Values, AllocatorGatherOriginal434Names.StackValues })
        {
            byte[] bytes = AllocatorGatherOriginal434Access.Resource(file);
            Term expected = OwnedFunctionOriginal407Terms.Decode(bytes);
            File.WriteAllBytes(Path.Combine(output, AllocatorGatherOriginal434Names.Expected + file), bytes);
            File.WriteAllText(
                Path.Combine(output, AllocatorGatherOriginal434Names.Expected + file + AllocatorGatherOriginal434Names.Json),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expected))
            );
            bool present = captured.TryGetValue(file, out var actual);
            whole[file] = present && expected.Equals(actual.Term);
            raw[file] = present && bytes.AsSpan().SequenceEqual(actual.Bytes);
            if (!present) continue;
            equal += file == AllocatorGatherOriginal434Names.Values
                ? IoPrintableOriginal404Comparison.Rows(
                    expected,
                    actual.Term,
                    file,
                    AllocatorGatherOriginal434Names.Rows,
                    differences,
                    ref examined
                )
                : IoPrintableOriginal404Comparison.Profiles(
                    expected,
                    actual.Term,
                    AllocatorGatherOriginal434Names.Rows,
                    differences,
                    ref examined
                );
        }
        File.WriteAllText(
            Path.Combine(output, AllocatorGatherOriginal434Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = AllocatorGatherOriginal434Names.WholeRows,
                Examined = examined,
                Equal = equal,
                WholeTerms = whole,
                WholeRawEtf = raw,
                Errors = errors,
                Differences = differences,
                OrderedProcessSignals = true,
                WholeOriginalInternalImageClaim = false,
                PhysicalNumericEqualityClaim = false,
                ProfileArity = 6,
                Projection = false,
                ApiOrFullContractPromotion = false
            })
        );
        AllocatorGatherOriginal434Access.Require(errors.Count == 0 && differences.Count == 0
            && examined == AllocatorGatherOriginal434Names.WholeRows && equal == examined
            && whole.Values.All(value => value) && raw.Values.All(value => value));
    }
}
