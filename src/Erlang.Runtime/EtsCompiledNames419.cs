namespace Erlang;

internal static class EtsCompiledNames419
{
    internal const string Module = "ets", Compile = "match_spec_compile", IsCompiled = "is_compiled_ms", Run = "match_spec_run_r";
    internal const string Foreign = "Compiled ETS programs require their exact live runtime and registry";
    internal const string Conflict = "Compiled ETS native registration refuses conflicting or source-owned entries";
    internal const string Signature = "Compiled ETS native signature was not admitted";
    internal const string MissingCanonical = "Native compiled ETS program requires an implemented canonical guard operation";
    internal const string AsyncCanonical = "Native compiled ETS guard unexpectedly requires async dispatch";
    internal const int ContextReductions = 4000;
    internal static readonly (string Function, int Arity)[] Signatures = [(Compile, 1), (IsCompiled, 1), (Run, 3)];
}
