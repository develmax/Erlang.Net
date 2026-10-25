namespace Erlang.Differential;

public static class SourceTailCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } =
    [
        Create("qualified", "source_tail_contract:loop(20000)"),
        Create("external-fun", "F=erlang:make_fun(source_tail_contract,loop,1),F(20000)"),
        Create("case", "case kept of kept->source_tail_contract:loop(2000) end"),
        Create("if", "if true->source_tail_contract:loop(2000) end"),
        Create("receive", "self()!kept,receive kept->source_tail_contract:loop(2000) end"),
        Create("begin", "begin kept,source_tail_contract:loop(2000) end"),
        Create("non-tail", "done=source_tail_contract:loop(2000),done"),
        Create("effects", "put(trace,[]),{first,second,[second,first]}=source_tail_contract:pair(push(first),push(second)),done"),
        Create(
            "fun-effects",
            "put(trace,[]),F=erlang:make_fun(source_tail_contract,pair,2),{first,second,[second,first]}=F(push(first),push(second)),done"
        ),
        Create(
            "badarity",
            "F=erlang:make_fun(source_tail_contract,loop,1),try F() of _->error(accepted) catch error:{badarity,_}->done end"
        )
    ];

    private static CompiledModuleCase Create(string name, string body) => new(
        "compiled-source-tail-" + name,
        "-module(source_tail_contract). -export([run/0,loop/1,pair/2]). run()->" + body +
        ". loop(0)->done;loop(N)->source_tail_contract:loop(N-1). pair(A,B)->{A,B,get(trace)}. push(X)->put(trace,[X|get(trace)]),X.",
        Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
    );
}
