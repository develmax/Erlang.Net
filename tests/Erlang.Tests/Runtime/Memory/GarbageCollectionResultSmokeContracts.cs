namespace Erlang.Tests;

internal static class GarbageCollectionResultSmokeContracts
{
    internal static bool Owns(string module, string function, int arity)
        => module == ErlangModuleNames.Module && function == GarbageCollectionResultSmokeNames.Function && arity == 1;

    internal static async Task Run()
    {
        foreach (int capacity in GarbageCollectionResultSmokeNames.Capacities)
        {
            await using var runtime = new ProcessRuntime(capacity);
            var actor = runtime.Spawn(async context =>
            {
                Require((await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    SequenceTraceNames.SystemInfo,
                    Term.A(GarbageCollectionResultSmokeNames.Schedulers)
                )).Equals(Term.I(capacity)));
                try
                {
                    _ = await runtime.Modules.Call(
                        context,
                        ErlangModuleNames.Module,
                        GarbageCollectionResultSmokeNames.Function,
                        Nil.Value
                    );
                    Require(false);
                }
                catch (ErlangException exception) when (exception.Reason.Equals(Term.A(ErlangErrorReasons.FunctionClause)))
                {
                }
                var reference = runtime.MakeReference();
                Term marker = Term.A(GarbageCollectionResultSmokeNames.Marker);
                runtime.Send(context.Self, marker);
                for (int index = 0; index < capacity; index++)
                    runtime.Send(context.Self, Term.Tuple(reference, Term.Tuple(Term.I(index), Term.I(2), Term.I(3))));
                Term extra = Term.Tuple(reference, Term.Tuple(Term.I(0), Term.I(99), Term.I(99)));
                runtime.Send(context.Self, extra);
                Require((await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    GarbageCollectionResultSmokeNames.Function,
                    reference
                )).Equals(Term.Tuple(Term.I(2 * capacity), Term.I(3 * capacity), Term.I(0))));
                Require((await context.ReceiveAsync<Term>(value => value, TimeSpan.Zero))!.Equals(marker));
                Require((await context.ReceiveAsync<Term>(value => value, TimeSpan.Zero))!.Equals(extra));
                Require(await context.ReceiveAsync<Term>(value => value, TimeSpan.Zero) is null);

                return Nil.Value;
            });
            Require((await actor.Completion.WaitAsync(GarbageCollectionResultSmokeNames.Deadline)).Equals(Term.A(ProcessExitReasons.Normal)));
        }
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(GarbageCollectionResultSmokeNames.Failure);
    }
}
