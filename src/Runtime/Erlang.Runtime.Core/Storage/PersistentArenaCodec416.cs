using System.Numerics;

namespace Erlang;

internal static class PersistentArenaCodec416
{
    internal static PersistentArenaImage416 Encode(ProcessRuntime owner, Term root, CancellationToken cancellation)
    {
        using var roots = new PersistentArenaRootBuilder416();
        using var output = new MemoryStream();
        using var writer = new BinaryWriter(output);
        writer.Write(PersistentArenaNames416.Magic);
        for (int index = 0; index < 4; index++) writer.Write(0UL);
        var offsets = new Dictionary<Term, ulong>(ReferenceEqualityComparer.Instance);
        var active = new HashSet<Term>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<(Term Value, PersistentArenaNode416? Description)>();
        pending.Push((root, null));
        while (pending.TryPop(out var work))
        {
            cancellation.ThrowIfCancellationRequested();
            if (offsets.ContainsKey(work.Value)) continue;
            if (work.Description is null)
            {
                if (!active.Add(work.Value)) throw new InvalidDataException(PersistentArenaNames416.Cycle);
                PersistentArenaNode416 description = Describe(owner, work.Value, roots);
                pending.Push((work.Value, description));
                for (int index = description.Children.Length - 1; index >= 0; index--)
                    pending.Push((description.Children[index], null));
                continue;
            }
            PersistentArenaNode416 node = work.Description;
            ulong position = checked((ulong)output.Position);
            writer.Write((int)node.Kind);
            writer.Write(node.Data.Length);
            writer.Write(node.Children.Length);
            writer.Write(node.Root);
            writer.Write(node.Auxiliary);
            writer.Write(node.Data);
            foreach (Term child in node.Children) writer.Write(offsets[child]);
            offsets.Add(work.Value, position);
            active.Remove(work.Value);
        }
        cancellation.ThrowIfCancellationRequested();
        ulong extent = checked((ulong)output.Length);
        if (extent > (ulong)Array.MaxLength) throw new OutOfMemoryException();
        output.Position = sizeof(ulong);
        writer.Write(extent);
        writer.Write(offsets[root]);
        writer.Write(checked((ulong)offsets.Count));
        byte[] bytes = output.ToArray();
        PersistentArenaRoots416 bundle = roots.Seal();
        try
        {
            System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(PersistentArenaNames416.RootCountOffset), checked((ulong)bundle.Count));

            return new(bytes, bundle);
        }
        catch
        {
            bundle.Dispose();
            throw;
        }
    }

    internal static Term Decode(byte[] bytes, PersistentArenaRoots416 roots, CancellationToken cancellation)
        => DecodeSelected591(
            bytes,
            roots,
            cancellation,
            true,
            false
        );

    internal static Term DecodeSelected591(
        byte[] bytes,
        PersistentArenaRoots416 roots,
        CancellationToken cancellation,
        bool admitResults,
        bool valueOnly
    ) => DecodeSelected594(
        bytes,
        roots,
        cancellation,
        admitResults,
        valueOnly,
        null
    );

    internal static Term DecodeSelected594(
        byte[] bytes,
        PersistentArenaRoots416 roots,
        CancellationToken cancellation,
        bool admitResults,
        bool valueOnly,
        AtomicsHostCopyTransaction591? transaction
    )
    {
        Require(bytes.Length >= PersistentArenaNames416.HeaderBytes);
        using var input = new MemoryStream(bytes, false);
        using var reader = new BinaryReader(input);
        Require(reader.ReadUInt64() == PersistentArenaNames416.Magic);
        Require(reader.ReadUInt64() == checked((ulong)bytes.Length));
        ulong root = reader.ReadUInt64();
        ulong count = reader.ReadUInt64();
        Require(reader.ReadUInt64() == checked((ulong)roots.Count));
        // Select the actual escaping graph before any leaf admission. Still
        // decode/validate ALL nodes below; comparison mode admits no outputs.
        HashSet<ulong>? escaping = admitResults
            ? PersistentArenaSelection591.Escaping(
                bytes,
                root,
                valueOnly,
                cancellation
            ) : null;
        var decoded = new Dictionary<ulong, Term>();
        var admittedHosts591 = new List<IDisposable>();
        try
        {
            while (input.Position < input.Length)
            {
                cancellation.ThrowIfCancellationRequested();
                Require(input.Length - input.Position >= PersistentArenaNames416.NodeHeaderBytes);
                ulong position = checked((ulong)input.Position);
                var kind = (PersistentArenaKind416)reader.ReadInt32();
                int length = reader.ReadInt32();
                int children = reader.ReadInt32();
                int external = reader.ReadInt32();
                long auxiliary = reader.ReadInt64();
                Require(length >= 0 && children >= 0
                    && (long)length + (long)children * PersistentArenaNames416.OffsetBytes <= input.Length - input.Position);
                byte[] data = reader.ReadBytes(length);
                var values = new Term[children];
                for (int index = 0; index < children; index++)
                {
                    ulong offset = reader.ReadUInt64();
                    Require(offset < position && decoded.ContainsKey(offset));
                    values[index] = decoded[offset];
                }
                PersistentArenaRoot416? offheap = external == PersistentArenaNames416.MissingRoot ? null : roots.Get(external);
                Term value = kind switch
                {
                    PersistentArenaKind416.Nil when length == 0 && children == 0 && offheap is null => Nil.Value,
                    PersistentArenaKind416.Atom when children == 0 && offheap is null => Atom(data),
                    PersistentArenaKind416.Integer when children == 0 && offheap is null => new Integer(new BigInteger(data)),
                    PersistentArenaKind416.Float when length == 0 && children == 0 && offheap is null => new FloatTerm(BitConverter.Int64BitsToDouble(auxiliary)),
                    PersistentArenaKind416.Cons when length == 0 && children == 2 && offheap is null => new Cons(values[0], values[1]),
                    PersistentArenaKind416.Tuple when length == 0 && offheap is null => new TupleTerm(values),
                    PersistentArenaKind416.Map when length == 0 && children % 2 == 0 && offheap is null => new MapTerm(
                        Enumerable.Range(0, children / 2).Select(index => new KeyValuePair<Term, Term>(values[index * 2], values[index * 2 + 1]))),
                    PersistentArenaKind416.InlineBinary when children == 0 && offheap is null && auxiliary >= 0 && auxiliary <= int.MaxValue
                        => new BitString(data, (int)auxiliary),
                    PersistentArenaKind416.ReferenceBinary when length == sizeof(int) && children == 0 && offheap is not null
                        => offheap.BinarySelected591(auxiliary, System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(data), escaping?.Contains(position) == true),
                    PersistentArenaKind416.NativeRecord when offheap is null => Record(data, values, auxiliary),
                    PersistentArenaKind416.Identity when children == 0 && offheap is not null => offheap.Identifier(data),
                    PersistentArenaKind416.ResourceIdentity when length == 0 && children == 0 && offheap is not null => offheap.IdentitySelected594(escaping?.Contains(position) == true, transaction),
                    PersistentArenaKind416.Function when length == 0 && offheap is not null => Function(offheap, values, auxiliary),
                    _ => throw new InvalidDataException(PersistentArenaNames416.Invalid)
                };
                IDisposable? host = value is ResourceReferenceTerm resource
                    ? (IDisposable?)resource.AtomicsHost586 ?? (IDisposable?)resource.CountersHost596 ?? (IDisposable?)resource.RegexHost597 ?? resource.BinaryHost598 : null;
                if (escaping?.Contains(position) == true && host is not null)
                {
                    try
                    {
                        admittedHosts591.Add(host);
                    }
                    catch (Exception original)
                    {
                        try
                        {
                            host.Dispose();
                        }
                        catch (Exception cleanup)
                        {
                            throw new AggregateException(original, cleanup);
                        }
                        throw;
                    }
                }
                decoded.Add(position, value);
            }
            Require((ulong)decoded.Count == count && decoded.ContainsKey(root));

            return decoded[root];
        }
        catch (Exception original)
        {
            Exception? cleanupFailure = null;
            // A later malformed node/admission failure must not leave hidden
            // finalizable host carriers created by earlier selected leaves.
            foreach (IDisposable host in admittedHosts591)
                try
                {
                    host.Dispose();
                }
                catch (Exception cleanup)
                {
                    cleanupFailure ??= cleanup;
                }
            if (cleanupFailure is not null)
                throw new AggregateException(original, cleanupFailure);
            throw;
        }
    }

    private static Atom Atom(byte[] bytes)
    {
        string[] names = ReadNames(bytes);
        Require(names.Length == 1);

        return Term.A(names[0]);
    }

    private static FunctionTerm Function(PersistentArenaRoot416 root, Term[] environment, long arity)
    {
        FunctionTerm function = root.Function(environment);
        Require(function.Arity == arity);

        return function;
    }

    private static NativeRecordTerm Record(byte[] bytes, Term[] values, long exported)
    {
        string[] names = ReadNames(bytes);
        Require(names.Length == values.Length + 2 && exported is 0 or 1);

        return new(
            Term.A(names[0]),
            Term.A(names[1]),
            exported != 0,
            values.Select((value, index) => new KeyValuePair<Atom, Term>(Term.A(names[index + 2]), value))
        );
    }

    private static PersistentArenaNode416 Describe(ProcessRuntime owner, Term value, PersistentArenaRootBuilder416 roots)
    {
        switch (value)
        {
            case Nil: return new(PersistentArenaKind416.Nil, [], []);
            case Atom atom: return new(PersistentArenaKind416.Atom, Names([atom.Name]), []);
            case Integer integer: return new(PersistentArenaKind416.Integer, integer.Value.ToByteArray(), []);
            case FloatTerm number:
                return new(
                    PersistentArenaKind416.Float,
                    [],
                    [],
                    Auxiliary: BitConverter.DoubleToInt64Bits(number.Value)
                );
            case Cons list: return new(PersistentArenaKind416.Cons, [], [list.Head, list.Tail]);
            case TupleTerm tuple: return new(PersistentArenaKind416.Tuple, [], tuple.Items.ToArray());
            case MapTerm map: return new(PersistentArenaKind416.Map, [], map.Entries.SelectMany(pair => new[] { pair.Key, pair.Value }).ToArray());
            case NativeRecordTerm record:
                return new(
                    PersistentArenaKind416.NativeRecord,
                    Names(new[] { record.Module.Name, record.Name.Name }.Concat(record.FieldNames.Select(name => name.Name)).ToArray()),
                    record.Values.ToArray(),
                    Auxiliary: record.IsExported ? 1 : 0
                );
            case BitString binary:
                PersistentBinarySlice416 slice = binary.CapturePersistentSlice416();

                return slice.Backing is null
                    ? new(
                        PersistentArenaKind416.InlineBinary,
                        binary.ToArray(),
                        [],
                        Auxiliary: binary.BitLength
                    )
                    : new(
                        PersistentArenaKind416.ReferenceBinary,
                        Int32(slice.BitLength),
                        [],
                        roots.Binary(slice.Backing),
                        slice.BitOffset
                    );
            case ResourceReferenceTerm:
                return new(
                    PersistentArenaKind416.ResourceIdentity,
                    [],
                    [],
                    roots.Identity(owner, value)
                );
            case Pid or ReferenceTerm or PortTerm:
                return new(
                    PersistentArenaKind416.Identity,
                    PersistentIdentifier416.Encode(value),
                    [],
                    roots.Identifier(owner, value)
                );
            case FunctionTerm function:
                return new(
                    PersistentArenaKind416.Function,
                    [],
                    function.Environment.ToArray(),
                    roots.Function(owner, function),
                    function.Arity
                );
            default: throw new NotSupportedException(PersistentArenaNames416.Unsupported);
        }
    }

    private static byte[] Int32(int value)
    {
        var bytes = new byte[sizeof(int)];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes, value);

        return bytes;
    }

    private static byte[] Names(string[] names)
    {
        using var memory = new MemoryStream();
        using var writer = new BinaryWriter(memory);
        writer.Write(names.Length);
        foreach (string name in names)
        {
            writer.Write(name.Length);
            foreach (char value in name) writer.Write((ushort)value);
        }

        return memory.ToArray();
    }

    private static string[] ReadNames(byte[] bytes)
    {
        Require(bytes.Length >= sizeof(int));
        using var memory = new MemoryStream(bytes, false);
        using var reader = new BinaryReader(memory);
        int count = reader.ReadInt32();
        Require(count >= 0 && count <= (memory.Length - memory.Position) / sizeof(int));
        var names = new string[count];
        for (int index = 0; index < count; index++)
        {
            int length = reader.ReadInt32();
            Require(length >= 0 && length <= (memory.Length - memory.Position) / sizeof(char));
            var text = new char[length];
            for (int character = 0; character < length; character++) text[character] = (char)reader.ReadUInt16();
            names[index] = new(text);
        }
        Require(memory.Position == memory.Length);

        return names;
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidDataException(PersistentArenaNames416.Invalid);
    }
}
