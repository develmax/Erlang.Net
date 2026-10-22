using System.Numerics;

namespace Erlang;

// Logical language storage words. These are not CLR object sizes or native
// addresses. Each actual Term wrapper is a node; payload bytes are excluded.
internal static class DebugSharingWordLayout425
{
    internal const int WordBits = 64;
    internal const int SmallBits = 59;
    internal const int Header = 1;
    internal const int ConsWords = 2;
    internal const int Float = 2;
    internal const int Function = 2;
    internal const int BinaryHeader = 2;
    internal const int ReferenceBinaryWrapper = 8;
    internal const int LocalReference = 3;
    internal const int PidReference = 4;
    internal const int MagicReference = 3;
    internal const int ExternalIdentity = 4;
    internal const int ExternalReferenceHeader = 3;
    internal const int HalfWords = 2;
    internal const int MapHeader = 3;
    internal const int MapEntry = 2;
    private static readonly BigInteger SmallBound = BigInteger.One << SmallBits;

    internal static bool Immediate(Term value) => value switch
    {
        Nil or Atom => true,
        TupleTerm { HasGlobalEmptyStorage428: true } => true,
        Integer integer => integer.Value >= -SmallBound && integer.Value < SmallBound,
        Pid pid => pid.LocalIdentity is not null,
        PortTerm port => port.LocalIdentity is not null,
        _ => false
    };

    internal static ulong OwnWords(Term value) => value switch
    {
        _ when Immediate(value) => 0,
        Integer integer => Header + (ulong)((BigInteger.Abs(integer.Value).GetBitLength() + WordBits - 1) / WordBits),
        FloatTerm => Float,
        Cons => ConsWords,
        TupleTerm tuple => checked((ulong)tuple.Items.Count + Header),
        NativeRecordTerm record => checked((ulong)record.Values.Count + Header),
        HamtBitmapTerm429 node => checked(Header + (ulong)node.Children.Count),
        MapTerm map => checked(MapHeader + (ulong)map.Entries.Count * (map.FlatKeys426 is null ? MapEntry : 1UL)),
        FunctionTerm function => checked(Function + (ulong)function.Environment.Count),
        BitString binary => binary.HasReferenceStorage425
            ? ReferenceBinaryWrapper : BinaryHeader + (ulong)(binary.BitLength + WordBits - 1L) / WordBits,
        ResourceReferenceTerm => MagicReference,
        ReferenceTerm reference when reference.LocalIdentity is not null => (ulong)(reference.Words.Count > LocalReference ? PidReference : LocalReference),
        ReferenceTerm reference => ExternalReferenceHeader + ((ulong)reference.Words.Count + HalfWords - 1) / HalfWords,
        Pid or PortTerm => ExternalIdentity,
        _ => throw new NotSupportedException(PersistentArenaNames416.Unsupported)
    };
}
