namespace Erlang;

internal static class ProcessTableQueries
{
    private static readonly Atom None = Term.A(ProcessTableNames.None);

    public static Term Next(ProcessContext context, Term iterator)
    {
        if (iterator is not TupleTerm { Items.Count: 2 } pair)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        if (pair.Items[1] is Cons buffered)
            return Term.Tuple(buffered.Head, Term.Tuple(pair.Items[0], buffered.Tail));
        if (pair.Items[1] is not Nil || pair.Items[0] is not Integer cursor || cursor.Value < 0 || cursor.Value > int.MaxValue)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        int position = (int)cursor.Value;
        while (true)
        {
            context.Runtime.AdmitSignals(context);
            context.Cancellation.ThrowIfCancellationRequested();
            var batch = context.Runtime.ReadProcessBatch(position);
            if (batch is null)
                return None;
            position = batch.Cursor;
            if (batch.Processes.Count == 0)
                continue;
            Term tail = Nil.Value;
            for (int index = batch.Processes.Count - 1; index > 0; index--)
                tail = new Cons(batch.Processes[index], tail);

            return Term.Tuple(batch.Processes[0], Term.Tuple(Term.I(position), tail));
        }
    }
}
