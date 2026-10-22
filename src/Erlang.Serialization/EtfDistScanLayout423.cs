namespace Erlang;

internal static class EtfDistScanLayout423
{
    internal const int MaximumTupleArity = (1 << 24) - 1;
    internal const int LegacyPidTail = EtfFieldWidths.UInt32Bytes * 2 + 1;
    internal const int PidTail = EtfFieldWidths.UInt32Bytes * 3;
    internal const int LegacyPortTail = EtfFieldWidths.UInt32Bytes + 1;
    internal const int PortTail = EtfFieldWidths.UInt32Bytes * 2;
    internal const int WidePortTail = EtfFieldWidths.UInt64Bytes + EtfFieldWidths.UInt32Bytes;
    internal const int LegacyReferenceTail = EtfFieldWidths.UInt32Bytes + 1;
    internal const int NewFunctionPrefix = EtfFunctionLayout.SizeBytes + 1 + EtfFunctionLayout.UniqueBytes + EtfFieldWidths.UInt32Bytes;
    internal const int InternalAtom3Bytes = 3;
}
