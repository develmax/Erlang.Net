using System.Text.Json;
namespace Erlang.Tests;

internal static class DebugDispatchOriginal432Checks
{
    private static async Task<Term> SourceChild(
        ProcessRuntime runtime,
        RuntimeNodeFoundationHostActor actor,
        string function,
        string output
    )
    {
        ReferenceTerm reference = runtime.MakeReference();
        TupleTerm started = (TupleTerm)await actor.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            DebugDispatchOriginal432Names.Module,
            DebugDispatchOriginal432Names.StartRoute,
            context.Self,
            reference,
            Term.A(function)
        ));
        var child = (Pid)started.Items[0];
        var monitor = (ReferenceTerm)started.Items[1];
        bool settled = false;
        try
        {
            TupleTerm envelope = await actor.InvokeAsync(async context => await context.ReceiveAsync<TupleTerm>(
                message =>
                message is TupleTerm tuple
                    && (tuple.Items.Count == 2 && tuple.Items[0].Equals(reference)
                        || tuple.Items.Count == 5 && tuple.Items[0].Equals(Term.A(DebugDispatchOriginal432Names.Down))
                            && tuple.Items[1].Equals(monitor) && tuple.Items[3].Equals(child)) ? tuple : null,
                DebugDispatchOriginal432Names.Deadline
            )
                ?? throw new TimeoutException(DebugDispatch432Names.Failure));
            if (envelope.Items.Count == 5)
            {
                settled = true;
                File.WriteAllText(
                    Path.Combine(output, function + DebugDispatchOriginal432Names.UnexpectedDownSuffix),
                    JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(envelope))
                );

                throw new ErlangException(envelope.Items[4]);
            }
            TupleTerm down = await actor.InvokeAsync(async context => await context.ReceiveAsync<TupleTerm>(
                message =>
                message is TupleTerm { Items.Count: 5 } tuple && tuple.Items[0].Equals(Term.A(DebugDispatchOriginal432Names.Down))
                    && tuple.Items[1].Equals(monitor) && tuple.Items[3].Equals(child) ? tuple : null,
                DebugDispatchOriginal432Names.Deadline
            )
                ?? throw new TimeoutException(DebugDispatch432Names.Failure));
            settled = true;
            DebugDispatchOriginal432Access.DebugDispatch432Require(down.Items[4].Equals(Term.A(DebugDispatchOriginal432Names.Normal)));

            return envelope.Items[1];
        }
        finally
        {
            if (!settled)
            {
                await actor.InvokeAsync(async context => await runtime.Modules.Call(
                    context,
                    DebugDispatchOriginal432Names.Erlang,
                    DebugDispatchOriginal432Names.Exit,
                    child,
                    Term.A(DebugDispatchOriginal432Names.Kill)
                ));
                await actor.InvokeAsync(async context => await context.ReceiveAsync<TupleTerm>(
                    message => message is TupleTerm { Items.Count: 5 } tuple
                    && tuple.Items[0].Equals(Term.A(DebugDispatchOriginal432Names.Down)) && tuple.Items[1].Equals(monitor) ? tuple : null,
                    DebugDispatchOriginal432Names.Deadline
                ) ?? throw new TimeoutException(DebugDispatch432Names.Failure));
            }
        }
    }

    internal static async Task Run()
    {
        string output = Path.GetFullPath(Path.Combine(DebugDispatchOriginal432Names.Output, Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(output);
        var captured = new Dictionary<string, (Term Term, byte[] Raw)>();
        var errors = new List<object>();
        ProcessRuntime? runtime = null;
        RuntimeNodeFoundationHostActor? actor = null;
        try
        {
            runtime = await DebugDispatchOriginal432Access.Create();
            actor = new RuntimeNodeFoundationHostActor(runtime);
            foreach (var route in new[]{(Function:DebugDispatchOriginal432Names.Run,File:DebugDispatchOriginal432Names.Values),
                (Function:DebugDispatchOriginal432Names.Stacks,File:DebugDispatchOriginal432Names.StackValues),
                (Function:DebugDispatch432Names.Instructions,File:"instructions.etf")})
            {
                try
                {
                    Term term = route.File == "instructions.etf"
                        ? await actor.InvokeAsync(async context => await context.Runtime.Modules.Call(context, DebugDispatch432Names.Debug, route.Function))
                        : await SourceChild(
                            runtime,
                            actor,
                            route.Function,
                            output
                        );
                    BitString wire = (BitString)await actor.InvokeAsync(async context => await context.Runtime.Modules.Call(
                        context,
                        "erlang",
                        "term_to_binary",
                        term
                    ));
                    byte[] raw = wire.ToArray();
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
        bool original = false;
        int rows = 0;
        var typed = new Dictionary<string, bool>();
        var rawEqual = new Dictionary<string, bool>();
        try
        {
            using JsonDocument document = JsonDocument.Parse(DebugDispatchOriginal432Access.Resource("original-completion.json"));
            var proof = document.RootElement;
            original = proof.GetProperty("Passed").GetBoolean() && proof.GetProperty("WholeRows").GetInt32() == 96 && proof.GetProperty("NativeExceptionsCaptured").GetInt32() == 54
                && proof.GetProperty("OriginalWord64ProfilePassed").GetBoolean() && proof.GetProperty("MatchedExternalSourceChildMfa").GetBoolean() && !proof.GetProperty("ExpectedRewritten").GetBoolean();
            foreach (string file in new[] { DebugDispatchOriginal432Names.Values, DebugDispatchOriginal432Names.StackValues, "instructions.etf" })
            {
                byte[] bytes = DebugDispatchOriginal432Access.Resource(file);
                Term expected = OwnedFunctionOriginal407Terms.Decode(bytes);
                File.WriteAllBytes(Path.Combine(output, "expected-" + file), bytes);
                File.WriteAllText(Path.Combine(output, "expected-" + file + ".json"), JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expected)));
                if (!captured.TryGetValue(file, out var actual)) continue;
                typed[file] = expected.Equals(actual.Term);
                rawEqual[file] = bytes.AsSpan().SequenceEqual(actual.Raw);
                if (file == DebugDispatchOriginal432Names.Values) rows += Cons.Items(expected).Count();
                else if (file == DebugDispatchOriginal432Names.StackValues) rows += Cons.Items(expected).Sum(profile => Cons.Items(((TupleTerm)profile).Items[2]).Count());
                else DebugDispatchOriginal432Access.DebugDispatch432Require(Cons.Items(actual.Term).Any());
            }
        }
        catch (Exception error)
        {
            errors.Add(new { Comparison = error.ToString() });
        }
        bool strict = original && rows == 96 && errors.Count == 0 && new[] { DebugDispatchOriginal432Names.Values, DebugDispatchOriginal432Names.StackValues }.All(file => typed.GetValueOrDefault(file) && rawEqual.GetValueOrDefault(file));
        File.WriteAllText(
            Path.Combine(output, "comparison.json"),
            JsonSerializer.Serialize(new
            {
                OriginalAccepted = original,
                WholeBreakpointRows = rows,
                StrictBreakpointWholeRawAndTyped = strict,
                WholeTyped = typed,
                WholeRaw = rawEqual,
                Errors = errors,
                InstructionsSourceProtocol = "Actual selected managed execution delegate versus actual native specific opcode handlers; raw differences retained, no native address equivalence",
                BreakpointTrapProfile = "Actual managed pause/resume controller; original native OS break was not invoked",
                FullDebug20Compatible = false,
                ProjectedFields = false,
                GoldenRewritten = false
            })
        );
        DebugDispatchOriginal432Access.DebugDispatch432Require(strict);
    }
}
