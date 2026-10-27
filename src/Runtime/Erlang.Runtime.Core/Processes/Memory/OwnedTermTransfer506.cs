namespace Erlang;

// Input is already a detached portable graph. This commit pass invokes no code
// descriptor factories, atom creation, node observation or runtime callbacks.
internal static class OwnedTermTransfer506
{
    internal static Term Copy(Term source, ProcessTermHeap506 heap)
    {
        var stack = new Stack<OwnedPayloadCopyFrame506>();
        stack.Push(new(source));
        while (true)
        {
            OwnedPayloadCopyFrame506 frame = stack.Peek();
            if (frame.Index < frame.Children.Length)
            {
                stack.Push(new(frame.Children[frame.Index]));
                continue;
            }
            Term result = frame.Source switch
            {
                Cons => heap.CreateCons(frame.Copied[0], frame.Copied[1]),
                TupleTerm => heap.CreateTuple(frame.Copied),
                _ => frame.Source
            };
            stack.Pop();
            if (stack.Count == 0)
                return result;
            OwnedPayloadCopyFrame506 parent = stack.Peek();
            parent.Copied[parent.Index++] = result;
        }
    }

}
