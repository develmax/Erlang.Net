namespace Erlang.NifNativeBackend;

public static class HeapFunctionCaseNames
{
    public const string SourceBirth = "runtime/statistics-canonical-source-fun-real-entry-environment-birth-and-gc";
    internal const string Module = "canonical_fun_birth";
    internal const string Function = "run";
    internal const string ProbeModule = "canonical_fun_probe";
    internal const string ProbeFunction = "observe";
    internal const string HeapBlocks = "blocks";
    internal const string SourceFile = "canonical_fun_birth.erl";
    internal const nuint FunSubtag = 0x14;
    internal const int HeaderWordsWithOneFree = 3;
    internal const int EnvironmentShift = 16;
    internal const int ArityShift = 8;
    internal const string Source = """
-module(canonical_fun_birth).
-export([run/1]).
run(N) ->
    Before = canonical_fun_probe:observe(),
    K = {N, N + 1},
    F = fun() -> true = erlang:garbage_collect(), K end,
    true = erlang:garbage_collect(),
    Live = canonical_fun_probe:observe(),
    V = F(),
    true = erlang:garbage_collect(),
    After = canonical_fun_probe:observe(),
    {Before, Live, After, V}.
""";
}
