using System.Text.Json;
using Erlang;
namespace Erlang.Tests;

internal static class InternalSpawnOriginal429Checks
{
    internal static async Task Run()
    {
        string output = Path.GetFullPath(Path.Combine(InternalSpawnOriginal429Names.Output,
            System.Guid.NewGuid().ToString(InternalSpawnOriginal429Names.Guid)));
        Directory.CreateDirectory(output);
        var captured = new Dictionary<string, (Term Term, byte[] Bytes)>();
        var errors = new List<object>();
        ProcessRuntime? runtime = null;
        RuntimeNodeFoundationHostActor? actor = null;
        try
        {
            runtime = new ProcessRuntime(1);
            ProcessRuntime owner = runtime;
            var declared = new BeamDependencySelection(new[] { InternalSpawnOriginal429Names.Runtime, InternalSpawnOriginal429Names.File, InternalSpawnOriginal429Names.Internal, InternalSpawnOriginal429Names.Module });
            await PreparedBeamFixtureImages825.PrepareAndPublishAsync(
                owner,
                [InternalSpawnOriginal429Access.Resource(InternalSpawnOriginal429Names.FacetBeam), InternalSpawnOriginal429Access.Resource(InternalSpawnOriginal429Names.WorkerBeam)],
                declared,
                (registry, images) =>
                {
                    PreparedBeamImage facet = images[0];
                    PreparedBeamImage worker = images[1];
                    InternalSpawnOriginal429Access.Require(registry.PublishPreparedBeam(new[] { facet, worker }).Status == BeamPublicationStatus.Published);
                    InternalSpawnOriginal429Access.Require(facet.NativePrimitiveExports.Count == InternalSpawnOriginal429Names.NativeCount
                        && facet.NativePrimitiveExports.Contains(new BeamFunctionSignature(InternalSpawnOriginal429Names.NativeFunction, 4)));
                    InternalSpawnOriginal429Access.Require(registry.Exports.Count(export => export.Module == facet.Name) == InternalSpawnOriginal429Names.TotalExports);
                    foreach (var image in new[] { facet, worker })
                        foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                            InternalSpawnOriginal429Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
                }
            );
            actor = new RuntimeNodeFoundationHostActor(owner);
            RuntimeNodeFoundationHostActor caller = actor;

            async Task Capture(string function, string file)
            {
                try
                {
                    Term value = await caller.InvokeAsync(async context => await owner.Modules.Call(context,
                        InternalSpawnOriginal429Names.Module, function));
                    BitString wire = (BitString)await caller.InvokeAsync(async context => await owner.Modules.Call(
                        context,
                        InternalSpawnOriginal429Names.Runtime,
                        InternalSpawnOriginal429Names.Encode,
                        value
                    ));
                    captured.Add(file, (value, wire.ToArray()));
                    File.WriteAllBytes(Path.Combine(output, InternalSpawnOriginal429Names.Actual + file), wire.ToArray());
                    File.WriteAllText(
                        Path.Combine(output, InternalSpawnOriginal429Names.Actual + file + InternalSpawnOriginal429Names.Json),
                        JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(value))
                    );
                }
                catch (Exception error)
                {
                    errors.Add(new { Route = file, Failure = error.ToString() });
                }
            }
            await Capture(InternalSpawnOriginal429Names.Run, InternalSpawnOriginal429Names.Values);
            await Capture(InternalSpawnOriginal429Names.Stacks, InternalSpawnOriginal429Names.StackValues);
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
        foreach (string file in new[] { InternalSpawnOriginal429Names.Values, InternalSpawnOriginal429Names.StackValues })
        {
            byte[] bytes = InternalSpawnOriginal429Access.Resource(file);
            Term expected = OwnedFunctionOriginal407Terms.Decode(bytes);
            File.WriteAllBytes(Path.Combine(output, InternalSpawnOriginal429Names.Expected + file), bytes);
            File.WriteAllText(
                Path.Combine(output, InternalSpawnOriginal429Names.Expected + file + InternalSpawnOriginal429Names.Json),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expected))
            );
            bool present = captured.TryGetValue(file, out var actual);
            whole[file] = present && expected.Equals(actual.Term);
            raw[file] = present && bytes.AsSpan().SequenceEqual(actual.Bytes);
            if (!present) continue;
            equal += file == InternalSpawnOriginal429Names.Values
                ? IoPrintableOriginal404Comparison.Rows(
                    expected,
                    actual.Term,
                    file,
                    InternalSpawnOriginal429Names.Rows,
                    differences,
                    ref examined
                )
                : IoPrintableOriginal404Comparison.Profiles(
                    expected,
                    actual.Term,
                    InternalSpawnOriginal429Names.Rows,
                    differences,
                    ref examined
                );
        }
        File.WriteAllText(
            Path.Combine(output, InternalSpawnOriginal429Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = InternalSpawnOriginal429Names.WholeRows,
                Examined = examined,
                Equal = equal,
                WholeTerms = whole,
                WholeRawEtf = raw,
                Errors = errors,
                Differences = differences,
                OrderedProcessSignals = true,
                WholeOriginalInternalImageClaim = false,
                PhysicalNumericEqualityClaim = false,
                ProfileArity = 3,
                Projection = false,
                ApiOrFullContractPromotion = false
            })
        );
        InternalSpawnOriginal429Access.Require(errors.Count == 0 && differences.Count == 0
            && examined == InternalSpawnOriginal429Names.WholeRows && equal == examined
            && whole.Values.All(value => value) && raw.Values.All(value => value));
    }
}
