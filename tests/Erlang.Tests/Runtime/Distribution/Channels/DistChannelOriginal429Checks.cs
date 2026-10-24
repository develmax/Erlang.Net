using System.Text.Json;
using Erlang;
namespace Erlang.Tests;

internal static class DistChannelOriginal429Checks
{
    internal static async Task Run()
    {
        string output = Path.GetFullPath(Path.Combine(DistChannelOriginal429Names.Output,
            System.Guid.NewGuid().ToString(DistChannelOriginal429Names.Guid)));
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
                RuntimeBootstrapDistributionProfile429.Register(owner, registry);
                RuntimeBootstrapDistChannel429.Register(owner, registry);
                var declared = new BeamDependencySelection([DistChannelOriginal429Names.Runtime, DistChannelOriginal429Names.File, DistChannelOriginal429Names.Internal]);
                PreparedBeamImage facet = registry.PrepareBeam(DistChannelOriginal429Access.Resource(DistChannelOriginal429Names.FacetBeam), declared);
                DistChannelOriginal429Access.Require(facet.Name == DistChannelOriginal429Names.Internal
                    && facet.NativePrimitiveExports.Count == DistChannelOriginal429Names.NativeCount
                    && DistChannelOriginal429Names.NativeSignatures.All(signature => facet.NativePrimitiveExports.Contains(signature)));
                PreparedBeamImage worker = registry.PrepareBeam(DistChannelOriginal429Access.Resource(DistChannelOriginal429Names.WorkerBeam), declared);
                DistChannelOriginal429Access.Require(registry.PublishPreparedBeam([facet, worker]).Status == BeamPublicationStatus.Published);
                foreach (var image in new[] { facet, worker })
                    foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                        DistChannelOriginal429Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
                DistChannelOriginal429Access.Require(registry.Exports.Count(export => export.Module == facet.Name) == DistChannelOriginal429Names.FacetExports);
            });
            actor = new RuntimeNodeFoundationHostActor(owner);
            RuntimeNodeFoundationHostActor caller = actor;

            async Task Capture(string function, string file)
            {
                try
                {
                    Term value = await caller.InvokeAsync(async context => await owner.Modules.Call(context,
                        DistChannelOriginal429Names.Module, function));
                    BitString wire = (BitString)await caller.InvokeAsync(async context => await owner.Modules.Call(
                        context,
                        DistChannelOriginal429Names.Runtime,
                        DistChannelOriginal429Names.Encode,
                        value
                    ));
                    captured.Add(file, (value, wire.ToArray()));
                    File.WriteAllBytes(Path.Combine(output, DistChannelOriginal429Names.Actual + file), wire.ToArray());
                    File.WriteAllText(
                        Path.Combine(output, DistChannelOriginal429Names.Actual + file + DistChannelOriginal429Names.Json),
                        JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(value))
                    );
                }
                catch (Exception error)
                {
                    errors.Add(new { Route = file, Failure = error.ToString() });
                }
            }
            await Capture(DistChannelOriginal429Names.Run, DistChannelOriginal429Names.Values);
            await Capture(DistChannelOriginal429Names.Stacks, DistChannelOriginal429Names.StackValues);
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
        foreach (string file in new[] { DistChannelOriginal429Names.Values, DistChannelOriginal429Names.StackValues })
        {
            byte[] bytes = DistChannelOriginal429Access.Resource(file);
            Term expected = OwnedFunctionOriginal407Terms.Decode(bytes);
            File.WriteAllBytes(Path.Combine(output, DistChannelOriginal429Names.Expected + file), bytes);
            File.WriteAllText(
                Path.Combine(output, DistChannelOriginal429Names.Expected + file + DistChannelOriginal429Names.Json),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expected))
            );
            bool present = captured.TryGetValue(file, out var actual);
            whole[file] = present && expected.Equals(actual.Term);
            raw[file] = present && bytes.AsSpan().SequenceEqual(actual.Bytes);
            if (!present) continue;
            equal += file == DistChannelOriginal429Names.Values
                ? IoPrintableOriginal404Comparison.Rows(
                    expected,
                    actual.Term,
                    file,
                    DistChannelOriginal429Names.Rows,
                    differences,
                    ref examined
                )
                : IoPrintableOriginal404Comparison.Profiles(
                    expected,
                    actual.Term,
                    DistChannelOriginal429Names.Rows,
                    differences,
                    ref examined
                );
        }
        File.WriteAllText(
            Path.Combine(output, DistChannelOriginal429Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = DistChannelOriginal429Names.WholeRows,
                Examined = examined,
                Equal = equal,
                WholeTerms = whole,
                WholeRawEtf = raw,
                Errors = errors,
                Differences = differences,
                OriginalCompiledWorkerUnchanged = true,
                SourceDerivedThreeStubFacet = true,
                ActualSourceOwnershipChecked = true,
                ActualNativePidChannelProtocol = true,
                SourceOwnProtocolPredicates = true,
                PortControllerOrPendingDownClaim = false,
                WholeOriginalInternalImageClaim = false,
                PhysicalNumericEqualityClaim = false,
                ProfileArity = 3,
                Projection = false,
                ApiOrFullContractPromotion = false
            })
        );
        DistChannelOriginal429Access.Require(errors.Count == 0 && differences.Count == 0
            && examined == DistChannelOriginal429Names.WholeRows && equal == examined
            && whole.Values.All(value => value) && raw.Values.All(value => value));
    }
}
