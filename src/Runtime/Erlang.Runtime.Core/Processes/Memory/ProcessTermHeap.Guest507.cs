namespace Erlang;

internal sealed partial class ProcessTermHeap506
{
    // Called only while this heap gate is held; no code/image/runtime callback.
    internal void ValidateGuestGraph507(Term value) => ValidateLocalGraph(value);

    internal Term AdoptGuest507(
        Term source,
        ProcessTermConstruction514? construction = null,
        PreparedMapGraph517? maps = null,
        PreparedMapGraphUse517? admitted = null
    )
    {
        var copies = new Dictionary<Term, Term>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<GuestAdoptionFrame507>();
        pending.Push(new(source, maps));
        while (pending.TryPeek(out GuestAdoptionFrame507? frame))
        {
            if (IsOwnedLocal(frame.Source) || IsLiteralFloat556(frame.Source)
                || IsLiteralInteger559(frame.Source) || IsImmediateInteger559(frame.Source) || frame.NodePlan526 is null
                && (frame.Source is not (TupleTerm or Cons or MapTerm or FloatTerm or Integer)
                    || frame.Source is TupleTerm { Items.Count: 0 }
                    || frame.Source is MapTerm && frame.MapPlan is null))
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
                    pending.Push(new(child, maps));
                    continue;
                }
                frame.Built[frame.Next++] = copied;
                continue;
            }
            Term built = frame.NodePlan526 is { } nodePlan
                ? CreateSharedMapNode526(
                    nodePlan,
                    construction ?? throw new InvalidOperationException(MapRouting517Names.MissingPlan),
                    frame.Built,
                    admitted?.For(nodePlan.Owner) ?? throw new InvalidOperationException(MapRouting517Names.MissingPlan)
                )
                : frame.MapPlan is { } mapPlan
                ? CreateMapWithCustody517(
                    mapPlan,
                    construction ?? throw new InvalidOperationException(MapRouting517Names.MissingPlan),
                    frame.Built,
                    admitted?.For(mapPlan) ?? throw new InvalidOperationException(MapRouting517Names.MissingPlan)
                )
                : frame.Source is FloatTerm number
                ? CreateFloat556(number.Value, construction ?? throw new InvalidOperationException(ProcessTermConstruction514Names.InvalidCustody))
                : frame.Source is Integer integer
                ? CreateInteger559(integer.Value, construction ?? throw new InvalidOperationException(ProcessTermConstruction514Names.InvalidCustody))
                : frame.Source is Cons ? CreateCons(frame.Built[0], frame.Built[1]) : CreateTuple(frame.Built);
            if (frame.MapPlan is null && frame.NodePlan526 is null && frame.Source is not (FloatTerm or Integer))
                construction?.Publish(built);
            copies[frame.Source] = built;
            pending.Pop();
        }

        return copies[source];
    }
}
