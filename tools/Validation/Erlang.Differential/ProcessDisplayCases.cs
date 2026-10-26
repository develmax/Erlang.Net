namespace Erlang.Differential;

public static class ProcessDisplayCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    public static string? FixtureName(string function, int arity) => function == ProcessDisplayNames.Function && arity == 2 ? "erlang-process-display-self" : null;

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();
        const string prepare = "S=self(),{P,M}=spawn_monitor(fun()->S!ready,receive stop->ok end end),receive ready->ok end";
        const string finish = "P!stop,receive {'DOWN',M,process,P,normal}->ok after 5000->error(no_down) end";

        void Add(string name, string body, bool prints) => cases.Add(new(
            "erlang-process-display-" + name,
            "-module(process_display_contract). -export([run/0]). run()->" + body + ",done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        )
        {
            ExpectedStandardOutputHex = prints ? null : "",
            ExpectedStandardErrorHex = prints ? null : ""
        });

        Add("self", "true=erlang:process_display(self(),backtrace)", true);
        Add("owner", prepare + ",true=erlang:process_display(P,backtrace)," + finish, true);
        Add(
            "owner-unrelated-mailbox",
            prepare + ",S!unrelated,true=erlang:process_display(P,backtrace),receive unrelated->ok after 5000->error(lost_message) end," + finish,
            true
        );
        Add(
            "owner-sequence-preserved",
            prepare + ",erlang:seq_trace(label,label),true=erlang:seq_trace_print(seed),true=erlang:process_display(P,backtrace),{label,label}=erlang:seq_trace_info(label),{serial,{0,1}}=erlang:seq_trace_info(serial)," + finish,
            true
        );
        Add(
            "self-sequence-preserved",
            "erlang:seq_trace(label,label),true=erlang:seq_trace_print(seed),true=erlang:process_display(self(),backtrace),{label,label}=erlang:seq_trace_info(label),{serial,{0,1}}=erlang:seq_trace_info(serial)",
            true
        );
        foreach (string type in new[] { "wrong", "[]", "[backtrace]", "42", "{backtrace}", "<<1>>" })
            Add(
                "invalid-type-" + cases.Count,
                "S=self(),T = " + type + ",try erlang:process_display(S,T) of _->error(accepted) catch error:badarg:Stack->[{erlang,process_display,[S,T],Extra}|_]=Stack,{error_info,#{module:=erl_erts_errors,cause:=badopt}}=lists:keyfind(error_info,1,Extra),ok end",
                false
            );
        Add(
            "type-error-precedes-pid",
            "try erlang:process_display(42,wrong) of _->error(accepted) catch error:badarg:Stack->[{erlang,process_display,[42,wrong],Extra}|_]=Stack,{error_info,#{module:=erl_erts_errors,cause:=badopt}}=lists:keyfind(error_info,1,Extra),ok end",
            false
        );
        foreach (string pid in new[] { "42", "self", "[]", "{self()}" })
            Add(
                "invalid-pid-" + cases.Count,
                "P=" + pid + ",try erlang:process_display(P,backtrace) of _->error(accepted) catch error:badarg:Stack->[{erlang,process_display,[P,backtrace],Extra}|_]=Stack,{error_info,Info}=lists:keyfind(error_info,1,Extra),#{module:=erl_erts_errors}=Info,false=maps:is_key(cause,Info),ok end",
                false
            );
        Add(
            "dead-owner",
            "{P,M}=spawn_monitor(fun()->ok end),receive {'DOWN',M,process,P,_}->ok end,try erlang:process_display(P,backtrace) of _->error(accepted) catch error:badarg:Stack->[{erlang,process_display,[P,backtrace],Extra}|_]=Stack,{error_info,Info}=lists:keyfind(error_info,1,Extra),false=maps:is_key(cause,Info),ok end",
            false
        );
        Add(
            "dead-owner-type-precedence",
            "{P,M}=spawn_monitor(fun()->ok end),receive {'DOWN',M,process,P,_}->ok end,try erlang:process_display(P,wrong) of _->error(accepted) catch error:badarg:Stack->[{erlang,process_display,[P,wrong],Extra}|_]=Stack,{error_info,#{cause:=badopt}}=lists:keyfind(error_info,1,Extra),ok end",
            false
        );

        Add(
            "foreign-pid",
            "P=binary_to_term(<<131,103,100,0,10,\"other@host\",0,0,0,1,0,0,0,0,0>>),try erlang:process_display(P,backtrace) of _->error(accepted) catch error:badarg:Stack->[{erlang,process_display,[P,backtrace],Extra}|_]=Stack,{error_info,Info}=lists:keyfind(error_info,1,Extra),false=maps:is_key(cause,Info),ok end",
            false
        );
        Add(
            "old-local-creation",
            "<<131,Node/binary>>=term_to_binary(node()),P=binary_to_term(<<131,88,Node/binary,0:32,0:32,4294967295:32>>),try erlang:process_display(P,backtrace) of _->error(accepted) catch error:badarg:Stack->[{erlang,process_display,[P,backtrace],Extra}|_]=Stack,{error_info,Info}=lists:keyfind(error_info,1,Extra),false=maps:is_key(cause,Info),ok end",
            false
        );

        return cases.AsReadOnly();
    }
}
