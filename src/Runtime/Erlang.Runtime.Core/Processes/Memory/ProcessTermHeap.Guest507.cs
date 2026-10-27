namespace Erlang;

internal sealed partial class ProcessTermHeap506
{
    // Called only while this heap gate is held; no code/image/runtime callback.
    internal void ValidateGuestGraph507(Term value) => ValidateLocalGraph(value);

    internal Term AdoptGuest507(Term source, ProcessTermFrame506? construction = null)
    {
        var copies = new Dictionary<Term, Term>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<GuestAdoptionFrame507>();
        pending.Push(new(source));
        while (pending.TryPeek(out GuestAdoptionFrame507? frame))
        {
            if (IsOwnedLocal(frame.Source) || frame.Source is not (TupleTerm or Cons))
            {
                copies[frame.Source] = frame.Source;
                pending.Pop();
                continue;
            }
            if (frame.Next < frame.Children.Length)
            {
                Term child = frame.Children[frame.Next];
                if (!copies.TryGetValue(child, out Term? copied))
                {
                    pending.Push(new(child));
                    continue;
                }
                frame.Built[frame.Next++] = copied;
                continue;
            }
            Term built = frame.Source is Cons
                ? CreateCons(frame.Built[0], frame.Built[1]) : CreateTuple(frame.Built);
            construction?.PublishConstructedNode511(built);
            copies[frame.Source] = built;
            pending.Pop();
        }

        return copies[source];
    }
}
