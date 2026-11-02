using System.Text.Json;
using Erlang;
namespace Erlang.Tests;

internal static class ProcessAliveOriginal425Checks
{
    internal static async Task Run()
    {
        string output = Path.GetFullPath(Path.Combine(ProcessAliveOriginal425Names.Output,
            System.Guid.NewGuid().ToString(ProcessAliveOriginal425Names.Guid)));
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
                    {
                        RuntimeBootstrapProcessAlive425.Register(owner, registry);
                    }
                    {
                        RuntimeBootstrapAtomics.Register(owner, registry);
                    }
                });
                var declared = new BeamDependencySelection(new[] { ProcessAliveOriginal425Names.Runtime, ProcessAliveOriginal425Names.File, ProcessAliveOriginal425Names.Internal }.Concat(ProcessAliveOriginal425Names.Helpers));
                var helpers = ProcessAliveOriginal425Names.Helpers.Select(module => scope.Prepare(() => registry.PrepareBeam(AtomicsHost413Access.Resource(module + ".beam"), declared))).ToArray();
                PreparedBeamImage worker = scope.Prepare(() => registry.PrepareBeam(ProcessAliveOriginal425Access.Resource(ProcessAliveOriginal425Names.WorkerBeam), declared));

                return _ =>
                {
                    ProcessAliveOriginal425Access.Require(registry.PublishPreparedBeam(helpers.Append(worker).ToArray()).Status == BeamPublicationStatus.Published);
                    foreach (var image in helpers.Append(worker))
                        foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                            ProcessAliveOriginal425Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
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
                        ProcessAliveOriginal425Names.Module, function));
                    BitString wire = (BitString)await caller.InvokeAsync(async context => await owner.Modules.Call(
                        context,
                        ProcessAliveOriginal425Names.Runtime,
                        ProcessAliveOriginal425Names.Encode,
                        value
                    ));
                    captured.Add(file, (value, wire.ToArray()));
                    File.WriteAllBytes(Path.Combine(output, ProcessAliveOriginal425Names.Actual + file), wire.ToArray());
                    File.WriteAllText(
                        Path.Combine(output, ProcessAliveOriginal425Names.Actual + file + ProcessAliveOriginal425Names.Json),
                        JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(value))
                    );
                }
                catch (Exception error)
                {
                    errors.Add(new { Route = file, Failure = error.ToString() });
                }
            }
            await Capture(ProcessAliveOriginal425Names.Run, ProcessAliveOriginal425Names.Values);
            await Capture(ProcessAliveOriginal425Names.Stacks, ProcessAliveOriginal425Names.StackValues);
            await Capture(ProcessAliveOriginal425Names.Own, ProcessAliveOriginal425Names.OwnValues);
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
        foreach (string file in new[] { ProcessAliveOriginal425Names.Values, ProcessAliveOriginal425Names.StackValues })
        {
            byte[] bytes = ProcessAliveOriginal425Access.Resource(file);
            Term expected = OwnedFunctionOriginal407Terms.Decode(bytes);
            File.WriteAllBytes(Path.Combine(output, ProcessAliveOriginal425Names.Expected + file), bytes);
            File.WriteAllText(
                Path.Combine(output, ProcessAliveOriginal425Names.Expected + file + ProcessAliveOriginal425Names.Json),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expected))
            );
            bool present = captured.TryGetValue(file, out var actual);
            whole[file] = present && expected.Equals(actual.Term);
            raw[file] = present && bytes.AsSpan().SequenceEqual(actual.Bytes);
            if (!present) continue;
            equal += file == ProcessAliveOriginal425Names.Values
                ? IoPrintableOriginal404Comparison.Rows(
                    expected,
                    actual.Term,
                    file,
                    ProcessAliveOriginal425Names.Rows,
                    differences,
                    ref examined
                )
                : IoPrintableOriginal404Comparison.Profiles(
                    expected,
                    actual.Term,
                    ProcessAliveOriginal425Names.Rows,
                    differences,
                    ref examined
                );
        }
        bool ownState = false;
        try
        {
            byte[] expectedOwn = ProcessAliveOriginal425Access.Resource(ProcessAliveOriginal425Names.OwnValues);
            File.WriteAllBytes(Path.Combine(output, ProcessAliveOriginal425Names.Expected + ProcessAliveOriginal425Names.OwnValues), expectedOwn);
            Term sourceOwn = OwnedFunctionOriginal407Terms.Decode(expectedOwn);
            File.WriteAllText(
                Path.Combine(output, ProcessAliveOriginal425Names.Expected + ProcessAliveOriginal425Names.OwnValues + ProcessAliveOriginal425Names.Json),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(sourceOwn))
            );
            ProcessAliveOriginal425OwnState.Check(sourceOwn);
            ProcessAliveOriginal425OwnState.Check(captured[ProcessAliveOriginal425Names.OwnValues].Term);
            ownState = true;
        }
        catch (Exception error)
        {
            errors.Add(new { OwnStateProtocol = error.ToString() });
        }
        File.WriteAllText(
            Path.Combine(output, ProcessAliveOriginal425Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = ProcessAliveOriginal425Names.WholeRows,
                Examined = examined,
                Equal = equal,
                WholeTerms = whole,
                WholeRawEtf = raw,
                Errors = errors,
                Differences = differences,
                OrderedProcessSignals = true,
                OwnStateProtocol = ownState,
                OwnStateRows = 4,
                RawOwnStateRetained = true,
                OwnStateRawCrossVmEquality = false,
                WholeOriginalInternalImageClaim = false,
                PhysicalNumericEqualityClaim = false,
                ProfileArity = 3,
                Projection = false,
                ApiOrFullContractPromotion = false
            })
        );
        ProcessAliveOriginal425Access.Require(ownState && errors.Count == 0 && differences.Count == 0
            && examined == ProcessAliveOriginal425Names.WholeRows && equal == examined
            && whole.Values.All(value => value) && raw.Values.All(value => value));
    }
}
