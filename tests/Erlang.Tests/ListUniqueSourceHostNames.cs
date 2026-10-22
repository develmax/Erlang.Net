namespace Erlang.Tests;

internal static class ListUniqueSourceHostNames
{
    internal const string Fixture = "lists_unique_source_contract.erl";
    internal const string TestName = "runtime/lists-unique-source-merge-trampoline-and-argument-frames";
    internal const string Success = "ok";
    internal const string Error = "error";
    internal const string FunctionClause = "function_clause";
    internal const string Lists = "lists";
    internal const string File = "file";
    internal const string Line = "line";
    internal const string ListsFile = "lists.erl";
    internal const string UniqueSort = "usort";
    internal const string MergeLoop = "umergel";
    internal const string MergeLeft = "umerge2_1";
    internal const string Ascending = "asc";
    internal const string AtomA = "a";
    internal const string Tail = "tail";
    internal const string Raw = "raw";
    internal const string Left = "left";
    internal const string Right = "right";
    internal const string Failure = "Source unique sort/merge changed ordering, representatives, source helper arguments or continuation metadata.";
    internal const int UnionRange = 66;
    internal static readonly string[] Functions = [UniqueSort, MergeLoop, MergeLeft];
    internal static readonly int[] Lines = [2075, 3287, 3455];
    internal const int MergeContinuationLine = 3306;
}
