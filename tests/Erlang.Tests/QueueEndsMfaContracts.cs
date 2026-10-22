namespace Erlang.Tests;

internal static class QueueEndsMfaContracts
{
    internal static readonly (string Function, int Arity)[] Exports =
    [
        (QueueModuleNames.InReverse, 2), (QueueModuleNames.OutReverse, 1),
        (QueueModuleNames.Get, 1), (QueueModuleNames.GetReverse, 1),
        (QueueModuleNames.Peek, 1), (QueueModuleNames.PeekReverse, 1),
        (QueueModuleNames.Drop, 1), (QueueModuleNames.DropReverse, 1),
        (QueueModuleNames.Cons, 2), (QueueModuleNames.Head, 1),
        (QueueModuleNames.Tail, 1), (QueueModuleNames.Snoc, 2),
        (QueueModuleNames.Last, 1), (QueueModuleNames.Daeh, 1),
        (QueueModuleNames.Init, 1), (QueueModuleNames.Liat, 1), (QueueModuleNames.Lait, 1)
    ];

    internal static bool Owns(string module, string function, int arity)
        => module == QueueModuleNames.Module && Exports.Contains((function, arity));

    internal static async Task Run(string function, int arity)
    {
        Require(Owns(QueueModuleNames.Module, function, arity));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term empty = Term.Tuple(Nil.Value, Nil.Value);
            Term queue = Term.Tuple(Term.List(Term.I(3), Term.I(2)), Term.List(Term.I(1)));
            Term[] validArguments = Arguments(function, queue);
            Term expected = Expected(function);
            Require((await runtime.Modules.Call(
                context,
                QueueModuleNames.Module,
                function,
                validArguments
            )).Equals(expected));
            Term invalid = Term.I(42);
            try
            {
                await runtime.Modules.Call(
                    context,
                    QueueModuleNames.Module,
                    function,
                    Arguments(function, invalid)
                );
                throw new InvalidOperationException(QueueEndsHostNames.Failure);
            }
            catch (ErlangException error)
            {
                Require(error.ExceptionClass == ErlangExceptionClasses.Error
                    && error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)));
                var frame = (TupleTerm)((Cons)error.StackTraceTerm).Head;
                Require(frame.Items[0].Equals(Term.A(QueueModuleNames.Module))
                    && frame.Items[1].Equals(Term.A(ErrorOwner(function))));
            }
            if (arity == 1 && function != QueueModuleNames.OutReverse && function != QueueModuleNames.Peek && function != QueueModuleNames.PeekReverse)
            {
                try
                {
                    await runtime.Modules.Call(
                        context,
                        QueueModuleNames.Module,
                        function,
                        empty
                    );
                    throw new InvalidOperationException(QueueEndsHostNames.Failure);
                }
                catch (ErlangException error)
                {
                    Require(error.Reason.Equals(Term.A(QueueEndsHostNames.Empty)));
                }
            }

            return Nil.Value;
        });
    }

    internal static Term[] Arguments(string function, Term queue) => function switch
    {
        QueueModuleNames.InReverse or QueueModuleNames.Cons => [Term.I(0), queue],
        QueueModuleNames.Snoc => [queue, Term.I(4)],
        _ => [queue]
    };

    internal static Term Expected(string function) => function switch
    {
        QueueModuleNames.InReverse or QueueModuleNames.Cons => Term.Tuple(Term.List(Term.I(3), Term.I(2)), Term.List(Term.I(0), Term.I(1))),
        QueueModuleNames.Snoc => Term.Tuple(Term.List(Term.I(4), Term.I(3), Term.I(2)), Term.List(Term.I(1))),
        QueueModuleNames.OutReverse => Term.Tuple(Term.Tuple(Term.A(QueueEndsHostNames.Value), Term.I(3)), Term.Tuple(Term.List(Term.I(2)), Term.List(Term.I(1)))),
        QueueModuleNames.Get or QueueModuleNames.Head => Term.I(1),
        QueueModuleNames.GetReverse or QueueModuleNames.Last or QueueModuleNames.Daeh => Term.I(3),
        QueueModuleNames.Peek => Term.Tuple(Term.A(QueueEndsHostNames.Value), Term.I(1)),
        QueueModuleNames.PeekReverse => Term.Tuple(Term.A(QueueEndsHostNames.Value), Term.I(3)),
        QueueModuleNames.Drop or QueueModuleNames.Tail => Term.Tuple(Term.List(Term.I(3)), Term.List(Term.I(2))),
        QueueModuleNames.DropReverse or QueueModuleNames.Init or QueueModuleNames.Liat or QueueModuleNames.Lait => Term.Tuple(Term.List(Term.I(2)), Term.List(Term.I(1))),
        _ => throw new InvalidOperationException(QueueEndsHostNames.Failure)
    };

    private static string ErrorOwner(string function) => function switch
    {
        QueueModuleNames.Cons => QueueModuleNames.InReverse,
        QueueModuleNames.Snoc => QueueModuleNames.In,
        QueueModuleNames.Tail => QueueModuleNames.Drop,
        QueueModuleNames.Init or QueueModuleNames.Liat or QueueModuleNames.Lait => QueueModuleNames.DropReverse,
        QueueModuleNames.Last or QueueModuleNames.Daeh => QueueModuleNames.GetReverse,
        _ => function
    };

    internal static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(QueueEndsHostNames.Failure);
    }
}
