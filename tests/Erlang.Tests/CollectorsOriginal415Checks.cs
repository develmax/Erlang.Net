using System.Text.Json;
using Erlang;
namespace Erlang.Tests;

internal static class CollectorsOriginal415Checks
{
    internal static async Task Run()
    {
        string output = Path.GetFullPath(Path.Combine(CollectorsOriginal415Names.Output,
            System.Guid.NewGuid().ToString(CollectorsOriginal415Names.Guid)));
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
                CollectorsOriginal415Preparation.Prepare(registry);
            });
            actor = new RuntimeNodeFoundationHostActor(owner);
            RuntimeNodeFoundationHostActor caller = actor;

            async Task Capture(string function, string file)
            {
                try
                {
                    Term value = await caller.InvokeAsync(async context => await owner.Modules.Call(context,
                        CollectorsOriginal415Names.Module, function));
                    BitString wire = (BitString)await caller.InvokeAsync(async context => await owner.Modules.Call(
                        context,
                        CollectorsOriginal415Names.Runtime,
                        CollectorsOriginal415Names.Encode,
                        value
                    ));
                    captured.Add(file, (value, wire.ToArray()));
                    File.WriteAllBytes(Path.Combine(output, CollectorsOriginal415Names.Actual + file), wire.ToArray());
                    File.WriteAllText(
                        Path.Combine(output, CollectorsOriginal415Names.Actual + file + CollectorsOriginal415Names.Json),
                        JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(value))
                    );
                }
                catch (Exception error)
                {
                    errors.Add(new { Route = file, Failure = error.ToString() });
                }
            }
            await Capture(CollectorsOriginal415Names.Run, CollectorsOriginal415Names.Values);
            await Capture(CollectorsOriginal415Names.Stacks, CollectorsOriginal415Names.StackValues);
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
        foreach (string file in new[] { CollectorsOriginal415Names.Values, CollectorsOriginal415Names.StackValues })
        {
            byte[] bytes = CollectorsOriginal415Access.Resource(file);
            Term expected = OwnedFunctionOriginal407Terms.Decode(bytes);
            File.WriteAllBytes(Path.Combine(output, CollectorsOriginal415Names.Expected + file), bytes);
            File.WriteAllText(
                Path.Combine(output, CollectorsOriginal415Names.Expected + file + CollectorsOriginal415Names.Json),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expected))
            );
            bool present = captured.TryGetValue(file, out var actual);
            whole[file] = present && expected.Equals(actual.Term);
            raw[file] = present && bytes.AsSpan().SequenceEqual(actual.Bytes);
            if (!present) continue;
            equal += file == CollectorsOriginal415Names.Values
                ? IoPrintableOriginal404Comparison.Rows(
                    expected,
                    actual.Term,
                    file,
                    CollectorsOriginal415Names.Rows,
                    differences,
                    ref examined
                )
                : CollectorsOriginal415Comparison.Profiles(
                    expected,
                    actual.Term,
                    differences,
                    ref examined
                );
        }
        File.WriteAllText(
            Path.Combine(output, CollectorsOriginal415Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = CollectorsOriginal415Names.WholeRows,
                Examined = examined,
                Equal = equal,
                WholeTerms = whole,
                WholeRawEtf = raw,
                Errors = errors,
                Differences = differences,
                OriginalCollectorImages = true,
                OrdinaryActorOnly = true,
                PrivilegedAckEqualityClaim = false,
                ProfileArity = 2,
                ProfileSourceShapePreserved = true,
                Projection = false,
                ApiOrFullContractPromotion = false
            })
        );
        CollectorsOriginal415Access.Require(errors.Count == 0 && differences.Count == 0
            && examined == CollectorsOriginal415Names.WholeRows && equal == examined
            && whole.Values.All(value => value) && raw.Values.All(value => value));
    }
}
