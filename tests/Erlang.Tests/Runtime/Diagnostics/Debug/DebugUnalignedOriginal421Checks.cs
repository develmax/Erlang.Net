using System.Text.Json;
using Erlang;
namespace Erlang.Tests;

internal static class DebugUnalignedOriginal421Checks
{
    internal static async Task Run()
    {
        string output = Path.GetFullPath(Path.Combine(DebugUnalignedOriginal421Names.Output,
            System.Guid.NewGuid().ToString(DebugUnalignedOriginal421Names.Guid)));
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
                var declared = new BeamDependencySelection([DebugUnalignedOriginal421Names.Debug,
                    DebugUnalignedOriginal421Names.Runtime, DebugUnalignedOriginal421Names.Subject, DebugUnalignedOriginal421Names.Module]);
                PreparedBeamImage facet = DebugUnalignedOriginal421Preparation.Prepare(registry, declared);
                PreparedBeamImage worker = registry.PrepareBeam(
                    DebugUnalignedOriginal421Access.Resource(DebugUnalignedOriginal421Names.WorkerBeam), declared);
                PreparedBeamImage observer = registry.PrepareBeam(
                    DebugUnalignedOriginal421Access.Resource(DebugUnalignedOriginal421Names.ObserverBeam), declared);
                DebugUnalignedOriginal421Access.Require(registry.PublishPreparedBeam([facet, worker, observer]).Status == BeamPublicationStatus.Published);
                DebugUnalignedOriginal421Preparation.AssertPublished(registry, facet);
                foreach (var image in new[] { worker, observer })
                    foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                        DebugUnalignedOriginal421Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
            });
            actor = new RuntimeNodeFoundationHostActor(owner);
            RuntimeNodeFoundationHostActor caller = actor;

            async Task Capture(string function, string file)
            {
                try
                {
                    Term value = await caller.InvokeAsync(async context => await owner.Modules.Call(context,
                        DebugUnalignedOriginal421Names.Module, function));
                    BitString wire = (BitString)await caller.InvokeAsync(async context => await owner.Modules.Call(
                        context,
                        DebugUnalignedOriginal421Names.Runtime,
                        DebugUnalignedOriginal421Names.Encode,
                        value
                    ));
                    captured.Add(file, (value, wire.ToArray()));
                    File.WriteAllBytes(Path.Combine(output, DebugUnalignedOriginal421Names.Actual + file), wire.ToArray());
                    File.WriteAllText(
                        Path.Combine(output, DebugUnalignedOriginal421Names.Actual + file + DebugUnalignedOriginal421Names.Json),
                        JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(value))
                    );
                }
                catch (Exception error)
                {
                    errors.Add(new { Route = file, Failure = error.ToString() });
                }
            }
            await Capture(DebugUnalignedOriginal421Names.Run, DebugUnalignedOriginal421Names.Values);
            await Capture(DebugUnalignedOriginal421Names.Stacks, DebugUnalignedOriginal421Names.StackValues);
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
        foreach (string file in new[] { DebugUnalignedOriginal421Names.Values, DebugUnalignedOriginal421Names.StackValues })
        {
            byte[] bytes = DebugUnalignedOriginal421Access.Resource(file);
            Term expected = OwnedFunctionOriginal407Terms.Decode(bytes);
            File.WriteAllBytes(Path.Combine(output, DebugUnalignedOriginal421Names.Expected + file), bytes);
            File.WriteAllText(
                Path.Combine(output, DebugUnalignedOriginal421Names.Expected + file + DebugUnalignedOriginal421Names.Json),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expected))
            );
            bool present = captured.TryGetValue(file, out var actual);
            whole[file] = present && expected.Equals(actual.Term);
            raw[file] = present && bytes.AsSpan().SequenceEqual(actual.Bytes);
            if (!present) continue;
            equal += file == DebugUnalignedOriginal421Names.Values
                ? IoPrintableOriginal404Comparison.Rows(
                    expected,
                    actual.Term,
                    file,
                    DebugUnalignedOriginal421Names.Rows,
                    differences,
                    ref examined
                )
                : IoPrintableOriginal404Comparison.Profiles(
                    expected,
                    actual.Term,
                    DebugUnalignedOriginal421Names.Rows,
                    differences,
                    ref examined
                );
        }
        File.WriteAllText(
            Path.Combine(output, DebugUnalignedOriginal421Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = DebugUnalignedOriginal421Names.WholeRows,
                Examined = examined,
                Equal = equal,
                WholeTerms = whole,
                WholeRawEtf = raw,
                Errors = errors,
                Differences = differences,
                SourceDerivedOneNativeFacet = true,
                WholeOriginalDebug20Claim = false,
                ProfileArity = 3,
                Projection = false,
                ApiOrFullContractPromotion = false
            })
        );
        DebugUnalignedOriginal421Access.Require(errors.Count == 0 && differences.Count == 0
            && examined == DebugUnalignedOriginal421Names.WholeRows && equal == examined
            && whole.Values.All(value => value) && raw.Values.All(value => value));
    }
}
