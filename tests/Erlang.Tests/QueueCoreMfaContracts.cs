namespace Erlang.Tests;

internal static class QueueCoreMfaContracts
{
    internal static bool Owns(string module, string function, int arity)
        => module == QueueModuleNames.Module && (function, arity) is
            (QueueModuleNames.New, 0)
            or (QueueModuleNames.IsQueue, 1)
            or (QueueModuleNames.IsEmpty, 1)
            or (QueueModuleNames.Length, 1)
            or (QueueModuleNames.In, 2)
            or (QueueModuleNames.Out, 1)
            or (QueueModuleNames.FromList, 1)
            or (QueueModuleNames.ToList, 1);

    internal static async Task Run(string function, int arity)
    {
        if (!Owns(QueueModuleNames.Module, function, arity))
            throw new InvalidOperationException(QueueCoreHostNames.Failure);
        await using var runtime = new ProcessRuntime();
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        await owner.InvokeAsync(async context =>
        {
            Term empty = Term.Tuple(Nil.Value, Nil.Value);
            Term source = Term.List(Term.I(1), Term.I(2), Term.I(3));
            Term queue = Term.Tuple(Term.List(Term.I(3), Term.I(2)), Term.List(Term.I(1)));
            Term invalid = Term.A(QueueCoreHostNames.Item);
            Term improper = new Cons(Term.I(1), Term.A(QueueCoreHostNames.Tail));

            async ValueTask<Term> Call(params Term[] arguments)
                => await runtime.Modules.Call(
                    context,
                    QueueModuleNames.Module,
                    function,
                    arguments
                );

            async Task Equal(Term expected, params Term[] arguments)
            {
                if (!(await Call(arguments)).Equals(expected))
                    throw new InvalidOperationException(QueueCoreHostNames.Failure);
            }

            async Task BadArgument(params Term[] arguments)
            {
                try
                {
                    await Call(arguments);
                }
                catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                    && error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
                {
                    return;
                }

                throw new InvalidOperationException(QueueCoreHostNames.Failure);
            }
            switch (function)
            {
                case QueueModuleNames.New:
                    await Equal(empty);
                    break;
                case QueueModuleNames.IsQueue:
                    await Equal(Term.A(QueueCoreHostNames.True), queue);
                    await Equal(Term.A(QueueCoreHostNames.True), Term.Tuple(improper, Nil.Value));
                    await Equal(Term.A(QueueCoreHostNames.False), invalid);
                    break;
                case QueueModuleNames.IsEmpty:
                    await Equal(Term.A(QueueCoreHostNames.True), empty);
                    await Equal(Term.A(QueueCoreHostNames.False), queue);
                    await BadArgument(invalid);
                    break;
                case QueueModuleNames.Length:
                    await Equal(Term.I(3), queue);
                    await Equal(Term.I(0), empty);
                    await BadArgument(Term.Tuple(improper, Nil.Value));
                    break;
                case QueueModuleNames.In:
                    await Equal(Term.Tuple(Term.List(Term.I(4)), Nil.Value), Term.I(4), empty);
                    await Equal(Term.Tuple(Term.List(Term.I(4), Term.I(3), Term.I(2)), Term.List(Term.I(1))), Term.I(4), queue);
                    await BadArgument(Term.I(4), invalid);
                    break;
                case QueueModuleNames.Out:
                    await Equal(Term.Tuple(Term.A(QueueCoreHostNames.Empty), empty), empty);
                    await Equal(
                        Term.Tuple(Term.Tuple(Term.A(QueueCoreHostNames.Value), Term.I(1)),
                        Term.Tuple(Term.List(Term.I(3)), Term.List(Term.I(2)))),
                        queue
                    );
                    await BadArgument(invalid);
                    break;
                case QueueModuleNames.FromList:
                    await Equal(empty, Nil.Value);
                    await Equal(queue, source);
                    await BadArgument(improper);
                    break;
                case QueueModuleNames.ToList:
                    await Equal(Nil.Value, empty);
                    await Equal(source, queue);
                    await BadArgument(Term.Tuple(improper, Nil.Value));
                    break;
            }

            return Nil.Value;
        });
    }
}
