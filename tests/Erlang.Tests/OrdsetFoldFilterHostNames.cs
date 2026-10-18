namespace Erlang.Tests;

internal static class OrdsetFoldFilterHostNames
{
    internal const string LateTest = "runtime/ordsets-fold-filter-late-source-failures";
    internal const string IdentityTest = "runtime/ordsets-fold-filter-async-object-identity";
    internal const string DeepTest = "runtime/ordsets-fold-filter-deep-iterative-spines";
    internal const string CancellationTest = "runtime/ordsets-fold-filter-callback-cancellation-cleans-source-scopes";
    internal const string Module = "ordsets";
    internal const string Fold = "fold";
    internal const string Filter = "filter";
    internal const string Lists = "lists";
    internal const string FoldHelper = "foldl_1";
    internal const string Comprehension = "-filter/2-lc$^0/1-0-";
    internal const string LateFailure = "ordset_callback_late_failure";
    internal const string FollowupFailure = "ordset_after_cancel_failure";
    internal const string BadFilter = "bad_filter";
    internal const string BadGenerator = "bad_generator";
    internal const string FunctionClause = "function_clause";
    internal const string Tail = "ordset_improper_tail";
    internal const string File = "file";
    internal const string Line = "line";
    internal const string Source = "lists.erl";
    internal const string Erlang = "erlang";
    internal const string Error = "error";
    internal const string Failure = "Ordsets fold/filter HOST source contract failed.";
    internal const int FoldHelperCallLine = 2470;
    internal const int FilterCallLine = 2527;
    internal const int Cells = 32768;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(20);
}
