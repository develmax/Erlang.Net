namespace Erlang;

public sealed partial class Mailbox
{
    internal MailboxSnapshot601 CaptureSnapshot601()
    {
        lock (gate)
        {
            // Both arrays are allocated before the first interest acquisition.
            var captured = new MailboxSnapshot601(messages.Count);
            try
            {
                foreach (Entry entry in messages)
                    if (!entry.Control) captured.Add(entry.Value, entry.Custody593?.Graph);
            }
            catch (Exception error)
            {
                captured.Failure = error;
            }

            return captured;
        }
    }

    private IReadOnlyList<Term> CompletePublicSnapshot601(MailboxSnapshot601 captured)
        => captured.With(() =>
        {
            Term[] values = captured.Values.ToArray();
            if (Runtime593 is not { } runtime) return (IReadOnlyList<Term>)values;
            int first = 0;
            while (first < values.Length && !MailboxValueEscape.RequiresDetach(values[first])) first++;
            if (first == values.Length) return (IReadOnlyList<Term>)values;

            return AtomicsHostAggregate594.Publish(transaction =>
            {
                // Preserve ordinary public term/callback identities. Only the
                // messages needing real Host custody require portable copying.
                for (int index = first; index < values.Length; index++)
                {
                    Term value = values[index];
                    if (!MailboxValueEscape.RequiresDetach(value)) continue;
                    Term portable = OwnedTermDetachedCopy506.CopyAtomics586(
                        runtime,
                        value,
                        exportHostAtomics586: true,
                        hostCopies591: transaction
                    );
                    values[index] = NativeResourceRoots524.Contains524(portable)
                        ? NativeTermHostScope524.AdmitCurrent524(runtime, portable) : portable;
                }

                return (IReadOnlyList<Term>)values;
            });
        });
}
