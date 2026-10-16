namespace Erlang.Tests;

internal static class NonTailStackLifecycleHostNames
{
    internal const string Test = "host_non_tail_stack_async_leaf_old_image_and_lease_cleanup";
    internal const string Broken = "Non-tail recursion changed async leaf, old source image, cleanup or lease lifetime.";
    internal const string Module = "oracle_stack_lifecycle";
    internal const string Bridge = "stack_lifecycle_bridge";
    internal const string Run = "run";
    internal const string Held = "held";
    internal const string Cleanup = "cleanup";
    internal const string RegisterIntrinsic = "RegisterIntrinsic";
    internal const string IsExecutingCode = "IsExecutingCode";
    internal const string PreparePurge = "PreparePurge";
    internal const string FinishPurge = "FinishPurge";
    internal const string Kill = "kill";
    internal const string Killed = "killed";
    internal const string Fault = "stack_lifecycle_fault";
    internal const int Depth = 256;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(15);
    internal const string NamedSource = """
        -module(oracle_stack_lifecycle).
        -export([run/1]).
        run(N)->walk(N).
        walk(0)->stack_lifecycle_bridge:held();
        walk(N)->try walk(N-1)+1 after stack_lifecycle_bridge:cleanup() end.
        """;
    internal const string AnonymousSource = """
        -module(oracle_stack_lifecycle).
        -export([run/1]).
        run(N)->
            Walk=fun(_,0)->stack_lifecycle_bridge:held();
                    (Self,Left)->try Self(Self,Left-1)+1 after stack_lifecycle_bridge:cleanup() end
                 end,
            Walk(Walk,N).
        """;
    internal const string ReplacementSource = """
        -module(oracle_stack_lifecycle).
        -export([run/1]).
        run(_)->replacement.
        """;
}
