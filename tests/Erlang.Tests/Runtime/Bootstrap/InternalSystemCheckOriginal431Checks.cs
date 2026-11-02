using System.Text.Json;
using Erlang;
namespace Erlang.Tests;

internal static class InternalSystemCheckOriginal431Checks
{
    internal static async Task Run()
    {
        string output = Path.GetFullPath(Path.Combine(InternalSystemCheckOriginal431Names.Output,
            System.Guid.NewGuid().ToString(InternalSystemCheckOriginal431Names.Guid)));
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
                (registry, scope) =>
            {
                var declared = new BeamDependencySelection(new[] { InternalSystemCheckOriginal431Names.Runtime, InternalSystemCheckOriginal431Names.File, InternalSystemCheckOriginal431Names.Internal, InternalSystemCheckOriginal431Names.Module });
                PreparedBeamImage facet = scope.Prepare(() => registry.PrepareBeam(InternalSystemCheckOriginal431Access.Resource(InternalSystemCheckOriginal431Names.FacetBeam), declared));
                PreparedBeamImage worker = scope.Prepare(() => registry.PrepareBeam(InternalSystemCheckOriginal431Access.Resource(InternalSystemCheckOriginal431Names.WorkerBeam), declared));

                return ValueTask.FromResult<Action<ModuleRegistry>>(_ =>
                {
                    InternalSystemCheckOriginal431Access.Require(registry.PublishPreparedBeam(new[] { facet, worker }).Status == BeamPublicationStatus.Published);
                    InternalSystemCheckOriginal431Access.Require(facet.NativePrimitiveExports.Count == InternalSystemCheckOriginal431Names.NativeCount
                    && facet.NativePrimitiveExports.Contains(new BeamFunctionSignature(InternalSystemCheckOriginal431Names.NativeFunction, 1)));
                    InternalSystemCheckOriginal431Access.Require(registry.Exports.Count(export => export.Module == facet.Name) == InternalSystemCheckOriginal431Names.TotalExports);
                    foreach (var image in new[] { facet, worker })
                        foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                            InternalSystemCheckOriginal431Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
                });
            }
            );
            actor = new RuntimeNodeFoundationHostActor(owner);
            RuntimeNodeFoundationHostActor caller = actor;

            async Task Capture(string function, string file)
            {
                try
                {
                    Term value = await caller.InvokeAsync(async context => await owner.Modules.Call(context,
                        InternalSystemCheckOriginal431Names.Module, function));
                    BitString wire = (BitString)await caller.InvokeAsync(async context => await owner.Modules.Call(
                        context,
                        InternalSystemCheckOriginal431Names.Runtime,
                        InternalSystemCheckOriginal431Names.Encode,
                        value
                    ));
                    captured.Add(file, (value, wire.ToArray()));
                    File.WriteAllBytes(Path.Combine(output, InternalSystemCheckOriginal431Names.Actual + file), wire.ToArray());
                    File.WriteAllText(
                        Path.Combine(output, InternalSystemCheckOriginal431Names.Actual + file + InternalSystemCheckOriginal431Names.Json),
                        JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(value))
                    );
                }
                catch (Exception error)
                {
                    errors.Add(new { Route = file, Failure = error.ToString() });
                }
            }
            await Capture(InternalSystemCheckOriginal431Names.Run, InternalSystemCheckOriginal431Names.Values);
            await Capture(InternalSystemCheckOriginal431Names.Stacks, InternalSystemCheckOriginal431Names.StackValues);
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
        foreach (string file in new[] { InternalSystemCheckOriginal431Names.Values, InternalSystemCheckOriginal431Names.StackValues })
        {
            byte[] bytes = InternalSystemCheckOriginal431Access.Resource(file);
            Term expected = OwnedFunctionOriginal407Terms.Decode(bytes);
            File.WriteAllBytes(Path.Combine(output, InternalSystemCheckOriginal431Names.Expected + file), bytes);
            File.WriteAllText(
                Path.Combine(output, InternalSystemCheckOriginal431Names.Expected + file + InternalSystemCheckOriginal431Names.Json),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expected))
            );
            bool present = captured.TryGetValue(file, out var actual);
            whole[file] = present && expected.Equals(actual.Term);
            raw[file] = present && bytes.AsSpan().SequenceEqual(actual.Bytes);
            if (!present) continue;
            equal += file == InternalSystemCheckOriginal431Names.Values
                ? IoPrintableOriginal404Comparison.Rows(
                    expected,
                    actual.Term,
                    file,
                    InternalSystemCheckOriginal431Names.Rows,
                    differences,
                    ref examined
                )
                : IoPrintableOriginal404Comparison.Profiles(
                    expected,
                    actual.Term,
                    InternalSystemCheckOriginal431Names.Rows,
                    differences,
                    ref examined
                );
        }
        File.WriteAllText(
            Path.Combine(output, InternalSystemCheckOriginal431Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = InternalSystemCheckOriginal431Names.WholeRows,
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
        InternalSystemCheckOriginal431Access.Require(errors.Count == 0 && differences.Count == 0
            && examined == InternalSystemCheckOriginal431Names.WholeRows && equal == examined
            && whole.Values.All(value => value) && raw.Values.All(value => value));
    }
}
