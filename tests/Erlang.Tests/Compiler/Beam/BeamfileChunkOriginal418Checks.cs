using System.Text.Json;
using Erlang;
namespace Erlang.Tests;

internal static class BeamfileChunkOriginal418Checks
{
    internal static async Task Run()
    {
        string output = Path.GetFullPath(Path.Combine(BeamfileChunkOriginal418Names.Output,
            System.Guid.NewGuid().ToString(BeamfileChunkOriginal418Names.Guid)));
        Directory.CreateDirectory(output);
        var captured = new Dictionary<string, (Term Term, byte[] Bytes)>();
        var errors = new List<object>();
        ProcessRuntime? runtime = null;
        RuntimeNodeFoundationHostActor? actor = null;
        try
        {
            runtime = new ProcessRuntime();
            ProcessRuntime owner = runtime;
            await owner.InitializeModulesAsync(registry =>
            {
                var declared = new BeamDependencySelection([BeamfileChunkOriginal418Names.Internal,
                    BeamfileChunkOriginal418Names.Runtime, BeamfileChunkOriginal418Names.Subject, BeamfileChunkOriginal418Names.Module]);
                PreparedBeamImage facet = BeamfileChunkOriginal418Preparation.Prepare(registry, declared);
                PreparedBeamImage worker = registry.PrepareBeam(
                    BeamfileChunkOriginal418Access.Resource(BeamfileChunkOriginal418Names.WorkerBeam), declared);
                PreparedBeamImage observer = registry.PrepareBeam(
                    BeamfileChunkOriginal418Access.Resource(BeamfileChunkOriginal418Names.ObserverBeam), declared);
                BeamfileChunkOriginal418Access.Require(registry.PublishPreparedBeam([facet, worker, observer]).Status == BeamPublicationStatus.Published);
                BeamfileChunkOriginal418Preparation.AssertPublished(registry, facet);
                foreach (var image in new[] { worker, observer })
                    foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                        BeamfileChunkOriginal418Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
            });
            actor = new RuntimeNodeFoundationHostActor(owner);
            RuntimeNodeFoundationHostActor caller = actor;

            async Task Capture(string function, string file)
            {
                try
                {
                    Term value = await caller.InvokeAsync(async context => await owner.Modules.Call(context,
                        BeamfileChunkOriginal418Names.Module, function));
                    BitString wire = (BitString)await caller.InvokeAsync(async context => await owner.Modules.Call(
                        context,
                        BeamfileChunkOriginal418Names.Runtime,
                        BeamfileChunkOriginal418Names.Encode,
                        value
                    ));
                    captured.Add(file, (value, wire.ToArray()));
                    File.WriteAllBytes(Path.Combine(output, BeamfileChunkOriginal418Names.Actual + file), wire.ToArray());
                    File.WriteAllText(
                        Path.Combine(output, BeamfileChunkOriginal418Names.Actual + file + BeamfileChunkOriginal418Names.Json),
                        JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(value))
                    );
                }
                catch (Exception error)
                {
                    errors.Add(new { Route = file, Failure = error.ToString() });
                }
            }
            await Capture(BeamfileChunkOriginal418Names.Run, BeamfileChunkOriginal418Names.Values);
            await Capture(BeamfileChunkOriginal418Names.Stacks, BeamfileChunkOriginal418Names.StackValues);
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
        foreach (string file in new[] { BeamfileChunkOriginal418Names.Values, BeamfileChunkOriginal418Names.StackValues })
        {
            byte[] bytes = BeamfileChunkOriginal418Access.Resource(file);
            Term expected = OwnedFunctionOriginal407Terms.Decode(bytes);
            File.WriteAllBytes(Path.Combine(output, BeamfileChunkOriginal418Names.Expected + file), bytes);
            File.WriteAllText(
                Path.Combine(output, BeamfileChunkOriginal418Names.Expected + file + BeamfileChunkOriginal418Names.Json),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expected))
            );
            bool present = captured.TryGetValue(file, out var actual);
            whole[file] = present && expected.Equals(actual.Term);
            raw[file] = present && bytes.AsSpan().SequenceEqual(actual.Bytes);
            if (!present) continue;
            equal += file == BeamfileChunkOriginal418Names.Values
                ? IoPrintableOriginal404Comparison.Rows(
                    expected,
                    actual.Term,
                    file,
                    BeamfileChunkOriginal418Names.Rows,
                    differences,
                    ref examined
                )
                : IoPrintableOriginal404Comparison.Profiles(
                    expected,
                    actual.Term,
                    BeamfileChunkOriginal418Names.Rows,
                    differences,
                    ref examined
                );
        }
        File.WriteAllText(
            Path.Combine(output, BeamfileChunkOriginal418Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = BeamfileChunkOriginal418Names.WholeRows,
                Examined = examined,
                Equal = equal,
                WholeTerms = whole,
                WholeRawEtf = raw,
                Errors = errors,
                Differences = differences,
                SourceDerivedOneNativeFacet = true,
                WholeOriginalInternal71Claim = false,
                ProfileArity = 3,
                Projection = false,
                ApiOrFullContractPromotion = false
            })
        );
        BeamfileChunkOriginal418Access.Require(errors.Count == 0 && differences.Count == 0
            && examined == BeamfileChunkOriginal418Names.WholeRows && equal == examined
            && whole.Values.All(value => value) && raw.Values.All(value => value));
    }
}
