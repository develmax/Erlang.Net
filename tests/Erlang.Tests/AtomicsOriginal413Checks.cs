using System.Text.Json;
using Erlang;
namespace Erlang.Tests;

internal static class AtomicsOriginal413Checks
{
    internal static async Task Run(string output)
    {
        Directory.CreateDirectory(output);
        var results = new Dictionary<string, (Term Value, byte[] Bytes)>();
        var errors = new List<object>();
        ProcessRuntime? runtime = null;
        AtomicsOriginal413Actor? actor = null;
        try
        {
            runtime = await AtomicsHost413Access.Create();
            var image = runtime.Modules.PrepareBeam(
                AtomicsHost413Access.Resource(AtomicsOriginal413Names.WorkerBeam),
                new BeamDependencySelection([AtomicsHost413Names.Atomics,AtomicsHost413Names.Runtime,AtomicsHost413Names.Internal,
                    AtomicsHost413Names.Lists,AtomicsHost413Names.Maps,AtomicsHost413Names.File])
            );
            AtomicsHost413Access.Require(runtime.Modules.PublishPreparedBeam([image]).Status == BeamPublicationStatus.Published);
            foreach (var export in runtime.Modules.Exports.Where(export => export.Module == AtomicsOriginal413Names.Worker))
                AtomicsHost413Access.Require(ReferenceEquals(runtime.Modules.SourceOwner(export.Module, export.Function, export.Arity), image));
            actor = new AtomicsOriginal413Actor(runtime);
            ProcessRuntime owner = runtime;
            AtomicsOriginal413Actor caller = actor;

            async Task Capture(string function, string file)
            {
                try
                {
                    Term value = await caller.InvokeAsync(async context => await owner.Modules.Call(context, AtomicsOriginal413Names.Worker, function));
                    BitString wire = (BitString)await caller.InvokeAsync(async context => await owner.Modules.Call(
                        context,
                        AtomicsHost413Names.Runtime,
                        AtomicsHost413Names.Encode,
                        value
                    ));
                    byte[] bytes = wire.ToArray();
                    results.Add(file, (value, bytes));
                    File.WriteAllBytes(Path.Combine(output, AtomicsOriginal413Names.Actual + file), bytes);
                    File.WriteAllText(
                        Path.Combine(output, AtomicsOriginal413Names.Actual + file + AtomicsOriginal413Names.Json),
                        JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(value))
                    );
                }
                catch (Exception error)
                {
                    errors.Add(new { Route = file, Failure = error.ToString() });
                }
            }
            await Capture(AtomicsOriginal413Names.Run, AtomicsOriginal413Names.Values);
            await Capture(AtomicsOriginal413Names.Stacks, AtomicsOriginal413Names.StackValues);
            await Capture(AtomicsOriginal413Names.ResourceStacks, AtomicsOriginal413Names.ResourceValues);
        }
        catch (Exception error)
        {
            errors.Add(new { Preparation = error.ToString() });
        }
        finally
        {
            if (actor is not null) try
                {
                    await actor.DisposeAsync();
                }
                catch (Exception error)
                {
                    errors.Add(new { ActorCleanup = error.ToString() });
                }
            if (runtime is not null) try
                {
                    await runtime.DisposeAsync();
                }
                catch (Exception error)
                {
                    errors.Add(new { RuntimeCleanup = error.ToString() });
                }
        }
        // No expected term enters the VM. Decode only after independent routes AND native-storage close have settled.
        var differences = new List<object>();
        var whole = new Dictionary<string, bool>();
        var raw = new Dictionary<string, bool>();
        int examined = 0, equal = 0, retainedResourceRows = 0;
        foreach (string file in new[] { AtomicsOriginal413Names.Values, AtomicsOriginal413Names.StackValues, AtomicsOriginal413Names.ResourceValues })
        {
            byte[] bytes = AtomicsHost413Access.Resource(file);
            Term expected = OwnedFunctionOriginal407Terms.Decode(bytes);
            File.WriteAllBytes(Path.Combine(output, AtomicsOriginal413Names.Expected + file), bytes);
            File.WriteAllText(
                Path.Combine(output, AtomicsOriginal413Names.Expected + file + AtomicsOriginal413Names.Json),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expected))
            );
            bool present = results.TryGetValue(file, out var actual);
            if (file == AtomicsOriginal413Names.ResourceValues)
            {
                // These full unmodified frames contain VM-local magic references. They remain independent raw evidence.
                if (present)
                {
                    Term[] profiles = IoPrintableOriginal404Terms.List(actual.Value, out bool proper);
                    AtomicsHost413Access.Require(proper && profiles.Length == 5);
                    foreach (Term profile in profiles)
                    {
                        AtomicsHost413Access.Require(profile is TupleTerm { Items.Count: 3 });
                        Term[] rows = IoPrintableOriginal404Terms.List(((TupleTerm)profile).Items[2], out bool properRows);
                        AtomicsHost413Access.Require(properRows && rows.Length == 9);
                        retainedResourceRows += rows.Length;
                    }
                }
                continue;
            }
            whole[file] = present && expected.Equals(actual.Value);
            raw[file] = present && bytes.AsSpan().SequenceEqual(actual.Bytes);
            if (!present) continue;
            equal += file == AtomicsOriginal413Names.Values
                ? IoPrintableOriginal404Comparison.Rows(
                    expected,
                    actual.Value,
                    file,
                    AtomicsOriginal413Names.Rows,
                    differences,
                    ref examined
                )
                : IoPrintableOriginal404Comparison.Profiles(
                    expected,
                    actual.Value,
                    AtomicsOriginal413Names.Rows,
                    differences,
                    ref examined
                );
        }
        RuntimeAtomicsReport413? closed = runtime?.AtomicsStorage413;
        bool closedStorage = closed is { Closed: true, LiveArrays: 0, OwnedNativeBytes: 0, ActiveInvocations: 0 } && closed.Created == closed.Freed;
        File.WriteAllText(
            Path.Combine(output, AtomicsOriginal413Names.Receipt),
            JsonSerializer.Serialize(new
            {
                ExpectedStable = AtomicsOriginal413Names.StableRows,
                Examined = examined,
                Equal = equal,
                WholeStableTerms = whole,
                WholeStableRawEtf = raw,
                Errors = errors,
                Differences = differences,
                ExpectedResourceRows = AtomicsOriginal413Names.ResourceRows,
                RetainedResourceRows = retainedResourceRows,
                ResourceStackCrossVmEqualityClaim = false,
                CompleteResourceRawFramesRetained = true,
                StableSourceOwnedResourceIdentityAssertionsIncluded = true,
                ClosedStorage = closedStorage,
                StorageReport = closed,
                DefaultApiOrFullContractPromotion = false
            })
        );
        AtomicsHost413Access.Require(errors.Count == 0 && differences.Count == 0
            && examined == AtomicsOriginal413Names.StableRows && equal == examined
            && whole.Values.All(value => value) && raw.Values.All(value => value)
            && retainedResourceRows == AtomicsOriginal413Names.ResourceRows && closedStorage);
    }
}
