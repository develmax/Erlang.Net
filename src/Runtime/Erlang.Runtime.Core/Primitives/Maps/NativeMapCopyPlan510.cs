namespace Erlang;

// Physical heap occurrences are copied independently, exactly once per actual
// tree/flat slot. Canonical/arithmetic arrays are views, not new occurrences.
internal sealed class NativeMapCopyPlan510
{
    private readonly ProcessRuntime runtime;
    private readonly NativeMapMaterializedLayout505 layout;
    private readonly NativeMapCopyNode510? root;
    private readonly List<KeyValuePair<Term, Term>> rows = new();
    private readonly Dictionary<KeyValuePair<Term, Term>, List<int>> rowIdentities = new(NativeMapRowReferenceComparer510.Instance);
    private readonly int[] physical;
    private readonly int[] canonical;
    private readonly int[] arithmetic;
    private readonly int[]? flatKeys;
    internal Term[] Children { get; }

    internal NativeMapCopyPlan510(ProcessRuntime runtime, MapTerm source)
    {
        this.runtime = runtime;
        if (!NativeMapMaterialization505.TryRead(source, null, out layout!))
            throw new NotSupportedException(NativeMapCopy510Names.MissingLayout);
        if (!layout.Matches(runtime))
            throw new NotSupportedException(NativeMapCopy510Names.ForeignDomain);
        // Source admission occurs outside all process heap gates.
        runtime.RequireMapHashOpen();
        if (layout.Root is { } original)
        {
            root = new(original);
            var nodes = new Stack<NativeMapCopyNode510>();
            nodes.Push(root);
            while (nodes.TryPeek(out var node))
            {
                if (node.Source.Leaf is { } pair)
                {
                    node.Row = AddRow(pair);
                    nodes.Pop();
                }
                else if (node.Next < node.Source.Children.Count)
                {
                    var child = new NativeMapCopyNode510(node.Source.Children[node.Next++]);
                    node.Children.Add(child);
                    nodes.Push(child);
                }
                else
                    nodes.Pop();
            }
        }
        else
            foreach (var pair in layout.PhysicalEntries)
                AddRow(pair);
        physical = View(layout.PhysicalEntries);
        canonical = View(source.Entries);
        arithmetic = View(source.ArithmeticKeyEntries);
        var children = rows.SelectMany(pair => new[] { pair.Key, pair.Value }).ToList();
        if (source.FlatKeys426 is { } keys)
        {
            var keysByIdentity = new Dictionary<Term, Queue<int>>(ReferenceEqualityComparer.Instance);
            foreach (int row in physical)
            {
                if (!keysByIdentity.TryGetValue(rows[row].Key, out var occurrences))
                    keysByIdentity.Add(rows[row].Key, occurrences = new());
                occurrences.Enqueue(row);
            }
            flatKeys = new int[keys.Items.Count];
            for (int index = 0; index < flatKeys.Length; index++)
            {
                Term key = keys.Items[index];
                if (keysByIdentity.TryGetValue(key, out var occurrences) && occurrences.TryDequeue(out int match))
                    flatKeys[index] = match * 2;
                else
                {
                    flatKeys[index] = children.Count;
                    children.Add(key);
                }
            }
        }
        Children = children.ToArray();
    }

    private int AddRow(KeyValuePair<Term, Term> row)
    {
        int index = rows.Count;
        rows.Add(row);
        if (!rowIdentities.TryGetValue(row, out var occurrences))
            rowIdentities.Add(row, occurrences = new());
        occurrences.Add(index);

        return index;
    }

    private int[] View(IReadOnlyList<KeyValuePair<Term, Term>> source)
    {
        var next = new Dictionary<KeyValuePair<Term, Term>, int>(NativeMapRowReferenceComparer510.Instance);

        return source.Select(pair =>
        {
            int cursor = next.GetValueOrDefault(pair);
            int index = rowIdentities.TryGetValue(pair, out var occurrences) && cursor < occurrences.Count
                ? occurrences[cursor] : AddRow(pair);
            next[pair] = cursor + 1;

            return index;
        }).ToArray();
    }

    internal MapTerm Build(Term[] values)
    {
        if (values.Length != Children.Length)
            throw new ArgumentException(NativeMapCopy510Names.Payload, nameof(values));

        KeyValuePair<Term, Term> Row(int index) => new(values[index * 2], values[index * 2 + 1]);
        HamtViewNode? copiedRoot = null;
        if (root is not null)
        {
            // Build can be called only once for this explicit copy frame.
            var nodes = new Stack<(NativeMapCopyNode510 Node, bool Exit)>();
            var completed = new Stack<HamtViewNode>();
            nodes.Push((root, false));
            while (nodes.TryPop(out var current))
            {
                var source = current.Node.Source;
                if (current.Exit)
                {
                    var children = new HamtViewNode[current.Node.Children.Count];
                    for (int index = children.Length - 1; index >= 0; index--)
                        children[index] = completed.Pop();
                    completed.Push(new(
                        source.Leaf is null ? null : Row(current.Node.Row),
                        children,
                        source.Bitmap,
                        source.IsCollision,
                        layout.Domain,
                        layout.HashRetention,
                        source.IsHead
                    ));
                    continue;
                }
                nodes.Push((current.Node, true));
                for (int index = current.Node.Children.Count - 1; index >= 0; index--)
                    nodes.Push((current.Node.Children[index], false));
            }
            copiedRoot = completed.Pop();
        }
        TupleTerm? copiedKeys = flatKeys is null ? null : new TupleTerm(flatKeys.Select(index => values[index]));

        return NativeMapMaterialization505.PublishCopied510(
            runtime,
            layout,
            physical.Select(Row).ToArray(),
            copiedRoot,
            canonical.Select(Row).ToArray(),
            arithmetic.Select(Row).ToArray(),
            copiedKeys
        );
    }
}
