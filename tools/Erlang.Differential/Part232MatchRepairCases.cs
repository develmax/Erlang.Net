namespace Erlang.Differential;

public static class Part232MatchRepairCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var modules = new List<CompiledModuleCase>();

        void Add(string name, string body) => modules.Add(new(
            "erlang-match-repair-" + name,
            "-module(match_repair_contract). -export([run/0]). run()->" + body + ",done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));
        Add(
            "table-empty-diagnostic",
            "{error,[{error,\"Body clause does not return anything.\"}]}=erlang:match_spec_test(any,[{'_',[],[]}],table)"
        );
        Add("trace-empty-body-allowed", "{ok,true,[],[]}=erlang:match_spec_test([], [{'_',[],[]}],trace)");
        Add("table-nonempty-returns-empty-list", "{ok,[],[],[]}=erlang:match_spec_test(any,[{'_',[],[{const,[]}]}],table)");
        Add(
            "trace-return-exception-flags",
            "{ok,true,[return_trace,exception_trace],[]}=erlang:match_spec_test([], [{'_',[],[{return_trace},{exception_trace}]}],trace)"
        );
        foreach (string action in new[] { "return_trace", "exception_trace" })
        {
            Add(
                "send-" + action + "-flags-ignored",
                "S=self(),P=spawn(fun()->receive go->S!payload end,receive stop->ok end end),MS=[{'_',[],[{" + action + "}]}],1=erlang:trace_pattern(send,MS),try 1=erlang:trace(P,true,[send,{tracer,S}]),P!go,receive payload->ok end,receive {trace,P,send,payload,S}->ok end,R=erlang:trace_delivered(P),receive {trace_delivered,P,R}->ok end,receive {trace,P,return_from,_,_}->error(unexpected_return);{trace,P,exception_from,_,_}->error(unexpected_exception) after 0->ok end after 1=erlang:trace_pattern(send,false),1=erlang:trace(P,false,[send]),P!stop end"
            );
            Add(
                "receive-" + action + "-flags-ignored",
                "S=self(),P=spawn(fun()->receive payload->S!ready end,receive stop->ok end end),MS=[{'_',[],[{" + action + "}]}],1=erlang:trace_pattern('receive',MS),try 1=erlang:trace(P,true,['receive',{tracer,S}]),P!payload,receive ready->ok end,receive {trace,P,'receive',payload}->ok end,R=erlang:trace_delivered(P),receive {trace_delivered,P,R}->ok end,receive {trace,P,return_from,_,_}->error(unexpected_return);{trace,P,exception_from,_,_}->error(unexpected_exception) after 0->ok end after 1=erlang:trace_pattern('receive',false),1=erlang:trace(P,false,['receive']),P!stop end"
            );
        }
        Add(
            "nested-variable-map-key-before-empty-body",
            "{error,[{error,\"Variable found in map key.\"}]}=erlang:match_spec_test({#{}},[{{#{'$1'=>a}},[],[]}],table)"
        );
        Add(
            "nested-underscore-map-key-before-empty-body",
            "{error,[{error,\"Underscore found in map key.\"}]}=erlang:match_spec_test({#{}},[{{#{'_'=>a}},[],[]}],table)"
        );

        return modules.AsReadOnly();
    }
}
