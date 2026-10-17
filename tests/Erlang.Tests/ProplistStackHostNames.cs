namespace Erlang.Tests;

internal static class ProplistStackHostNames
{
    internal const string Test = "proplists-get-value-source-stack-metadata";
    internal const string Module = "proplist_get_value_contract";
    internal const string Observe = "stack_observations";
    internal const string Tail = "tail";
    internal const string NonTail = "non_tail";
    internal const string TailCaller = "observe_stack";
    internal const string NonTailCaller = "invoke_stack";
    internal const string PropertyKey = "key";
    internal const string ImproperTail = "tail";
    internal const string Undefined = "undefined";
    internal const string Fallback = "fallback";
    internal const string File = "file";
    internal const string Line = "line";
    internal const string ProplistFile = "proplists.erl";
    internal const int ProplistLine = 222;
    internal const int TailLine = 78;
    internal const int NonTailTwoLine = 85;
    internal const int NonTailThreeLine = 86;
    internal const int CallerArity = 3;
    internal const string Failure = "Source stack metadata differs from the pinned original twelve observations.";
}
