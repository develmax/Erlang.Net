using System.Text.Json;
using Erlang;
namespace Erlang.Tests;

internal static class SystemTasksOriginal428Checks
{
    internal static async Task Run()
    {
        string output = Path.GetFullPath(Path.Combine(SystemTasksOriginal428Names.Output,
            System.Guid.NewGuid().ToString(SystemTasksOriginal428Names.Guid)));
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
                    RuntimeBootstrapInternalSystemTasks428.Register(owner, registry);
                });
                var declared = new BeamDependencySelection([SystemTasksOriginal428Names.Runtime, SystemTasksOriginal428Names.File, SystemTasksOriginal428Names.Internal]);
                PreparedBeamImage facet = scope.Prepare(() => registry.PrepareBeam(SystemTasksOriginal428Access.Resource(SystemTasksOriginal428Names.FacetBeam), declared));
                SystemTasksOriginal428Access.Require(facet.Name == SystemTasksOriginal428Names.Internal
                    && facet.NativePrimitiveExports.Count == SystemTasksOriginal428Names.NativeCount
                    && SystemTasksOriginal428Names.NativeSignatures.All(signature => facet.NativePrimitiveExports.Contains(signature)));
                PreparedBeamImage handler = scope.Prepare(() => registry.PrepareBeam(SystemTasksOriginal428Access.Resource(SystemTasksOriginal428Names.HandlerBeam), declared));
                SystemTasksOriginal428Access.Require(handler.Name == SystemTasksOriginal428Names.HandlerModule && handler.NativePrimitiveExports.Count == 0);
                PreparedBeamImage worker = scope.Prepare(() => registry.PrepareBeam(SystemTasksOriginal428Access.Resource(SystemTasksOriginal428Names.WorkerBeam), declared));

                return _ =>
                {
                    SystemTasksOriginal428Access.Require(registry.PublishPreparedBeam([facet, handler, worker]).Status == BeamPublicationStatus.Published);
                    foreach (var image in new[] { facet, handler, worker })
                        foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                            SystemTasksOriginal428Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
                    SystemTasksOriginal428Access.Require(registry.Exports.Count(export => export.Module == facet.Name) == SystemTasksOriginal428Names.FacetExports);
                };
            }
            );
            RuntimeBootstrapInternalSystemTasks428.StartOriginalHandlers(owner);
            actor = new RuntimeNodeFoundationHostActor(owner);
            RuntimeNodeFoundationHostActor caller = actor;

            async Task Capture(string function, string file)
            {
                try
                {
                    Term value = await caller.InvokeAsync(async context => await owner.Modules.Call(context,
                        SystemTasksOriginal428Names.Module, function));
                    BitString wire = (BitString)await caller.InvokeAsync(async context => await owner.Modules.Call(
                        context,
                        SystemTasksOriginal428Names.Runtime,
                        SystemTasksOriginal428Names.Encode,
                        value
                    ));
                    captured.Add(file, (value, wire.ToArray()));
                    File.WriteAllBytes(Path.Combine(output, SystemTasksOriginal428Names.Actual + file), wire.ToArray());
                    File.WriteAllText(
                        Path.Combine(output, SystemTasksOriginal428Names.Actual + file + SystemTasksOriginal428Names.Json),
                        JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(value))
                    );
                }
                catch (Exception error)
                {
                    errors.Add(new { Route = file, Failure = error.ToString() });
                }
            }
            await Capture(SystemTasksOriginal428Names.Run, SystemTasksOriginal428Names.Values);
            await Capture(SystemTasksOriginal428Names.Stacks, SystemTasksOriginal428Names.StackValues);
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
        foreach (string file in new[] { SystemTasksOriginal428Names.Values, SystemTasksOriginal428Names.StackValues })
        {
            byte[] bytes = SystemTasksOriginal428Access.Resource(file);
            Term expected = OwnedFunctionOriginal407Terms.Decode(bytes);
            File.WriteAllBytes(Path.Combine(output, SystemTasksOriginal428Names.Expected + file), bytes);
            File.WriteAllText(
                Path.Combine(output, SystemTasksOriginal428Names.Expected + file + SystemTasksOriginal428Names.Json),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expected))
            );
            bool present = captured.TryGetValue(file, out var actual);
            whole[file] = present && expected.Equals(actual.Term);
            raw[file] = present && bytes.AsSpan().SequenceEqual(actual.Bytes);
            if (!present) continue;
            equal += file == SystemTasksOriginal428Names.Values
                ? IoPrintableOriginal404Comparison.Rows(
                    expected,
                    actual.Term,
                    file,
                    SystemTasksOriginal428Names.Rows,
                    differences,
                    ref examined
                )
                : IoPrintableOriginal404Comparison.Profiles(
                    expected,
                    actual.Term,
                    SystemTasksOriginal428Names.Rows,
                    differences,
                    ref examined
                );
        }
        File.WriteAllText(
            Path.Combine(output, SystemTasksOriginal428Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = SystemTasksOriginal428Names.WholeRows,
                Examined = examined,
                Equal = equal,
                WholeTerms = whole,
                WholeRawEtf = raw,
                Errors = errors,
                Differences = differences,
                OriginalCompiledWorkerUnchanged = true,
                SourceDerivedFiveStubFacet = true,
                ActualSourceOwnershipChecked = true,
                RealSourceHandlersAndSystemTaskBackend = true,
                RoleActorPidRawEqualityClaim = false,
                WholeOriginalInternalImageClaim = false,
                PhysicalNumericEqualityClaim = false,
                ProfileArity = 3,
                Projection = false,
                ApiOrFullContractPromotion = false
            })
        );
        SystemTasksOriginal428Access.Require(errors.Count == 0 && differences.Count == 0
            && examined == SystemTasksOriginal428Names.WholeRows && equal == examined
            && whole.Values.All(value => value) && raw.Values.All(value => value));
    }
}
