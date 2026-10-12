namespace Erlang.Differential;

public static class AnonymousTailCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } =
    [
        Create("qualified", "F=fun()->anonymous_tail_contract:loop(20000) end,F()"),
        Create("local", "F=fun()->loop(20000) end,F()"),
        Create("nested", "F=fun()->G=fun()->loop(2000) end,G() end,F()"),
        Create("capture", "Value=done,F=fun()->identity(Value) end,F()"),
        Create("runtime", "F=fun()->maps:get(key,#{key=>done}) end,F()"),
        Create("effects", "put(trace,[]),F=fun()->pair(push(first),push(second)) end,{first,second,[second,first]}=F(),done"),
        Create("missing", "F=fun()->missing_anon_tail:absent() end,try F() of _->error(accepted) catch error:undef->done end"),
        Create("clause", "F=fun(first)->loop(2000) end,try F(second) of _->error(accepted) catch error:function_clause->done end"),
        Create("arity", "F=fun(X)->identity(X) end,try F() of _->error(accepted) catch error:{badarity,_}->done end")
    ];

    private static CompiledModuleCase Create(string name, string body) => new(
        "compiled-anonymous-static-tail-" + name,
        "-module(anonymous_tail_contract). -export([run/0,loop/1]). run()->" + body +
        ". loop(0)->done;loop(N)->anonymous_tail_contract:loop(N-1). identity(X)->X. pair(A,B)->{A,B,get(trace)}. push(X)->put(trace,[X|get(trace)]),X.",
        Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
    );
}
