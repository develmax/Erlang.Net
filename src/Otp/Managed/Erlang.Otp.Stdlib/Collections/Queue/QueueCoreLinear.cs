using System.Numerics;

namespace Erlang;

internal static class QueueCoreLinear
{
    internal static async ValueTask<BigInteger> Length(
        ProcessContext context,
        Term list,
        string owner,
        int line,
        int ownerArity = 1
    )
    {
        BigInteger count = BigInteger.Zero;
        Term remaining = list;
        while (remaining is Cons cell)
        {
            await context.ReduceAsync();
            count++;
            remaining = cell.Tail;
        }
        if (remaining is not Nil)
            throw QueueCoreTrace.Dependency(
                context,
                ErlangModuleNames.Module,
                QueueCoreSource.ErlangLength,
                [list],
                owner,
                line,
                ownerArity
            );

        return count;
    }

    internal static ValueTask<Term> Reverse(
        ProcessContext context,
        Term list,
        string owner,
        int line,
        int ownerArity = 1,
        Term? tail = null,
        bool includeOwner = true
    ) => QueueCoreReverse.Run(
        context,
        list,
        owner,
        line,
        ownerArity,
        tail,
        includeOwner
    );

    internal static async ValueTask<(Term Prefix, Term Remainder)> Split(ProcessContext context, Term list, BigInteger count)
    {
        Term reversed = Nil.Value;
        while (count > BigInteger.Zero)
        {
            await context.ReduceAsync();
            var cell = (Cons)list;
            reversed = new Cons(cell.Head, reversed);
            list = cell.Tail;
            count--;
        }
        Term prefix = Nil.Value;
        while (reversed is Cons cell)
        {
            await context.ReduceAsync();
            prefix = new Cons(cell.Head, prefix);
            reversed = cell.Tail;
        }

        return (prefix, list);
    }

    internal static async ValueTask<Term> Append(
        ProcessContext context,
        Term prefix,
        Term tail,
        string owner,
        int line,
        int ownerArity = 1
    )
    {
        var pending = new Stack<Term>();
        Term remaining = prefix;
        while (remaining is Cons cell)
        {
            await context.ReduceAsync();
            pending.Push(cell.Head);
            remaining = cell.Tail;
        }
        if (remaining is not Nil)
            throw QueueCoreTrace.Dependency(
                context,
                ErlangModuleNames.Module,
                QueueCoreSource.Append,
                [prefix, tail],
                owner,
                line,
                ownerArity
            );
        while (pending.TryPop(out Term? item))
        {
            await context.ReduceAsync();
            tail = new Cons(item, tail);
        }

        return tail;
    }
}
