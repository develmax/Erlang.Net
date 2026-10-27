namespace Erlang;

internal static class CellStorage526Names
{
    internal const int WordBytes = 8;
    internal const byte EmptyKind = 0;
    internal const byte TermKind = 1;
    internal const byte HeaderKind = 2;
    internal const byte NumericDataKind556 = 3;
    internal const string NotNumeric556 = "The requested physical cell is not a raw numeric payload word.";
    internal const string WordSize = "Owned native cell storage requires the admitted 64-bit source profile.";
    internal const string NotHeader = "The requested physical cell is not an encoded header word.";
    internal const string GateRequired = "Native word allocation requires its owning process heap gate.";
    internal const string InvalidHeader = "The stored ERTS header bits disagree with the admitted physical node.";
}
