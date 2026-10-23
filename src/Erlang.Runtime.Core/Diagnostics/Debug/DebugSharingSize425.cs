namespace Erlang;

internal static class DebugSharingSize425
{
    internal static ulong Flat(Term source)
    {
        // Memoize each subtree's flat cost; add that cost on every incoming
        // edge. An exponential DAG does not require exponential CLR work.
        var known = new Dictionary<Term, ulong>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<DebugSharingSizeFrame425>();
        pending.Push(new(source));
        while (true)
        {
            DebugSharingSizeFrame425 frame = pending.Peek();
            if (frame.Index < frame.Children.Length)
            {
                Term child = frame.Children[frame.Index];
                if (known.TryGetValue(child, out ulong size))
                {
                    frame.Size = unchecked(frame.Size + size);
                    frame.Index++;
                }
                else pending.Push(new(child));
                continue;
            }
            known.Add(frame.Value, frame.Size);
            pending.Pop();
            if (pending.Count == 0) return frame.Size;
        }
    }

    internal static ulong Shared(ProcessRuntime runtime, Term source)
    {
        var seen = new HashSet<Term>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<Term>();
        pending.Push(source);
        ulong size = 0;
        while (pending.TryPop(out Term? value))
        {
            if (!seen.Add(value) || runtime.IsSharingLiteral425(value)) continue;
            size = unchecked(size + DebugSharingWordLayout425.OwnWords(value));
            foreach (Term child in DebugSharingGraph425.Children(value)) pending.Push(child);
        }

        return size;
    }
}
