namespace Erlang;

public sealed partial class ProcessDictionary
{
    internal bool TryKeysGuest605(
        OwnedTermInvocation506 invocation,
        Term? expectedValue,
        bool filter,
        out Term result
    )
    {
        result = Nil.Value;
        if (!Admitted507(invocation))
            return false;

        KeyValuePair<Term, Term>[] captured;
        ProcessTermBorrow506 borrow;
        lock (gate)
        {
            CheckOpen();
            captured = data.ToArray();
            // This is registered guest custody, with no detached Host outputs.
            // Its independent frame protects both keys and values while exact
            // comparison and selected result construction run outside D.
            borrow = context.TermHeap506.Borrow(frame!, Term.Tuple(
                captured.SelectMany(pair => new[] { pair.Key, pair.Value }).ToArray()
            ));
        }

        using (borrow)
        {
            invocation.HoldGuestResult507(Nil.Value);
            for (int index = captured.Length - 1; index >= 0; index--)
            {
                context.Cancellation.ThrowIfCancellationRequested();
                KeyValuePair<Term, Term> pair = captured[index];
                if (filter && !pair.Value.Equals(expectedValue))
                    continue;

                invocation.Frame.CreateConsInto507(
                    invocation.ResultSlot507,
                    pair.Key,
                    invocation.Frame.Read(invocation.ResultSlot507)
                );
            }
            result = invocation.Frame.Read(invocation.ResultSlot507);

            return true;
        }
    }
}
