namespace Erlang;

// Input and every constructed child remain under one514 transaction custody
// until the caller publishes its final slot. Copy each physical occurrence;
// unlike same-process adoption, an import is not reference-memoized.
internal static class OwnedTermTransfer506
{
    internal static Term Copy(
        Term source,
        ProcessTermHeap506 heap,
        PreparedMapGraph517 maps,
        ProcessTermConstruction514 construction,
        PreparedMapGraphUse517 admitted
    )
    {
        var stack = new Stack<OwnedPayloadCopyFrame506>();
        stack.Push(new(source, maps));
        while (true)
        {
            OwnedPayloadCopyFrame506 frame = stack.Peek();
            if (frame.Index < frame.Children.Length)
            {
                stack.Push(new(frame.Children[frame.Index], maps));
                continue;
            }
            Term result = frame.NodePlan526 is { } nodePlan
                ? heap.CreateSharedMapNode526(
                    nodePlan,
                    construction,
                    frame.Copied,
                    admitted.For(nodePlan.Owner)
                )
                : frame.Source switch
                {
                    Integer when ProcessTermHeap506.IsImmediateInteger559(frame.Source) => frame.Source,
                    Integer number => heap.CreateInteger559(number.Value, construction),
                    FloatTerm number => heap.CreateFloat556(number.Value, construction),
                    ReferenceTerm reference when heap.IsOrdinaryLocalReference562(reference) => heap.CreateReference562(reference, construction),
                    Cons => heap.CreateCons(frame.Copied[0], frame.Copied[1]),
                    TupleTerm { Items.Count: 0 } => frame.Source,
                    TupleTerm => heap.CreateTuple(frame.Copied),
                    MapTerm when frame.MapPlan is { } plan => heap.CreateMapWithCustody517(
                        plan,
                        construction,
                        frame.Copied,
                        admitted.For(plan)
                    ),
                    _ => frame.Source
                };
            if (frame.MapPlan is null && frame.NodePlan526 is null && frame.Source is not (FloatTerm or Integer)
                && !heap.IsOrdinaryLocalReference562(frame.Source))
                construction.Publish(result);
            stack.Pop();
            if (stack.Count == 0)
                return result;
            OwnedPayloadCopyFrame506 parent = stack.Peek();
            parent.Copied[parent.Index++] = result;
        }
    }
}
