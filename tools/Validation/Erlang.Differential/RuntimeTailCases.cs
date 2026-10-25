namespace Erlang.Differential;

public static class RuntimeTailCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } =
    [
        Create("map-get", "maps:get(key,#{key=>done})"),
        Create("list-last", "lists:last([first,done])"),
        Create("case", "case kept of kept->maps:get(key,#{key=>done}) end"),
        Create("receive", "self()!kept,receive kept->lists:last([first,done]) end"),
        Create("effects", "put(trace,[]),done=maps:get(push(key),push(#{key=>done})),true=([#{key=>done},key]=:=get(trace)),done"),
        Create("non-tail", "done=maps:get(key,#{key=>done}),done"),
        Create("missing-module", "try missing_runtime_tail:absent() of _->error(accepted) catch error:undef->done end"),
        Create("map-error", "try maps:get(missing,#{}) of _->error(accepted) catch error:{badkey,missing}->done end")
    ];

    private static CompiledModuleCase Create(string name, string body) => new(
        "compiled-runtime-tail-" + name,
        "-module(runtime_tail_contract). -export([run/0]). run()->" + body + ". push(X)->put(trace,[X|get(trace)]),X.",
        Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
    );
}
