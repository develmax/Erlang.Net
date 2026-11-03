using System.Collections;

namespace Erlang;

internal sealed class ProcessTermSequence506(
    ProcessTermHeap506 owner,
    long identity,
    ProcessTermSlab506 slab,
    int offset,
    int length,
    int headerWords526 = 0,
    int rawPayloadWords556 = 0,
    bool immediateConsClosure = false,
    bool immutablePlainClosure550 = false,
    bool mutableSequenceToken814 = false,
    bool immutableLiteralOnlyClosure132 = false,
    bool immutableReadonlyCaptureClosure138 = false,
    bool immutableNoBackingClosure192 = false,
    bool immutableNoBinaryClosure192 = false,
    bool emptyLiteralCapture200 = false,
    bool immutableLiteralEdges397 = false,
    bool immutableNativePublishedClosure441 = false
) : IOwnedFloatStorage556, IOwnedIntegerStorage559, IOwnedReferenceStorage562, IOwnedResourceBitsStorage577, IOwnedHeapBitStringStorage, IOwnedFunctionStorage, IOwnedExternalIdentifierStorage, IOwnedNativeRecordStorage
{
    // Immutable construction certificate for Cons-only/immediate-leaf chains.
    // It is not consulted by collectors, and never certifies tuple mutation.
    internal bool HasImmediateConsClosure { get; } = immediateConsClosure;

    // Immutable Cons/Tuple descendants with already-published native headers.
    // This is NOT an empty backing or GC-edge proof.
    internal bool HasImmutableNativePublishedClosure441 { get; } = immutableNativePublishedClosure441;

    // Fresh physical Cons/Tuple graph, with only immediate leaves and already
    // certified live blocks from THIS heap. Collectors still visit every edge.
    internal bool HasImmutablePlainClosure550 { get; } = immutablePlainClosure550;

    // Immutable same-heap numeric/inline-heap-binary closure. This metadata-only
    // proof does not skip binary handle discovery or physical collector edges.
    internal bool HasImmutableNoBackingClosure192 { get; } = immutableNoBackingClosure192;

    // Stricter closure excludes inline heap binaries as well: even their shared
    // zero-byte sentinel remains an actual512 handle unless this flag is true.
    internal bool HasImmutableNoBinaryClosure192 { get; } = immutableNoBinaryClosure192;

    // Actual constructor Capture801(children) succeeded with NO literal lease,
    // and every child had an immutable readonly/no-backing proof. This flag
    // skips only later literal metadata capture after live same-heap admission.
    // It neither grants native authority nor skips backing/collector edges.
    internal bool HasEmptyLiteralCapture200 { get; } = emptyLiteralCapture200;

    // Only immutable Cons/Tuple literal-edge metadata. This does not grant
    // validation, native promotion, binary retention or collector shortcuts.
    internal bool HasImmutableLiteralEdges397 { get; } = immutableLiteralEdges397;

    // Set only by the typed token allocator. An uncertified ordinary tuple
    // is not a mutable state capability merely because its shape matches.
    internal bool IsMutableSequenceToken814 { get; } = mutableSequenceToken814;

    // Constructor proof for EMPTY native-header/binary descendant discovery
    // only. Literal custody, validation and real collectors still walk edges.
    internal bool HasImmutableLiteralOnlyClosure132 { get; } = immutableLiteralOnlyClosure132;

    // Capture/validation only. Ordinary readonly binaries still require REAL
    // header promotions/backing discovery in524/512 and actual collector edges.
    internal bool HasImmutableReadonlyCaptureClosure138 { get; } = immutableReadonlyCaptureClosure138;

    public object Owner => owner;
    public long Identity { get; } = identity;
    public int Count { get; } = length;
    internal ProcessTermSlab506 Slab { get; } = slab;
    internal int Offset { get; } = offset;
    internal int HeaderWords526 { get; } = headerWords526;
    internal int RawPayloadWords556 { get; } = rawPayloadWords556;
    internal int AllocationOffset526 => Offset - HeaderWords526 - RawPayloadWords556;
    internal int AllocationWords526 => checked(Count + HeaderWords526 + RawPayloadWords556);
    public Term this[int index] => owner.Read(this, index);

    public double ReadFloat556() => owner.ReadFloat556(this);

    public System.Numerics.BigInteger ReadInteger559() => owner.ReadInteger559(this);

    public IReadOnlyList<uint> ReadReferenceWords562() => owner.ReadReferenceWords562(this);

    public ResourceBitsRead577 ReadResourceBits577() => owner.ReadResourceBits577(this);

    public int ReadHeapBitLength() => owner.ReadHeapBitLength(this);

    public byte ReadHeapByte(int index) => owner.ReadHeapBitByte(this, index);

    public byte[] CopyHeapBytes() => owner.CopyHeapBitBytes(this);

    public BitString CopyHeapValue255() => owner.CopyHeapValue255(this);

    public BitString CopyResourceValue255() => owner.CopyResourceValue255(this);

    public int ReadFunctionArity() => owner.ReadFunctionArity(this);

    public IReadOnlyList<Term> ReadFunctionEnvironment() => owner.ReadFunctionEnvironment(this);

    public ValueTask<Term> InvokeFunctionBody(ITermExecutionContext context, IReadOnlyList<Term> arguments)
        => owner.InvokeFunctionBody(this, context, arguments);

    public TupleTerm ReadRecordDefinition() => owner.ReadRecordDefinition(this);

    public Atom ReadRecordModule() => owner.ReadRecordModule(this);

    public Atom ReadRecordName() => owner.ReadRecordName(this);

    public bool ReadRecordExported() => owner.ReadRecordExported(this);

    public IReadOnlyList<Atom> ReadRecordFieldNames() => owner.ReadRecordFieldNames(this);

    public IReadOnlyList<Term> ReadRecordValues() => owner.ReadRecordValues(this);

    public LocalNodeSnapshot ReadExternalIdentity() => owner.ReadExternalIdentity(this);

    public ulong ReadExternalId() => owner.ReadExternalId(this);

    public IReadOnlyList<uint> ReadExternalReferenceWords() => owner.ReadExternalReferenceWords(this);

    public IEnumerator<Term> GetEnumerator()
    {
        for (int index = 0; index < Count; index++)
            yield return this[index];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
