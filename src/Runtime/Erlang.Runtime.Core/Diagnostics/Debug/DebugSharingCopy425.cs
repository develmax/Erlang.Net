namespace Erlang;

internal static class DebugSharingCopy425
{
    internal static Term Copy(ProcessRuntime runtime, Term source, bool copyLiterals)
        => AtomicsHostAggregate594.Publish(transaction => CopySelected377(
            runtime,
            source,
            copyLiterals,
            transaction
        ));

    internal static Term CopySelected377(
        ProcessRuntime runtime,
        Term source,
        bool copyLiterals,
        AtomicsHostCopyTransaction591 transaction,
        DebugCopyDestination391? destination = null
    )
    {
        var known = new Dictionary<Term, Term>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<DebugSharingCopyFrame425>();
        pending.Push(new(source));
        while (true)
        {
            DebugSharingCopyFrame425 frame = pending.Peek();
            if (DebugSharingWordLayout425.Immediate(frame.Source) || (!copyLiterals && runtime.IsSharingLiteral425(frame.Source)))
            {
                known.Add(frame.Source, frame.Source);
                pending.Pop();
                if (pending.Count == 0) return frame.Source;
                continue;
            }
            if (copyLiterals && runtime.IsSharingLiteral425(frame.Source))
            {
                // Literal storage must never be marked in the sharing table.
                // Source copy_shared_perform copies each literal occurrence
                // through copy_struct_x, whose traversal is flat, not memoized.
                Term literalCopy = DebugDirtyTermCopy424.CopySelected595(
                    runtime,
                    frame.Source,
                    transaction,
                    destination
                );
                pending.Pop();
                if (pending.Count == 0) return literalCopy;
                DebugSharingCopyFrame425 parent = pending.Peek();
                parent.Copied[parent.Index++] = literalCopy;
                continue;
            }
            if (frame.Index < frame.Children.Length)
            {
                Term child = frame.Children[frame.Index];
                if (known.TryGetValue(child, out Term? copy)) frame.Copied[frame.Index++] = copy;
                else pending.Push(new(child));
                continue;
            }
            Term result = frame.Source switch
            {
                Cons => new Cons(frame.Copied[0], frame.Copied[1]),
                TupleTerm => new TupleTerm(frame.Copied),
                HamtBitmapTerm429 node => new HamtBitmapTerm429(
                    node.Domain,
                    node.Ownership,
                    node.Bitmap,
                    frame.Copied
                ),
                MapTerm map => CopyMap(map, frame.Copied),
                NativeRecordTerm record => new NativeRecordTerm(
                    record.Module,
                    record.Name,
                    record.IsExported,
                    record.FieldNames.Select((name, index) => new KeyValuePair<Atom, Term>(name, frame.Copied[index]))
                ),
                FunctionTerm function => CopyFunction(runtime, function, frame.Copied),
                _ => DebugDirtyTermCopy424.CopySelected595(
                    runtime,
                    frame.Source,
                    transaction,
                    destination
                )
            };
            result = destination?.Adopt(result) ?? result;
            known.Add(frame.Source, result);
            pending.Pop();
            if (pending.Count == 0)
            {
                runtime.ObserveIncomingTerm(result);

                return result;
            }
        }
    }

    private static MapTerm CopyMap(MapTerm source, Term[] values)
    {
        if (source.FlatKeys426 is not null)
        {
            var keys = (TupleTerm)values[0];

            return new MapTerm(
                keys.Items.Select((key, index) => new KeyValuePair<Term, Term>(key, values[index + 1])),
                keys,
                rebaseCopiedFlatKeys: true
            );
        }

        return new MapTerm(Enumerable.Range(0, values.Length / 2)
            .Select(index => new KeyValuePair<Term, Term>(values[index * 2], values[index * 2 + 1])));
    }

    private static FunctionTerm CopyFunction(ProcessRuntime runtime, FunctionTerm source, Term[] environment)
    {
        IRuntimeStoredFunctionRecipe recipe = RuntimeStoredFunctionRecipes.Capture(runtime, source);
        if (recipe is ExternalStoredFunctionRecipe external)
            recipe = new DebugDirtyExternalFunctionRecipe424(external.Owner, external.Identity);

        return recipe.Rebind(environment);
    }
}
