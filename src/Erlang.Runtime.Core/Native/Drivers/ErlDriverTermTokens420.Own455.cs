namespace Erlang;

internal sealed partial class ErlDriverTermTokens420
{
    private readonly Dictionary<Term, ErlDriverOwnIdentity455> ownIdentities455 = new(ReferenceEqualityComparer.Instance);

    internal ErlDriverTermWire455 CaptureOwnTerm455(Term value)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closed, this);
            int occurrence = 0;
            var bindings = new List<ErlDriverOwnTermBinding455>();
            var pending = new Stack<Term>();
            pending.Push(value);
            while (pending.TryPop(out Term? term))
            {
                switch (term)
                {
                    case Pid or PortTerm:
                        if (ownIdentities455.TryGetValue(term, out var own))
                            bindings.Add(new(occurrence, own.Actor, own.Handle));
                        occurrence = checked(occurrence + 1);
                        break;
                    case TupleTerm tuple:
                        for (int index = tuple.Items.Count - 1; index >= 0; index--) pending.Push(tuple.Items[index]);
                        break;
                    case Cons list:
                        pending.Push(list.Tail);
                        pending.Push(list.Head);
                        break;
                    case MapTerm map:
                        // Match the ordinary ETF writer's exact traversal.
                        for (int index = map.ArithmeticKeyEntries.Count - 1; index >= 0; index--)
                        {
                            var entry = map.ArithmeticKeyEntries[index];
                            pending.Push(entry.Value);
                            pending.Push(entry.Key);
                        }
                        break;
                }
            }

            // Bind cannot publish another spelling while either traversal runs.
            return new(ExternalTermFormat.Encode(value), bindings.ToArray());
        }
    }
}
