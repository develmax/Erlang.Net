using System.Text.Json;
using Erlang;
namespace Erlang.Tests;

internal static class InternalGcOriginal428Checks
{
    internal static async Task Run()
    {
        string output = Path.GetFullPath(Path.Combine(InternalGcOriginal428Names.Output,
            System.Guid.NewGuid().ToString(InternalGcOriginal428Names.Guid)));
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
                    RuntimeBootstrapInternalGarbageCollection428.Register(owner, registry);
                });
                var declared = new BeamDependencySelection([InternalGcOriginal428Names.Runtime, InternalGcOriginal428Names.File, InternalGcOriginal428Names.Internal]);
                PreparedBeamImage facet = scope.Prepare(() => registry.PrepareBeam(InternalGcOriginal428Access.Resource(InternalGcOriginal428Names.FacetBeam), declared));
                InternalGcOriginal428Access.Require(facet.Name == InternalGcOriginal428Names.Internal
                    && facet.NativePrimitiveExports.Count == InternalGcOriginal428Names.NativeCount
                    && InternalGcOriginal428Names.NativeSignatures.All(signature => facet.NativePrimitiveExports.Contains(signature)));
                PreparedBeamImage worker = scope.Prepare(() => registry.PrepareBeam(InternalGcOriginal428Access.Resource(InternalGcOriginal428Names.WorkerBeam), declared));

                return _ =>
                {
                    InternalGcOriginal428Access.Require(registry.PublishPreparedBeam([facet, worker]).Status == BeamPublicationStatus.Published);
                    foreach (var image in new[] { facet, worker })
                        foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                            InternalGcOriginal428Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
                    InternalGcOriginal428Access.Require(registry.Exports.Count(export => export.Module == facet.Name) == InternalGcOriginal428Names.FacetExports);
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
                        InternalGcOriginal428Names.Module, function));
                    BitString wire = (BitString)await caller.InvokeAsync(async context => await owner.Modules.Call(
                        context,
                        InternalGcOriginal428Names.Runtime,
                        InternalGcOriginal428Names.Encode,
                        value
                    ));
                    captured.Add(file, (value, wire.ToArray()));
                    File.WriteAllBytes(Path.Combine(output, InternalGcOriginal428Names.Actual + file), wire.ToArray());
                    File.WriteAllText(
                        Path.Combine(output, InternalGcOriginal428Names.Actual + file + InternalGcOriginal428Names.Json),
                        JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(value))
                    );
                }
                catch (Exception error)
                {
                    errors.Add(new { Route = file, Failure = error.ToString() });
                }
            }
            await Capture(InternalGcOriginal428Names.Run, InternalGcOriginal428Names.Values);
            await Capture(InternalGcOriginal428Names.Stacks, InternalGcOriginal428Names.StackValues);
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
        foreach (string file in new[] { InternalGcOriginal428Names.Values, InternalGcOriginal428Names.StackValues })
        {
            byte[] bytes = InternalGcOriginal428Access.Resource(file);
            Term expected = OwnedFunctionOriginal407Terms.Decode(bytes);
            File.WriteAllBytes(Path.Combine(output, InternalGcOriginal428Names.Expected + file), bytes);
            File.WriteAllText(
                Path.Combine(output, InternalGcOriginal428Names.Expected + file + InternalGcOriginal428Names.Json),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expected))
            );
            bool present = captured.TryGetValue(file, out var actual);
            whole[file] = present && expected.Equals(actual.Term);
            raw[file] = present && bytes.AsSpan().SequenceEqual(actual.Bytes);
            if (!present) continue;
            equal += file == InternalGcOriginal428Names.Values
                ? IoPrintableOriginal404Comparison.Rows(
                    expected,
                    actual.Term,
                    file,
                    InternalGcOriginal428Names.Rows,
                    differences,
                    ref examined
                )
                : IoPrintableOriginal404Comparison.Profiles(
                    expected,
                    actual.Term,
                    InternalGcOriginal428Names.Rows,
                    differences,
                    ref examined
                );
        }
        File.WriteAllText(
            Path.Combine(output, InternalGcOriginal428Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = InternalGcOriginal428Names.WholeRows,
                Examined = examined,
                Equal = equal,
                WholeTerms = whole,
                WholeRawEtf = raw,
                Errors = errors,
                Differences = differences,
                OriginalCompiledWorkerUnchanged = true,
                SourceDerivedOneStubFacet = true,
                ActualSourceOwnershipChecked = true,
                RealGcBackendNotPhysicalErlangHeapClaim = true,
                WholeOriginalInternalImageClaim = false,
                PhysicalNumericEqualityClaim = false,
                ProfileArity = 3,
                Projection = false,
                ApiOrFullContractPromotion = false
            })
        );
        InternalGcOriginal428Access.Require(errors.Count == 0 && differences.Count == 0
            && examined == InternalGcOriginal428Names.WholeRows && equal == examined
            && whole.Values.All(value => value) && raw.Values.All(value => value));
    }
}
