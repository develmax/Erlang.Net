namespace Erlang.NifNativeBackend;

public static class HeapFunctionImportCaseNames
{
    public const string SourceImport = "runtime/statistics-source-fun-import-actual-copied-env-dictionary-and-reclaim";
    internal const string Module = "prepared_fun_import";
    internal const string Make = "make";
    internal const string Take = "take";
    internal const string SourceFile = "prepared_fun_import.erl";
    internal const string Source = """
-module(prepared_fun_import).
-export([make/1,take/1]).
make(N) -> K = {N, N + 1}, fun() -> K end.
take(F) ->
    true = erlang:garbage_collect(),
    Before = canonical_fun_probe:observe(),
    V = F(),
    undefined = erlang:put(saved_imported_fun, F),
    D = erlang:get(saved_imported_fun),
    true = erlang:garbage_collect(),
    Stored = canonical_fun_probe:observe(),
    W = D(),
    D = erlang:erase(saved_imported_fun),
    {Before,Stored,V,W}.
""";
}
