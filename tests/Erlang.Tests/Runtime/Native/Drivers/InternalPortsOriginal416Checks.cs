using System.Text.Json;
using Erlang;
namespace Erlang.Tests;

internal static class InternalPortsOriginal416Checks
{
    internal static async Task Run()
    {
        string output = Path.GetFullPath(Path.Combine(InternalPortsOriginal416Names.Output,
            System.Guid.NewGuid().ToString(InternalPortsOriginal416Names.Guid)));
        Directory.CreateDirectory(output);
        var captured = new Dictionary<string, (Term Term, byte[] Bytes)>();
        var errors = new List<object>();
        ProcessRuntime? runtime = null;
        RuntimeNodeFoundationHostActor? actor = null;
        try
        {
            runtime = new ProcessRuntime();
            ProcessRuntime owner = runtime;
            await InternalPortsOriginal416Preparation.Prepare171Async(owner);
            actor = new RuntimeNodeFoundationHostActor(owner);
            RuntimeNodeFoundationHostActor caller = actor;

            async Task Capture(string function, string file)
            {
                try
                {
                    Term value = await caller.InvokeAsync(async context => await owner.Modules.Call(context,
                        InternalPortsOriginal416Names.Module, function));
                    BitString wire = (BitString)await caller.InvokeAsync(async context => await owner.Modules.Call(
                        context,
                        InternalPortsOriginal416Names.Runtime,
                        InternalPortsOriginal416Names.Encode,
                        value
                    ));
                    captured.Add(file, (value, wire.ToArray()));
                    File.WriteAllBytes(Path.Combine(output, InternalPortsOriginal416Names.Actual + file), wire.ToArray());
                    File.WriteAllText(
                        Path.Combine(output, InternalPortsOriginal416Names.Actual + file + InternalPortsOriginal416Names.Json),
                        JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(value))
                    );
                }
                catch (Exception error)
                {
                    errors.Add(new { Route = file, Failure = error.ToString() });
                }
            }
            await Capture(InternalPortsOriginal416Names.Run, InternalPortsOriginal416Names.Values);
            await Capture(InternalPortsOriginal416Names.Stacks, InternalPortsOriginal416Names.StackValues);
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
        foreach (string file in new[] { InternalPortsOriginal416Names.Values, InternalPortsOriginal416Names.StackValues })
        {
            byte[] bytes = InternalPortsOriginal416Access.Resource(file);
            Term expected = OwnedFunctionOriginal407Terms.Decode(bytes);
            File.WriteAllBytes(Path.Combine(output, InternalPortsOriginal416Names.Expected + file), bytes);
            File.WriteAllText(
                Path.Combine(output, InternalPortsOriginal416Names.Expected + file + InternalPortsOriginal416Names.Json),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expected))
            );
            bool present = captured.TryGetValue(file, out var actual);
            whole[file] = present && expected.Equals(actual.Term);
            raw[file] = present && bytes.AsSpan().SequenceEqual(actual.Bytes);
            if (!present) continue;
            equal += file == InternalPortsOriginal416Names.Values
                ? IoPrintableOriginal404Comparison.Rows(
                    expected,
                    actual.Term,
                    file,
                    InternalPortsOriginal416Names.Rows,
                    differences,
                    ref examined
                )
                : InternalPortsOriginal416Comparison.Profiles(
                    expected,
                    actual.Term,
                    differences,
                    ref examined
                );
        }
        File.WriteAllText(
            Path.Combine(output, InternalPortsOriginal416Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = InternalPortsOriginal416Names.WholeRows,
                Examined = examined,
                Equal = equal,
                WholeTerms = whole,
                WholeRawEtf = raw,
                Errors = errors,
                Differences = differences,
                DerivedNativePortFacet = true,
                OrdinaryActorOnly = true,
                WholeOriginalInternal71Claim = false,
                ProfileArity = 2,
                ProfileSourceShapePreserved = true,
                Projection = false,
                ApiOrFullContractPromotion = false
            })
        );
        InternalPortsOriginal416Access.Require(errors.Count == 0 && differences.Count == 0
            && examined == InternalPortsOriginal416Names.WholeRows && equal == examined
            && whole.Values.All(value => value) && raw.Values.All(value => value));
    }
}
