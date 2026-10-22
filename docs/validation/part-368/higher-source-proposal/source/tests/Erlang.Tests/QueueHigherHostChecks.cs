namespace Erlang.Tests;

internal static class QueueHigherHostChecks
{
    internal static async Task CallbackOrderAndImproperEvaluation()
    {
        await using var runtime = new ProcessRuntime();
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        await owner.InvokeAsync(async context =>
        {
            var observed = new List<Term>();
            var callback = new FunctionTerm(1, (_, a) =>
            {
                observed.Add(a[0]);

                return ValueTask.FromResult<Term>(Term.A(ErlangBooleanAtoms.True));
            });
            Term queue = Term.Tuple(Term.List(Term.I(4), Term.I(3)), Term.List(Term.I(1), Term.I(2)));
            await runtime.Modules.Call(context, QueueModuleNames.Module, QueueModuleNames.Filter, [callback, queue]);
            if (!Cons.From(observed).Equals(Term.List(Term.I(1), Term.I(2), Term.I(3), Term.I(4))))
                throw new InvalidOperationException(QueueHigherHostNames.Failure);
            observed.Clear();
            Term malformed = Term.Tuple(new Cons(Term.I(3), Term.A(QueueHigherHostNames.Tail)), Term.List(Term.I(1)));
            try
            {
                await runtime.Modules.Call(context, QueueModuleNames.Module, QueueModuleNames.Filter, [callback, malformed]);
                throw new InvalidOperationException(QueueHigherHostNames.Failure);
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.FunctionClause)))
            {
                if (!Cons.From(observed).Equals(Term.List(Term.I(1))))
                    throw new InvalidOperationException(QueueHigherHostNames.Failure);
            }
            Term falseTail = Term.Tuple(Nil.Value, new Cons(Term.I(1), Term.A(ErlangBooleanAtoms.False)));
            Term result = await runtime.Modules.Call(context, QueueModuleNames.Module, QueueModuleNames.Delete, [Term.I(1), falseTail]);
            if (!result.Equals(falseTail))
                throw new InvalidOperationException(QueueHigherHostNames.Failure);

            return Nil.Value;
        });
    }
}
