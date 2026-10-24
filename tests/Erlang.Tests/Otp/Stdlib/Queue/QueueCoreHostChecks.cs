using Erlang;

namespace Erlang.Tests;

public static class QueueCoreHostChecks
{
    public static Task Representation()
    {
        Equal(QueueOperations.New(), Term.Tuple(Nil.Value, Nil.Value));
        Equal(QueueOperations.FromList(List(1)), Term.Tuple(List(1), Nil.Value));
        Equal(QueueOperations.FromList(List(1, 2)), Term.Tuple(List(2), List(1)));
        Equal(QueueOperations.FromList(List(1, 2, 3)), Term.Tuple(List(3, 2), List(1)));
        Equal(QueueOperations.FromList(List(
            1,
            2,
            3,
            4
        )), Term.Tuple(List(4, 3), List(1, 2)));
        Equal(QueueOperations.FromList(List(
            1,
            2,
            3,
            4,
            5
        )), Term.Tuple(List(5, 4, 3), List(1, 2)));
        Equal(QueueOperations.Out(Term.Tuple(List(3, 2, 1), Nil.Value)), Removed(1, Term.Tuple(List(3), List(2))));
        Equal(QueueOperations.Out(Term.Tuple(List(
            5,
            4,
            3,
            2
        ), List(1))), Removed(1, Term.Tuple(List(5, 4), List(2, 3))));
        Equal(QueueOperations.Out(QueueOperations.New()), Term.Tuple(Term.A(QueueCoreHostNames.Empty), QueueOperations.New()));

        return Task.CompletedTask;
    }

    public static Task ImproperLists()
    {
        Term item = Term.A(QueueCoreHostNames.Item);
        Term tail = Term.A(QueueCoreHostNames.Tail);
        Term improper = new Cons(item, tail);
        Term rearOnly = Term.Tuple(improper, Nil.Value);
        Equal(QueueOperations.IsQueue(rearOnly), Term.A(QueueCoreHostNames.True));
        Equal(QueueOperations.IsEmpty(rearOnly), Term.A(QueueCoreHostNames.False));
        Equal(QueueOperations.In(tail, rearOnly), Term.Tuple(new Cons(tail, improper), Nil.Value));
        Equal(
            QueueOperations.Out(Term.Tuple(Nil.Value, improper)),
            Term.Tuple(Term.Tuple(Term.A(QueueCoreHostNames.Value), item), Term.Tuple(Nil.Value, tail))
        );
        BadArgument(() => QueueOperations.Length(rearOnly));
        BadArgument(() => QueueOperations.ToList(rearOnly));
        BadArgument(() => QueueOperations.Out(rearOnly));
        BadArgument(() => QueueOperations.FromList(improper));
        Term[] invalid = [item, Nil.Value, Term.Tuple(), Term.Tuple(Nil.Value), Term.Tuple(Nil.Value, Nil.Value, Nil.Value), Term.Tuple(item, Nil.Value), Term.Tuple(Nil.Value, item)];
        foreach (Term value in invalid)
        {
            Equal(QueueOperations.IsQueue(value), Term.A(QueueCoreHostNames.False));
            BadArgument(() => QueueOperations.IsEmpty(value));
            BadArgument(() => QueueOperations.Length(value));
            BadArgument(() => QueueOperations.In(item, value));
            BadArgument(() => QueueOperations.Out(value));
            BadArgument(() => QueueOperations.ToList(value));
        }

        return Task.CompletedTask;
    }

    public static Task Ordering()
    {
        Term source = Cons.From(Enumerable.Range(0, QueueCoreHostNames.LargeCount).Select(value => Term.I(value)));
        Term queue = QueueOperations.New();
        foreach (Term item in Cons.Items(source))
            queue = QueueOperations.In(item, queue);
        Term retained = queue;
        Equal(QueueOperations.ToList(queue), source);
        Equal(QueueOperations.Length(queue), Term.I(QueueCoreHostNames.LargeCount));
        for (int index = 0; index < QueueCoreHostNames.LargeCount; index++)
        {
            var result = (TupleTerm)QueueOperations.Out(queue);
            var value = (TupleTerm)result.Items[0];
            Equal(value, Term.Tuple(Term.A(QueueCoreHostNames.Value), Term.I(index)));
            queue = result.Items[1];
        }
        Equal(queue, QueueOperations.New());
        Equal(QueueOperations.ToList(retained), source);

        return Task.CompletedTask;
    }

    private static Term List(params int[] values) => Cons.From(values.Select(value => Term.I(value)));

    private static Term Removed(int value, Term queue) => Term.Tuple(Term.Tuple(Term.A(QueueCoreHostNames.Value), Term.I(value)), queue);

    private static void Equal(Term actual, Term expected)
    {
        if (!actual.Equals(expected))
            throw new InvalidOperationException(QueueCoreHostNames.Failure);
    }

    private static void BadArgument(Func<Term> operation)
    {
        try
        {
            operation();
        }
        catch (ErlangException failure) when (failure.ExceptionClass == ErlangExceptionClasses.Error && failure.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
        {
            return;
        }
        throw new InvalidOperationException(QueueCoreHostNames.Failure);
    }
}
