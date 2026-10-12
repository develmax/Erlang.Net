namespace Erlang;

internal sealed class ProcessSavedCalls
{
    private readonly object gate = new();
    private volatile Term[]? entries;
    private int next;
    private int count;
    private static readonly Atom Send = Term.A(ProcessSavedCallNames.Send);
    private static readonly Atom Receive = Term.A(ProcessSavedCallNames.Receive);
    private static readonly Atom Timeout = Term.A(ProcessSavedCallNames.Timeout);

    public int Configure(Term value)
    {
        if (value is not Integer size || size.Value < 0 || size.Value > ProcessSavedCallParameters.MaximumCapacity)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        int capacity = (int)size.Value;
        lock (gate)
        {
            int previous = entries?.Length ?? 0;
            entries = capacity == 0 ? null : new Term[capacity];
            next = 0;
            count = 0;

            return previous;
        }
    }

    public void Record(string module, string function, int arity)
    {
        if (entries is null)
            return;
        Append(Term.Tuple(Term.A(module), Term.A(function), Term.I(arity)));
    }

    public void Sent() => Append(Send);

    public void Received() => Append(Receive);

    public void TimedOut() => Append(Timeout);

    private void Append(Term value)
    {
        if (entries is null)
            return;
        lock (gate)
        {
            if (entries is null)
                return;
            entries[next] = value;
            next = (next + 1) % entries.Length;
            count = Math.Min(count + 1, entries.Length);
        }
    }

    public Term Snapshot()
    {
        lock (gate)
        {
            if (entries is null)
                return Term.A(ErlangBooleanAtoms.False);
            var values = new Term[count];
            int first = (next - count + entries.Length) % entries.Length;
            for (int index = 0; index < count; index++)
                values[index] = entries[(first + index) % entries.Length];

            return Cons.From(values);
        }
    }
}
