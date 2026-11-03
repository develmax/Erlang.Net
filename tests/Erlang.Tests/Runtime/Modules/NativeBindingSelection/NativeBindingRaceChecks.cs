using System.Text.Json;

namespace Erlang.Tests;

internal static class NativeBindingRaceChecks
{
    internal static async Task Run(string expectation)
    {
        await using var runtime = new ProcessRuntime();
        var completed = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle actor = runtime.Spawn(async context =>
        {
            try
            {
                if (expectation == NativeBindingSelectionNames.Control)
                {
                    runtime.Modules.DeleteModule(NativeBindingSelectionNames.Lists);
                    runtime.Modules.Register(
                        NativeBindingSelectionNames.Lists,
                        NativeBindingSelectionNames.Reverse,
                        NativeBindingSelectionNames.ReverseArity,
                        (caller, _) => caller.Runtime.Modules.Call(
                            caller,
                            NativeBindingSelectionNames.Erlang,
                            NativeBindingSelectionNames.Error,
                            Term.A(NativeBindingSelectionNames.Replacement)
                        )
                    );
                    Type trace = typeof(OrdsetCoreModule).Assembly.GetType(NativeBindingSelectionNames.TraceType)!;
                    Type continuation = typeof(OrdsetCoreModule).Assembly.GetType(NativeBindingSelectionNames.ContinuationType)!;
                    Type state = typeof(OrdsetCoreModule).Assembly.GetType(NativeBindingSelectionNames.StateType)!;
                    try
                    {
                        await (ValueTask<Term>)trace.GetMethod(
                            NativeBindingSelectionNames.ReverseMethod,
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static
                        )!
                            .Invoke(
                                null,
                                [context, new Cons(Term.I(1), Term.A(NativeBindingSelectionNames.Tail)), Term.List(Term.I(2), Term.I(1)),
                                Array.CreateInstance(continuation, 0), Enum.Parse(state, NativeBindingSelectionNames.ReverseState), NativeBindingSelectionNames.HelperLine]
                            )!;
                    }
                    catch (ErlangException failure)
                    {
                        bool contains = Cons.Items(failure.StackTraceTerm).OfType<TupleTerm>()
                            .Any(frame => frame.Items.Count >= 2 && frame.Items[1].Equals(Term.A(NativeBindingSelectionNames.Helper)));
                        if (!failure.Reason.Equals(Term.A(NativeBindingSelectionNames.Replacement)) || contains)
                            throw new InvalidDataException(NativeBindingSelectionNames.Failure + failure.StackTraceTerm);
                        completed.TrySetResult(new { Expectation = expectation, Reason = failure.Reason.ToString(), NativeOnlyHelperPresent = contains, Stack = failure.StackTraceTerm.ToString() });
                    }

                    return Nil.Value;
                }
                using var observer = new NativeBindingFrameObserver(context);
                Term inner = new Cons(Term.I(2), new Cons(Term.I(1), Term.A(NativeBindingSelectionNames.Tail)));
                Term outer = new Cons(inner, new Cons(Term.List(Term.I(1)), Term.A(NativeBindingSelectionNames.Tail)));
                try
                {
                    await runtime.Modules.Call(
                        context,
                        NativeBindingSelectionNames.Ordsets,
                        NativeBindingSelectionNames.Union,
                        outer
                    );
                    throw new InvalidDataException(NativeBindingSelectionNames.Failure);
                }
                catch (ErlangException error)
                {
                    if (observer.Failure is not null)
                        throw observer.Failure;
                    bool expected = error.Reason.Equals(Term.A(expectation == NativeBindingSelectionNames.Before ? NativeBindingSelectionNames.Replacement : NativeBindingSelectionNames.BadArgument));
                    bool helper = Cons.Items(error.StackTraceTerm).OfType<TupleTerm>()
                        .Any(frame => frame.Items.Count >= 2 && frame.Items[0].Equals(Term.A(NativeBindingSelectionNames.Lists))
                            && frame.Items[1].Equals(Term.A(NativeBindingSelectionNames.Helper)));
                    if (observer.Swaps != 1 || !expected || !helper)
                        throw new InvalidDataException(NativeBindingSelectionNames.Failure + error.Reason + error.StackTraceTerm);
                    completed.TrySetResult(new { Expectation = expectation, observer.Swaps, Reason = error.Reason.ToString(), NativeOnlyHelperPresent = helper, Stack = error.StackTraceTerm.ToString() });
                }
            }
            catch (Exception error)
            {
                completed.TrySetException(error);
            }

            return Nil.Value;
        });
        object result = await completed.Task.WaitAsync(NativeBindingSelectionNames.Deadline);
        await actor.Completion.WaitAsync(NativeBindingSelectionNames.Deadline);
        Console.WriteLine(JsonSerializer.Serialize(result));
    }
}
