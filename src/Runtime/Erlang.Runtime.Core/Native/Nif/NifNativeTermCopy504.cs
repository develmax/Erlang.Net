namespace Erlang;

internal static class NifNativeTermCopy504
{
    internal static NifNativeTermValue Copy(NifNativeTermValue value)
        => CopyWithStatic516(value, null);

    internal static NifNativeTermValue CopyWithStatic516(
        NifNativeTermValue value,
        Func<Term, Term>? copyStatic
    )
    {
        if (!value.IsPending)
            return copyStatic is null ? value : new(copyStatic(value.StaticTerm!));
        var copies = new Dictionary<NifNativeTermNode, NifNativeTermValue>(ReferenceEqualityComparer.Instance);
        var stack = new Stack<(NifNativeTermNode Node, bool Exit)>();
        stack.Push((value.Pending!, false));
        while (stack.TryPop(out var frame))
        {
            if (copies.ContainsKey(frame.Node))
                continue;
            if (!frame.Exit)
            {
                stack.Push((frame.Node, true));
                for (int index = frame.Node.ChildCount - 1; index >= 0; index--)
                    if (frame.Node.Child(index).Pending is { } child && !copies.ContainsKey(child))
                        stack.Push((child, false));
                continue;
            }

            NifNativeTermValue Read(NifNativeTermValue child) => child.Pending is { } node
                ? copies[node]
                : copyStatic is null ? child : new(copyStatic(child.StaticTerm!));
            NifNativeTermValue copied = frame.Node switch
            {
                NifNativeBinaryNode binary => binary.Storage.Size <= NifNativeTermCopy504Names.HeapBinaryMaximumBytes
                    ? new(NifNativeTermGraph.Resolve(new(binary))) : new(binary),
                NifNativeTupleNode tuple => new(new NifNativeTupleNode(tuple.Elements.Select(Read).ToArray())),
                NifNativeConsNode cons => new(new NifNativeConsNode(Read(cons.Head), Read(cons.Tail))),
                NifNativeMapNode504 map => new(map.Copy(Read)),
                _ => throw new InvalidOperationException(NifNativeTermCopy504Names.UnknownNode)
            };
            copies.Add(frame.Node, copied);
        }

        return copies[value.Pending!];
    }
}
