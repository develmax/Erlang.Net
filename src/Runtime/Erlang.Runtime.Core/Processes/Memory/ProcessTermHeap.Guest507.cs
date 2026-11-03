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
    ) => AdoptGuestCore800(
        source,
        construction,
        maps,
        admitted,
        null
    );

    private Term AdoptGuestCore800(
        Term source,
        ProcessTermConstruction514? construction,
        PreparedMapGraph517? maps,
        PreparedMapGraphUse517? admitted,
        Dictionary<Term, Term>? literalCopies800
    )
    {
        if (literalCopies800 is not null && LiteralDomain800 is not { IsSealed800: false })
            throw new InvalidOperationException(ModuleLiteral800Names.Invalid);
        var copies = literalCopies800 ?? new Dictionary<Term, Term>(ReferenceEqualityComparer.Instance);
        if (copies.TryGetValue(source, out Term? previous800)) return previous800;
        var pending = new Stack<GuestAdoptionFrame507>();
        pending.Push(new(source, maps));
        while (pending.TryPeek(out GuestAdoptionFrame507? frame))
        {
            if (IsAuthenticatedLiteral801(frame.Source))
            {
                copies[frame.Source] = frame.Source;
                pending.Pop();
                continue;
            }
            if (frame.Source is TupleTerm { OwnedStorage506: null, Items.Count: 0 })
            {
                Term empty = CanonicalEmptyTuple803;
                construction?.Admit(empty);
                copies[frame.Source] = empty;
                pending.Pop();
                continue;
            }
            if (frame.Next == 0 && frame.RecordPlan is { } recordMetadata)
                recordMetadata.AdmitMetadata(construction ?? throw new InvalidOperationException(ProcessTermConstruction514Names.InvalidCustody));
            if (frame.Source is FunctionTerm function && !IsOwnedLocal(function))
            {
                PreparedCanonicalFunction? prepared = maps?.FindCanonicalFunction(function);
                if (prepared is not null && HasActualFunctionEnvironment(prepared))
                {
                    var authority = construction ?? throw new InvalidOperationException(ProcessTermConstruction514Names.InvalidCustody);
                    if (!authority.Contains(function)) throw new InvalidOperationException(HeapFunctionLayout.Invalid);
                    // The map is keyed by the admitted source, but only its
                    // private fresh clone receives actual guest storage.
                    authority.Admit(prepared.Function);
                    copies[function] = CreateCanonicalLocalFunction(prepared, authority);
                }
                else copies[function] = function;
                pending.Pop();
                continue;
            }
            if (frame.Source is NativeRecordTerm literalRecord && runtime.IsSharingLiteral425(literalRecord)
                || frame.Source is Pid or PortTerm && !CanOwnExternalIdentifier(frame.Source)
                || IsOwnedLocal(frame.Source) || LiteralDomain800 is null
                    && (IsLoaderMapKeys565(frame.Source) || IsLiteralFloat556(frame.Source) || IsLiteralInteger559(frame.Source))
                || IsImmediateInteger559(frame.Source)
                || frame.Source is BitString candidate && !IsNativeResourceBinary577(candidate) && !CanOwnHeapBitString(candidate) && !CanOwnOrdinaryBinary(candidate)
                || frame.Source is ReferenceTerm && !IsLocalPidReference775(frame.Source) && !IsOrdinaryLocalReference562(frame.Source) && !IsNativeMagicReference576(frame.Source) && !CanOwnExternalIdentifier(frame.Source) || frame.NodePlan526 is null && frame.BitmapPlan785 is null
                && (frame.Source is not (TupleTerm or Cons or MapTerm or FloatTerm or Integer or ReferenceTerm or BitString or Pid or PortTerm or NativeRecordTerm)
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
                : frame.BitmapPlan785 is { } bitmapPlan
                ? CreateHamtBitmap785(
                    bitmapPlan,
                    construction ?? throw new InvalidOperationException(MapRouting517Names.MissingPlan),
                    frame.Built,
                    admitted?.For(bitmapPlan) ?? throw new InvalidOperationException(MapRouting517Names.MissingPlan)
                )
                : frame.MapPlan is { } mapPlan
                ? CreateMapWithCustody517(
                    mapPlan,
                    construction ?? throw new InvalidOperationException(MapRouting517Names.MissingPlan),
                    frame.Built,
                    admitted?.For(mapPlan) ?? throw new InvalidOperationException(MapRouting517Names.MissingPlan)
                )
                : frame.Source is BitString binary
                ? CanOwnHeapBitString(binary)
                    ? CreateHeapBitString(binary, construction ?? throw new InvalidOperationException(ProcessTermConstruction514Names.InvalidCustody))
                    : CanOwnOrdinaryBinary(binary)
                        ? CreateOrdinaryBinary(
                            binary,
                            construction ?? throw new InvalidOperationException(ProcessTermConstruction514Names.InvalidCustody),
                            maps!.FindOrdinaryBinary(binary),
                            true
                        )
                        : CreateResourceBits577(binary, construction ?? throw new InvalidOperationException(ProcessTermConstruction514Names.InvalidCustody))
                : frame.Source is NativeRecordTerm record
                ? CreateNativeRecord(
                    record,
                    frame.RecordPlan ?? throw new InvalidOperationException(NativeRecordHeapLayout.MissingPlan),
                    frame.Built,
                    construction ?? throw new InvalidOperationException(ProcessTermConstruction514Names.InvalidCustody)
                )
                : frame.Source is FloatTerm number
                ? CreateFloat556(number.Value, construction ?? throw new InvalidOperationException(ProcessTermConstruction514Names.InvalidCustody))
                : frame.Source is Integer integer
                ? CreateInteger559(integer.Value, construction ?? throw new InvalidOperationException(ProcessTermConstruction514Names.InvalidCustody))
                : CanOwnExternalIdentifier(frame.Source)
                ? CreateExternalIdentifier(
                    frame.Source,
                    construction ?? throw new InvalidOperationException(ProcessTermConstruction514Names.InvalidCustody),
                    maps!.FindExternalIdentifier(frame.Source)
                )
                : frame.Source is ResourceReferenceTerm resource && IsNativeMagicReference576(resource)
                ? CreateMagicReference576(resource, construction ?? throw new InvalidOperationException(ProcessTermConstruction514Names.InvalidCustody))
                : frame.Source is ReferenceTerm pidReference && IsLocalPidReference775(pidReference)
                ? CreatePidReference775(pidReference, construction ?? throw new InvalidOperationException(ProcessTermConstruction514Names.InvalidCustody))
                : frame.Source is ReferenceTerm reference
                ? CreateReference562(reference, construction ?? throw new InvalidOperationException(ProcessTermConstruction514Names.InvalidCustody))
                : frame.Source is Cons ? CreateCons(frame.Built[0], frame.Built[1]) : CreateTuple(frame.Built);
            if (frame.MapPlan is null && frame.NodePlan526 is null && frame.BitmapPlan785 is null && frame.Source is not (FloatTerm or Integer or BitString)
                && !IsLocalPidReference775(frame.Source) && !IsOrdinaryLocalReference562(frame.Source) && !IsNativeMagicReference576(frame.Source))
                construction?.Publish(built);
            copies[frame.Source] = built;
            pending.Pop();
        }

        return copies[source];
    }
}
