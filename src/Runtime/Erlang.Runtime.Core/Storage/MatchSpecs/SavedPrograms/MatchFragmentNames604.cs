namespace Erlang;

internal static class MatchFragmentNames604
{
    internal const int WordBytes = 8;
    internal const int PrefixBytes = 40;
    internal const int PrimaryMask = 3;
    internal const ulong ListTag = 1;
    internal const ulong BoxedTag = 2;
    internal const ulong SmallTag = 15;
    internal const ulong SmallMask = 15;
    internal const int SmallShift = 4;
    internal const int AtomShift = 6;
    internal const ulong AtomTag = 11;
    internal const ulong AtomMask = 63;
    internal const ulong Nil = 59;
    internal const int ArityShift = 6;
    internal const int MaximumNodes = 1_048_576;
    internal const long MinimumSmall = -(1L << 59);
    internal const long MaximumSmall = (1L << 59) - 1;
    internal const string Closed = "Native match fragment604 is closed.";
    internal const string Invalid = "Invalid native match fragment604 word or custody.";
}
