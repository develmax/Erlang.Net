using System.Text.Json;

namespace Erlang.Tests;

internal static class HamtOriginal430Checks
{
    internal static async Task Run()
    {
        string output = Path.GetFullPath(Path.Combine(HamtOriginal430Names.Output, Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(output);
        var captured = new Dictionary<string, (Term Term, byte[] Bytes)>();
        var errors = new List<object>();
        ProcessRuntime? runtime = null;
        RuntimeNodeFoundationHostActor? actor = null;
        try
        {
            runtime = await HamtOriginal430Access.Create();
            actor = new RuntimeNodeFoundationHostActor(runtime);
            foreach (var route in new[] { (Function: "run", File: "values.etf"), (Function: "run_stacks", File: "stacks.etf"), (Function: "own", File: "own.etf") })
            {
                try
                {
                    Term term = await actor.InvokeAsync(async context => await context.Runtime.Modules.Call(context, HamtOriginal430Names.Module, route.Function));
                    BitString encoded = (BitString)await actor.InvokeAsync(async context => await context.Runtime.Modules.Call(
                        context,
                        "erlang",
                        "term_to_binary",
                        term
                    ));
                    byte[] raw = encoded.ToArray();
                    captured.Add(route.File, (term, raw));
                    File.WriteAllBytes(Path.Combine(output, "actual-" + route.File), raw);
                    File.WriteAllText(Path.Combine(output, "actual-" + route.File + ".json"), JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(term)));
                }
                catch (Exception error)
                {
                    errors.Add(new { Route = route.File, Failure = error.ToString() });
                }
            }
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
        // Read expected bytes only after all routes and actual owner cleanup.
        bool original = false, ownEqual = false;
        var differences = new List<object>();
        var whole = new Dictionary<string, bool>();
        var rawEqual = new Dictionary<string, bool>();
        int examined = 0, equal = 0, unexpected = 0;
        try
        {
            using JsonDocument receipt = JsonDocument.Parse(HamtOriginal430Access.Resource("original-completion.json"));
            var proof = receipt.RootElement;
            original = proof.GetProperty("Passed").GetBoolean() && proof.GetProperty("OriginalWord64ProfilePassed").GetBoolean()
                && proof.GetProperty("WholeRows").GetInt32() == HamtOriginal430Names.Whole
                && proof.GetProperty("NativeExceptionsCaptured").GetInt32() == 36 && !proof.GetProperty("ExpectedRewritten").GetBoolean();
            foreach (string file in new[] { "values.etf", "stacks.etf", "own.etf" })
            {
                byte[] raw = HamtOriginal430Access.Resource(file);
                Term expected = OwnedFunctionOriginal407Terms.Decode(raw);
                File.WriteAllBytes(Path.Combine(output, "expected-" + file), raw);
                File.WriteAllText(Path.Combine(output, "expected-" + file + ".json"), JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expected)));
                if (!captured.TryGetValue(file, out var actual))
                {
                    errors.Add(new { MissingRoute = file });
                    continue;
                }
                whole[file] = expected.Equals(actual.Term);
                rawEqual[file] = raw.AsSpan().SequenceEqual(actual.Bytes);
                if (file == "own.etf")
                {
                    ownEqual = whole[file] && rawEqual[file];
                    continue;
                }
                if (file == "values.etf") equal += HamtOriginal430Comparison.Rows(
                    expected,
                    actual.Term,
                    file,
                    differences,
                    ref examined,
                    ref unexpected
                );
                else
                {
                    Term[] left = Cons.Items(expected).ToArray(), right = Cons.Items(actual.Term).ToArray();
                    HamtNative430Access.Require(left.Length == 5 && right.Length == left.Length);
                    for (int index = 0; index < left.Length; index++)
                    {
                        var l = (TupleTerm)left[index];
                        var a = (TupleTerm)right[index];
                        HamtNative430Access.Require(l.Items.Count == 3 && a.Items.Count == 3 && l.Items[0].Equals(a.Items[0]) && l.Items[1].Equals(a.Items[1]));
                        equal += HamtOriginal430Comparison.Rows(
                            l.Items[2],
                            a.Items[2],
                            new { File = file, Profile = index },
                            differences,
                            ref examined,
                            ref unexpected
                        );
                    }
                }
            }
        }
        catch (Exception error)
        {
            errors.Add(new { Comparison = error.ToString() });
        }
        File.WriteAllText(
            Path.Combine(output, HamtOriginal430Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = HamtOriginal430Names.Whole,
                Examined = examined,
                Equal = equal,
                Unexpected = unexpected,
                OriginalAccepted = original,
                OwnState4WholeRawAndTermEqual = ownEqual,
                WholeTerms = whole,
                WholeRawEtf = rawEqual,
                ExactCompatible = original && errors.Count == 0 && unexpected == 0 && equal == examined && whole.Count == 3 && whole.Values.All(value => value) && rawEqual.Values.All(value => value),
                PhysicalDifferencesHeld = differences.Count > 0,
                Differences = differences,
                Errors = errors,
                SourceProtocol = "Actual own64 word/view/immutable HAMT storage; not physical CLR or raw ERTS storage identity",
                ProjectedFields = false,
                GoldenRewritten = false,
                FullInternal71Claim = false,
                FullDebug20Claim = false,
                ApiOrFullContractPromotion = false
            })
        );
        HamtNative430Access.Require(original && ownEqual && examined == HamtOriginal430Names.Whole && unexpected == 0 && errors.Count == 0);
    }
}
