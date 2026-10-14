namespace Erlang;

internal static class BeamLiteralDiagnostics
{
    public const string InvalidCapacity = "Literal table capacity exceeds the explicit managed reader policy";
    public const string InvalidCount = "Invalid literal table entry count";
    public const string InvalidExternalTerm = "Invalid external term in literal table";
    public const string InvalidCompressedTable = "Invalid compressed literal table";
}
