using System.Numerics;
using System.Runtime.CompilerServices;

namespace Erlang;

internal sealed partial class RuntimeEtsStoredPayload : IDisposable
{
    private RuntimeEtsStoredNode[]? nodes;
    private RuntimeEtsCompressed438? compressed438;

    private RuntimeEtsStoredPayload(RuntimeEtsStoredNode[] nodes)
    {
        this.nodes = nodes;
        long bytes = checked((long)nodes.Length * Unsafe.SizeOf<RuntimeEtsStoredNode>());
        foreach (var node in nodes)
            bytes = checked(bytes + (node.Bytes?.LongLength ?? 0)
                + (node.Children?.LongLength ?? 0) * sizeof(int)
                + (node.Names?.LongLength ?? 0) * IntPtr.Size);
        ArrayPayloadBytes = bytes;
    }

    internal long ArrayPayloadBytes { get; }

    internal static RuntimeEtsStoredPayload Capture(
        ProcessRuntime owner,
        Term root,
        CancellationToken cancellation,
        bool compressed = false,
        long keyPosition = 1
    )
    {
        if (compressed && NativeResourceRoots524.Contains524(root))
            throw new NotSupportedException(NativeResource524Names.CompressedStorage);
        var indices = new Dictionary<Term, int>(ReferenceEqualityComparer.Instance);
        var completed = new HashSet<Term>(ReferenceEqualityComparer.Instance);
        var nodes = new List<RuntimeEtsStoredNode>();
        var pending = new Stack<(Term Term, bool Finish)>();
        pending.Push((root, false));
        try
        {
            while (pending.TryPop(out var item))
            {
                cancellation.ThrowIfCancellationRequested();
                if (item.Finish)
                {
                    int[] ChildIds(IEnumerable<Term> terms) => terms.Select(term => indices[term]).ToArray();
                    RuntimeEtsStoredNode node = item.Term switch
                    {
                        Nil => new(RuntimeEtsStoredKind.Nil),
                        Atom atom => new(RuntimeEtsStoredKind.Atom, Names: [atom.Name]),
                        Integer integer => new(RuntimeEtsStoredKind.Integer, Bytes: integer.Value.ToByteArray()),
                        FloatTerm number => new(RuntimeEtsStoredKind.Float, FloatBits: BitConverter.DoubleToInt64Bits(number.Value)),
                        BitString binary when binary.NativeResourceIdentity524 is not null => new(
                            RuntimeEtsStoredKind.NativeBinary524, NativeBinary524: binary.CapturePersistentSlice416()),
                        BitString binary => new(RuntimeEtsStoredKind.Binary, Bytes: binary.ToArray(), BitLength: binary.BitLength),
                        Cons list => new(RuntimeEtsStoredKind.Cons, Children: [indices[list.Head], indices[list.Tail]]),
                        TupleTerm tuple => new(RuntimeEtsStoredKind.Tuple, Children: ChildIds(tuple.Items)),
                        MapTerm map => new(RuntimeEtsStoredKind.Map, Children: ChildIds(map.Entries.SelectMany(pair => new[] { pair.Key, pair.Value }))),
                        NativeRecordTerm record => new(
                            RuntimeEtsStoredKind.NativeRecord,
                            Children: ChildIds(record.Values),
                            Names: new[] { record.Module.Name, record.Name.Name }.Concat(record.FieldNames.Select(field => field.Name)).ToArray(),
                            Exported: record.IsExported
                        ),
                        FunctionTerm function => new(
                            RuntimeEtsStoredKind.Function,
                            Children: ChildIds(function.Environment),
                            Function: RuntimeStoredFunctionRecipes.Capture(owner, function)
                        ),
                        Pid or ReferenceTerm or PortTerm => new(RuntimeEtsStoredKind.Identity, Identity: owner.ReserveExternalTerm(item.Term)),
                        _ => throw new ArgumentException(RuntimeEtsFoundationNames.UnsupportedTerm)
                    };
                    nodes[indices[item.Term]] = node;
                    completed.Add(item.Term);
                    continue;
                }
                if (completed.Contains(item.Term)) continue;
                if (indices.ContainsKey(item.Term)) throw new ArgumentException(RuntimeEtsFoundationNames.CyclicTerm);
                // Authenticate exact original BEAM factory ownership, never public metadata or delegate captures.
                if (item.Term is FunctionTerm authenticatedFunction) _ = RuntimeStoredFunctionRecipes.Capture(owner, authenticatedFunction);
                if (item.Term is not (Nil or Atom or Integer or FloatTerm or BitString or Cons or TupleTerm or MapTerm or NativeRecordTerm or FunctionTerm or Pid or ReferenceTerm or PortTerm))
                    throw new ArgumentException(RuntimeEtsFoundationNames.UnsupportedTerm);
                indices.Add(item.Term, nodes.Count);
                nodes.Add(default);
                pending.Push((item.Term, true));
                IEnumerable<Term> children = item.Term switch
                {
                    Cons list => [list.Head, list.Tail],
                    TupleTerm tuple => tuple.Items,
                    MapTerm map => map.Entries.SelectMany(pair => new[] { pair.Key, pair.Value }),
                    NativeRecordTerm record => record.Values,
                    FunctionTerm function => function.Environment,
                    _ => []
                };
                foreach (Term child in children.Reverse()) pending.Push((child, false));
            }
            cancellation.ThrowIfCancellationRequested();

            RuntimeEtsStoredNode[] owned = nodes.ToArray();

            return compressed ? new(EtsCompressedWriter438.Encode(owned, keyPosition, cancellation)) : new(owned);
        }
        catch
        {
            foreach (var node in nodes)
            {
                node.Identity?.Dispose();
                node.NativeBinary524?.Backing?.Dispose();
            }
            throw;
        }
    }

    internal Term Materialize(CancellationToken cancellation)
    {
        RuntimeEtsCompressed438? compressed = Volatile.Read(ref compressed438);
        RuntimeEtsStoredNode[] owned = compressed is null
            ? Volatile.Read(ref nodes) ?? throw new ObjectDisposedException(nameof(RuntimeEtsStoredPayload))
            : EtsCompressedReader438.Decode(compressed, cancellation);

        return MaterializeNodes438(owned, 0, cancellation);
    }

    internal static Term MaterializeNodes438(RuntimeEtsStoredNode[] owned, int root, CancellationToken cancellation)
    {
        var values = new Term?[owned.Length];
        var pending = new Stack<(int Index, bool Finish)>();
        pending.Push((root, false));
        while (pending.TryPop(out var item))
        {
            cancellation.ThrowIfCancellationRequested();
            if (values[item.Index] is not null) continue;
            var node = owned[item.Index];
            if (!item.Finish)
            {
                pending.Push((item.Index, true));
                if (node.Children is { } children)
                    foreach (int child in children.Reverse()) pending.Push((child, false));
                continue;
            }

            IEnumerable<Term> Children() => node.Children!.Select(index => values[index]!);
            values[item.Index] = node.Kind switch
            {
                RuntimeEtsStoredKind.Nil => Nil.Value,
                RuntimeEtsStoredKind.Atom => Term.A(node.Names![0]),
                RuntimeEtsStoredKind.Integer => new Integer(new BigInteger(node.Bytes!)),
                RuntimeEtsStoredKind.Float => new FloatTerm(BitConverter.Int64BitsToDouble(node.FloatBits)),
                RuntimeEtsStoredKind.NativeBinary524 => OwnedTermInvocation506.AdmitNativeStoredResult524(
                    node.NativeBinary524!.Value.Backing!.Materialize(
                        node.NativeBinary524.Value.BitOffset, node.NativeBinary524.Value.BitLength)),
                RuntimeEtsStoredKind.Binary => new BitString(node.Bytes!, node.BitLength),
                RuntimeEtsStoredKind.Cons => new Cons(values[node.Children![0]]!, values[node.Children[1]]!),
                RuntimeEtsStoredKind.Tuple => new TupleTerm(Children()),
                RuntimeEtsStoredKind.Map => new MapTerm(Enumerable.Range(0, node.Children!.Length / 2)
                    .Select(index => new KeyValuePair<Term, Term>(values[node.Children[index * 2]]!, values[node.Children[index * 2 + 1]]!))),
                RuntimeEtsStoredKind.NativeRecord => new NativeRecordTerm(
                    Term.A(node.Names![0]),
                    Term.A(node.Names[1]),
                    node.Exported,
                    node.Children!.Select((child, index) => new KeyValuePair<Atom, Term>(Term.A(node.Names[index + 2]), values[child]!))
                ),
                RuntimeEtsStoredKind.Function => node.Function!.Rebind(Children().ToArray()),
                RuntimeEtsStoredKind.Identity => OwnedTermInvocation506.AdmitNativeStoredResult524(node.Identity!.Value),
                _ => throw new InvalidOperationException()
            };
        }

        return values[root]!;
    }

    public void Dispose()
    {
        Interlocked.Exchange(ref compressed438, null);
        RuntimeEtsStoredNode[]? retired = Interlocked.Exchange(ref nodes, null);
        if (retired is null) return;
        var failures = new List<Exception>(retired.Length);
        foreach (RuntimeEtsStoredNode node in retired)
        {
            try
            {
                node.Identity?.Dispose();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            try
            {
                node.NativeBinary524?.Backing?.Dispose();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        }
        if (failures.Count != 0) throw new AggregateException(failures);
    }
}
