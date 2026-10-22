namespace Erlang;

internal static class EtsCursorTerms420
{
    internal static long Small(Term term)
    {
        if (term is not Integer value || value.Value > RuntimeEtsCoreNames407.MaximumSmall64 || value.Value < -RuntimeEtsCoreNames407.MaximumSmall64 - 1) throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None);

        return (long)value.Value;
    }

    internal static Term EncodedSmall(long value)
    {
        ulong mask = (1UL << EtsCursorNames420.SmallPayloadBits) - 1;
        ulong word = (ulong)value & mask;

        return Term.I((word & (1UL << (EtsCursorNames420.SmallPayloadBits - 1))) == 0 ? (long)word : unchecked((long)(word | ~mask)));
    }

    internal static void List(Term term)
    {
        if (term is not Cons and not Nil) throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None);
    }

    internal static Term Prefix(
        Term source,
        long count,
        bool reverse,
        out Term suffix
    )
    {
        Term rest = source;
        var items = new List<Term>();
        while (count-- > 0)
        {
            if (rest is not Cons cell) throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None);
            items.Add(cell.Head);
            rest = cell.Tail;
        }
        if (reverse) items.Reverse();
        suffix = rest;

        return Cons.From(items);
    }

    internal static async ValueTask<Term> ReverseTree(ProcessContext context, Term source, Term continuation)
    {
        Term rest = source, result = Nil.Value, entryList = source, entryResult = Nil.Value;
        int remaining = EtsCursorNames420.TreeReverseBudget;
        while (rest is Cons cell)
        {
            context.Cancellation.ThrowIfCancellationRequested();
            if (--remaining == 0)
            {
                await Task.Yield();
                entryList = rest;
                entryResult = result;
                remaining = EtsCursorNames420.TreeReverseBudget;
                continue;
            }
            result = new Cons(cell.Head, result);
            rest = cell.Tail;
        }
        if (rest is not Nil) throw RuntimeExceptionCapture.Call(
            context,
            new ErlangException(ErlangErrorReasons.BadArgument),
            context.CaptureExecutionChain(),
            EtsCursorNames420.Module,
            EtsCursorNames420.ReverseHelper,
            [entryList, entryResult, continuation],
            true,
            Nil.Value
        );

        return Term.Tuple(result, continuation);
    }
}
