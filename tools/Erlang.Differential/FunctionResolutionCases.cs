namespace Erlang.Differential;

public static class FunctionResolutionCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } =
    [
        Create("qualified-loaded", "L=erlang:loaded(),true=lists:member(function_resolution_contract,L)", ""),
        Create("private-loaded", "private=loaded()", "loaded()->private."),
        Create("forward", "42=answer()", "answer()->42."),
        Create("recursive", "3=count(3)", "count(0)->0;count(N)->1+count(N-1)."),
        Create("nested-private", "F=fun()->loaded() end,private=F()", "loaded()->private."),
        Create("comprehension-private", "[2,3]=[inc(X)||X<-[1,2]]", "inc(X)->X+1."),
        Create("dynamic-loaded", "F=loaded,L=apply(erlang,F,[]),true=is_list(L)", ""),
        Create("remote-missing", "ok=try erlang:loaded(1) of _->unexpected catch error:undef->ok end", "")
    ];

    private static CompiledModuleCase Create(string name, string body, string definitions) => new(
        "compiler-function-resolution-" + name,
        "-module(function_resolution_contract). -export([run/0]). run()->" + body + ",done." + (definitions.Length == 0 ? string.Empty : " " + definitions),
        Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
    );
}
