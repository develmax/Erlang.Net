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
    bool immediateConsClosure = false
) : IOwnedFloatStorage556, IOwnedIntegerStorage559, IOwnedReferenceStorage562, IOwnedResourceBitsStorage577, IOwnedHeapBitStringStorage, IOwnedFunctionStorage, IOwnedExternalIdentifierStorage, IOwnedNativeRecordStorage
{
    // Immutable construction certificate for Cons-only/immediate-leaf chains.
    // It is not consulted by collectors, and never certifies tuple mutation.
    internal bool HasImmediateConsClosure { get; } = immediateConsClosure;

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
