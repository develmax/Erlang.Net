namespace Erlang.Tests;

internal static class OriginalAtomics590SyntheticNames
{
    internal const string ForeignNode = "foreign@host";
    internal const string ChangedStack = "changed_stack";
    internal const string SameStack = "same_stack";
    internal const string Failure = "The atomics own-reference comparator negative control failed.";
    internal const uint ForeignCreation = 1;
    internal const int AliasFailureCaseIndex = 1;
    internal static uint[] SourceWords => [1, 0x20001, 3];
    internal static uint[] TargetWords => [2, 0x20000, 4];
    internal static uint[] AlternateWords => [3, 0x20000, 5];
    internal static uint[] AliasMarkedWords => [1, 0x10000, 4];
}
