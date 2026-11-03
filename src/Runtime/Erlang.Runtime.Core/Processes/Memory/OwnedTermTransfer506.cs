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
        if (Monitor.IsEntered(heap.Gate)) throw new InvalidOperationException(HeapFunctionLayout.Invalid);
        var stack = new Stack<OwnedPayloadCopyFrame506>();
        stack.Push(new(source, maps));
        while (true)
        {
            OwnedPayloadCopyFrame506 frame = stack.Peek();
            bool actualLiteral;
            lock (heap.Gate) actualLiteral = heap.IsAuthenticatedLiteral801(frame.Source);
            if (actualLiteral)
            {
                Term shared = frame.Source;
                stack.Pop();
                if (stack.Count == 0) return shared;
                OwnedPayloadCopyFrame506 literalParent = stack.Peek();
                literalParent.Copied[literalParent.Index++] = shared;
                continue;
            }
            if (frame.Index == 0 && frame.RecordPlan is { } recordMetadata)
                lock (heap.Gate) recordMetadata.AdmitMetadata(construction);
            if (frame.Index < frame.Children.Length)
            {
                stack.Push(new(frame.Children[frame.Index], maps));
                continue;
            }
            HeapFunctionRebinding? rebound = frame.Source is FunctionTerm function
                ? HeapFunctionImport.Rebind(heap.Runtime524, function, frame.Copied) : null;
            Term result;
            lock (heap.Gate)
            {
                result = rebound is not null
                    ? heap.CreateImportedFunction(rebound, construction)
                    : frame.NodePlan526 is { } nodePlan
                    ? heap.CreateSharedMapNode526(
                        nodePlan,
                        construction,
                        frame.Copied,
                        admitted.For(nodePlan.Owner)
                    )
                    : frame.BitmapPlan785 is { } bitmapPlan
                    ? heap.CreateHamtBitmap785(
                        bitmapPlan,
                        construction,
                        frame.Copied,
                        admitted.For(bitmapPlan)
                    )
                    : frame.Source switch
                    {
                        Integer when ProcessTermHeap506.IsImmediateInteger559(frame.Source) => frame.Source,
                        Integer number => heap.CreateInteger559(number.Value, construction),
                        FloatTerm number => heap.CreateFloat556(number.Value, construction),
                        BitString binary when heap.CanOwnHeapBitString(binary) => heap.CreateHeapBitString(binary, construction),
                        BitString binary when heap.CanOwnOrdinaryBinary(binary) => heap.CreateOrdinaryBinary(
                            binary,
                            construction,
                            maps.FindOrdinaryBinary(binary),
                            false
                        ),
                        BitString binary when heap.IsNativeResourceBinary577(binary) => heap.CreateResourceBits577(binary, construction, false),
                        Term external when heap.CanOwnExternalIdentifier(external) => heap.CreateExternalIdentifier(external, construction, maps.FindExternalIdentifier(external)),
                        ResourceReferenceTerm resource when heap.IsNativeMagicReference576(resource) => heap.CreateMagicReference576(resource, construction),
                        ReferenceTerm reference when heap.IsLocalPidReference775(reference) => heap.CreatePidReference775(reference, construction),
                        ReferenceTerm reference when heap.IsOrdinaryLocalReference562(reference) => heap.CreateReference562(reference, construction),
                        Cons => heap.CreateCons(frame.Copied[0], frame.Copied[1]),
                        NativeRecordTerm record when heap.Runtime524.IsSharingLiteral425(record) => record,
                        NativeRecordTerm record => heap.CreateNativeRecord(
                            record,
                            frame.RecordPlan ?? throw new InvalidOperationException(NativeRecordHeapLayout.MissingPlan),
                            frame.Copied,
                            construction
                        ),
                        TupleTerm { Items.Count: 0 } => heap.CanonicalEmptyTuple803,
                        TupleTerm => heap.CreateTuple(frame.Copied),
                        MapTerm when frame.MapPlan is { } plan => heap.CreateMapWithCustody517(
                            plan,
                            construction,
                            frame.Copied,
                            admitted.For(plan)
                        ),
                        _ => frame.Source
                    };
                if (frame.MapPlan is null && frame.NodePlan526 is null && frame.BitmapPlan785 is null && frame.Source is not (FloatTerm or Integer or BitString)
                    && !heap.IsLocalPidReference775(frame.Source) && !heap.IsOrdinaryLocalReference562(frame.Source) && !heap.IsNativeMagicReference576(frame.Source))
                    construction.Publish(result);
            }
            stack.Pop();
            if (stack.Count == 0)
                return result;
            OwnedPayloadCopyFrame506 parent = stack.Peek();
            parent.Copied[parent.Index++] = result;
        }
    }
}
