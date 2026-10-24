using System.Text.Json;
namespace Erlang.Tests;

internal static class ExportGlobalOriginal431Checks
{
    internal static async Task Run()
    {
        string output = Path.GetFullPath(Path.Combine(ExportGlobalOriginal431Names.Output, Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(output);
        var errors = new List<object>();
        Term? actual = null;
        byte[]? actualRaw = null;
        ProcessRuntime? runtime = null;
        RuntimeNodeFoundationHostActor? actor = null;
        try
        {
            runtime = await ExportGlobalOriginal431Access.Create();
            actor = new RuntimeNodeFoundationHostActor(runtime);
            actual = await actor.InvokeAsync(async context => await context.Runtime.Modules.Call(context, ExportGlobalOriginal431Names.Module, ExportGlobalOriginal431Names.Run));
            BitString wire = (BitString)await actor.InvokeAsync(async context => await context.Runtime.Modules.Call(
                context,
                ExportGlobalOriginal431Names.Runtime,
                ExportGlobalOriginal431Names.Encode,
                actual
            ));
            actualRaw = wire.ToArray();
            File.WriteAllBytes(Path.Combine(output, "actual-values.etf"), actualRaw);
            File.WriteAllText(Path.Combine(output, "actual-values.json"), JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(actual)));
        }
        catch (Exception error)
        {
            errors.Add(new { Execution = error.ToString() });
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
        bool original = false, typed = false, raw = false;
        int rows = 0;
        try
        {
            using JsonDocument proof = JsonDocument.Parse(ExportGlobalOriginal431Access.Resource(ExportGlobalOriginal431Names.Proof));
            var root = proof.RootElement;
            original = root.GetProperty("Passed").GetBoolean() && root.GetProperty("WholeRows").GetInt32() == 3
                            && root.GetProperty("OriginalWord64ProfilePassed").GetBoolean() && !root.GetProperty("ExpectedRewritten").GetBoolean()
                            && root.GetProperty("Stages").GetProperty("verify-dynamic-image").GetBoolean();
            byte[] expectedRaw = ExportGlobalOriginal431Access.Resource(ExportGlobalOriginal431Names.Values);
            Term expected = OwnedFunctionOriginal407Terms.Decode(expectedRaw);
            rows = Cons.Items(expected).Count();
            File.WriteAllBytes(Path.Combine(output, "expected-values.etf"), expectedRaw);
            File.WriteAllText(Path.Combine(output, "expected-values.json"), JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expected)));
            typed = actual is not null && expected.Equals(actual);
            raw = actualRaw is not null && expectedRaw.AsSpan().SequenceEqual(actualRaw);
        }
        catch (Exception error)
        {
            errors.Add(new { Comparison = error.ToString() });
        }
        File.WriteAllText(
            Path.Combine(output, "comparison.json"),
            JsonSerializer.Serialize(new { OriginalAccepted = original, WholeRows = rows, WholeTypedEqual = typed, WholeRawEqual = raw, Errors = errors, ProjectedFields = false, GoldenRewritten = false, MissingStubIncluded = true, ComparedAfterCleanup = true, FullDebug20Claim = false })
        );
        ExportGlobalOriginal431Access.Require(original && rows == 3 && typed && raw && errors.Count == 0);
    }
}
