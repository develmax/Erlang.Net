using System.Text.Json;
using Erlang;
namespace Erlang.Tests;

internal static class ApplyTailOriginal416Checks
{
    internal static async Task Run()
    {
        string output = Path.GetFullPath(Path.Combine(ApplyTailOriginal416Names.Output,
            System.Guid.NewGuid().ToString(ApplyTailOriginal416Names.Guid)));
        Directory.CreateDirectory(output);
        var captured = new Dictionary<string, (Term Term, byte[] Bytes)>();
        var errors = new List<object>();
        ProcessRuntime? runtime = null;
        RuntimeNodeFoundationHostActor? actor = null;
        try
        {
            runtime = new ProcessRuntime();
            ProcessRuntime owner = runtime;
            await global::Erlang.Tests.PreparedBeamFixtureScope171.RunAsync(
                owner,
                async (registry, scope) =>
            {
                await owner.InitializeModulesAsync(_ =>
                {
                    RuntimeBootstrapEtsTraversal416.Register(owner, registry);
                });
                var declared = new BeamDependencySelection([ApplyTailOriginal416Names.Ets,
                                ApplyTailOriginal416Names.Runtime, ApplyTailOriginal416Names.File]);
                PreparedBeamImage worker = scope.Prepare(() => registry.PrepareBeam(
                    ApplyTailOriginal416Access.Resource(ApplyTailOriginal416Names.WorkerBeam), declared));
                ApplyTailOriginal416Access.Require(worker.Name == ApplyTailOriginal416Names.Module
                    && worker.NativePrimitiveExports.Count == 0);

                return _ =>
                {
                    ApplyTailOriginal416Access.Require(registry.PublishPreparedBeam(worker).Status == BeamPublicationStatus.Published);
                    foreach (var export in registry.Exports.Where(export => export.Module == worker.Name))
                        ApplyTailOriginal416Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), worker));
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
                        ApplyTailOriginal416Names.Module, function));
                    BitString wire = (BitString)await caller.InvokeAsync(async context => await owner.Modules.Call(
                        context,
                        ApplyTailOriginal416Names.Runtime,
                        ApplyTailOriginal416Names.Encode,
                        value
                    ));
                    captured.Add(file, (value, wire.ToArray()));
                    File.WriteAllBytes(Path.Combine(output, ApplyTailOriginal416Names.Actual + file), wire.ToArray());
                    File.WriteAllText(
                        Path.Combine(output, ApplyTailOriginal416Names.Actual + file + ApplyTailOriginal416Names.Json),
                        JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(value))
                    );
                }
                catch (Exception error)
                {
                    errors.Add(new { Route = file, Failure = error.ToString() });
                }
            }
            await Capture(ApplyTailOriginal416Names.Run, ApplyTailOriginal416Names.Values);
            await Capture(ApplyTailOriginal416Names.Stacks, ApplyTailOriginal416Names.StackValues);
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
        foreach (string file in new[] { ApplyTailOriginal416Names.Values, ApplyTailOriginal416Names.StackValues })
        {
            byte[] bytes = ApplyTailOriginal416Access.Resource(file);
            Term expected = OwnedFunctionOriginal407Terms.Decode(bytes);
            File.WriteAllBytes(Path.Combine(output, ApplyTailOriginal416Names.Expected + file), bytes);
            File.WriteAllText(
                Path.Combine(output, ApplyTailOriginal416Names.Expected + file + ApplyTailOriginal416Names.Json),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expected))
            );
            bool present = captured.TryGetValue(file, out var actual);
            whole[file] = present && expected.Equals(actual.Term);
            raw[file] = present && bytes.AsSpan().SequenceEqual(actual.Bytes);
            if (!present) continue;
            equal += file == ApplyTailOriginal416Names.Values
                ? IoPrintableOriginal404Comparison.Rows(
                    expected,
                    actual.Term,
                    file,
                    ApplyTailOriginal416Names.Rows,
                    differences,
                    ref examined
                )
                : IoPrintableOriginal404Comparison.Profiles(
                    expected,
                    actual.Term,
                    ApplyTailOriginal416Names.Rows,
                    differences,
                    ref examined
                );
        }
        File.WriteAllText(
            Path.Combine(output, ApplyTailOriginal416Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = ApplyTailOriginal416Names.WholeRows,
                Examined = examined,
                Equal = equal,
                WholeTerms = whole,
                WholeRawEtf = raw,
                Errors = errors,
                Differences = differences,
                ActualDynamicApplyAndStaticNativeTail = true,
                CanonicalFiveOnly = true,
                ProfileArity = 3,
                Projection = false,
                ApiOrFullContractPromotion = false
            })
        );
        ApplyTailOriginal416Access.Require(errors.Count == 0 && differences.Count == 0
            && examined == ApplyTailOriginal416Names.WholeRows && equal == examined
            && whole.Values.All(value => value) && raw.Values.All(value => value));
    }
}
