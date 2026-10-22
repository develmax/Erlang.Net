using System.Text.Json;
using Erlang;
namespace Erlang.Tests;

internal static class DistDecodeOriginal423Checks
{
    internal static async Task Run()
    {
        string output = Path.GetFullPath(Path.Combine(DistDecodeOriginal423Names.Output,
            System.Guid.NewGuid().ToString(DistDecodeOriginal423Names.Guid)));
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
                RuntimeBootstrapDistDecode423.Register(owner, registry);
                var declared = new BeamDependencySelection([DistDecodeOriginal423Names.Runtime, DistDecodeOriginal423Names.File, DistDecodeOriginal423Names.Internal]);
                PreparedBeamImage facet = registry.PrepareBeam(DistDecodeOriginal423Access.Resource(DistDecodeOriginal423Names.FacetBeam), declared);
                DistDecodeOriginal423Access.Require(facet.Name == DistDecodeOriginal423Names.Internal && facet.NativePrimitiveExports.Count == 1
                    && facet.NativePrimitiveExports.Single().Name == DistDecodeOriginal423Names.Function && facet.NativePrimitiveExports.Single().Arity == 2);
                PreparedBeamImage worker = registry.PrepareBeam(DistDecodeOriginal423Access.Resource(DistDecodeOriginal423Names.WorkerBeam), declared);
                DistDecodeOriginal423Access.Require(registry.PublishPreparedBeam([facet, worker]).Status == BeamPublicationStatus.Published);
                DistDecodeOriginal423Access.Require(registry.Exports.Count(export => export.Module == DistDecodeOriginal423Names.Internal) == 3);
                foreach (var image in new[] { facet, worker })
                    foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                        DistDecodeOriginal423Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
            });
            actor = new RuntimeNodeFoundationHostActor(owner);
            RuntimeNodeFoundationHostActor caller = actor;

            async Task Capture(string function, string file)
            {
                try
                {
                    Term value = await caller.InvokeAsync(async context => await owner.Modules.Call(context,
                        DistDecodeOriginal423Names.Module, function));
                    BitString wire = (BitString)await caller.InvokeAsync(async context => await owner.Modules.Call(
                        context,
                        DistDecodeOriginal423Names.Runtime,
                        DistDecodeOriginal423Names.Encode,
                        value
                    ));
                    captured.Add(file, (value, wire.ToArray()));
                    File.WriteAllBytes(Path.Combine(output, DistDecodeOriginal423Names.Actual + file), wire.ToArray());
                    File.WriteAllText(
                        Path.Combine(output, DistDecodeOriginal423Names.Actual + file + DistDecodeOriginal423Names.Json),
                        JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(value))
                    );
                }
                catch (Exception error)
                {
                    errors.Add(new { Route = file, Failure = error.ToString() });
                }
            }
            await Capture(DistDecodeOriginal423Names.Run, DistDecodeOriginal423Names.Values);
            await Capture(DistDecodeOriginal423Names.Stacks, DistDecodeOriginal423Names.StackValues);
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
        foreach (string file in new[] { DistDecodeOriginal423Names.Values, DistDecodeOriginal423Names.StackValues })
        {
            byte[] bytes = DistDecodeOriginal423Access.Resource(file);
            Term expected = OwnedFunctionOriginal407Terms.Decode(bytes);
            File.WriteAllBytes(Path.Combine(output, DistDecodeOriginal423Names.Expected + file), bytes);
            File.WriteAllText(
                Path.Combine(output, DistDecodeOriginal423Names.Expected + file + DistDecodeOriginal423Names.Json),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expected))
            );
            bool present = captured.TryGetValue(file, out var actual);
            whole[file] = present && expected.Equals(actual.Term);
            raw[file] = present && bytes.AsSpan().SequenceEqual(actual.Bytes);
            if (!present) continue;
            equal += file == DistDecodeOriginal423Names.Values
                ? IoPrintableOriginal404Comparison.Rows(
                    expected,
                    actual.Term,
                    file,
                    DistDecodeOriginal423Names.Rows,
                    differences,
                    ref examined
                )
                : IoPrintableOriginal404Comparison.Profiles(
                    expected,
                    actual.Term,
                    DistDecodeOriginal423Names.Rows,
                    differences,
                    ref examined
                );
        }
        File.WriteAllText(
            Path.Combine(output, DistDecodeOriginal423Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = DistDecodeOriginal423Names.WholeRows,
                Examined = examined,
                Equal = equal,
                WholeTerms = whole,
                WholeRawEtf = raw,
                Errors = errors,
                Differences = differences,
                SourceDerivedPartialDebug = true,
                WholeOriginalDebugClaim = false,
                PhysicalNumericEqualityClaim = false,
                OwnLocalStatePredicatesFromSameWorker = true,
                PriorFailedOriginal216RetainedNotCreditedAsRawEquality = true,
                NativeVmFacetPa = false,
                ProfileArity = 3,
                Projection = false,
                ApiOrFullContractPromotion = false
            })
        );
        DistDecodeOriginal423Access.Require(errors.Count == 0 && differences.Count == 0
            && examined == DistDecodeOriginal423Names.WholeRows && equal == examined
            && whole.Values.All(value => value) && raw.Values.All(value => value));
    }
}
