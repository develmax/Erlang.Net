namespace Erlang.Tests;

internal static class ListUniqueSortSourceHostNames
{
    internal const string IdentityTest = "runtime/lists-usort-source-numeric-representatives-and-small-path-sharing";
    internal const string MalformedTest = "runtime/lists-usort-source-all-private-malformed-tail-frames";
    internal const string DeepTest = "runtime/lists-usort-source-deep-split-merge-trampoline";
    internal const string ReverseTest = "runtime/lists-usort-source-selects-current-reverse-after-admission";
    internal const string Lists = "lists";
    internal const string UniqueSort = "usort";
    internal const string Sort = "sort";
    internal const string Reverse = "reverse";
    internal const string ReplacementFailure = "unique_sort_selected_replacement_reverse";
    internal const string Kill = "kill";
    internal const string UsortHelper = "usort_1";
    internal const string SplitAscending = "usplit_1";
    internal const string SplitAscendingPending = "usplit_1_1";
    internal const string SplitDescending = "usplit_2";
    internal const string SplitDescendingPending = "usplit_2_1";
    internal const string File = "file";
    internal const string Line = "line";
    internal const string Source = "lists.erl";
    internal const string Tail = "unique_sort_improper_tail";
    internal const string FunctionClause = "function_clause";
    internal const string Failure = "Pinned unique sort source algorithm changed result representatives, sharing, malformed suffix arguments or source frames.";
    internal const string DynamicReverseFailure = "Unique sort selected reverse diagnostic: calls={0}; class={1}; reason={2}; reasonValueEqualsMarker={3}; argumentCount={4}; firstKind={5}; secondKind={6}";
    internal const int Cells = 32768;
    internal const int Modulus = 97;
    internal const int Step = 17;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(20);
    internal static readonly string[] ErrorFunctions = [UniqueSort, UsortHelper, SplitAscending, SplitDescending, SplitAscendingPending, SplitDescendingPending];
    internal static readonly int[] ErrorLines = [2075, 2116, 3217, 3250, 3232, 3265];
}
