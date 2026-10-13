namespace Erlang.Differential;

public static class SpawnRequestWrapperCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();
        const string module = "spawn_request_wrapper_contract";
        const string success = ",true=is_reference(R),receive {spawn_reply,R,ok,P}->M=monitor(process,P),P!stop,receive {'DOWN',M,process,P,normal}->ok after 5000->error(no_down) end after 5000->error(no_reply) end";
        const string disconnected = ",true=is_reference(R),receive {spawn_reply,R,error,noconnection}->ok after 5000->error(no_reply) end";

        void Add(string name, string body) => cases.Add(new(
            "erlang-spawn-request-wrapper-" + name,
            "-module(" + module + "). -export([run/0,wait/0]). run()->" + body + ",done. wait()->receive stop->ok end.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));

        Add("atom-third-local-success", "R=spawn_request(node()," + module + ",wait)" + success);
        Add(
            "integer-third-local-badarg-original-frame",
            "try spawn_request(" + module + ",empty,42) of _->error(accepted) catch error:badarg:Stack->[{erlang,spawn_request,[" + module + ",empty,42],_}|_]=Stack,ok end"
        );
        Add(
            "improper-third-local-badarg-original-frame",
            "try spawn_request(" + module + ",empty,[a|tail]) of _->error(accepted) catch error:badarg:Stack->[{erlang,spawn_request,[" + module + ",empty,[a|tail]],_}|_]=Stack,ok end"
        );
        Add("atom-third-four-argument-remote", "R=spawn_request(" + module + ",empty,wrong,[])" + disconnected);
        Add("four-argument-local-success", "R=spawn_request(node()," + module + ",wait,[])" + success);
        Add("five-argument-local-redispatch", "R=spawn_request(node()," + module + ",empty,wrong,[])" + disconnected);
        Add(
            "five-argument-remote-badarg-original-frame",
            "try spawn_request(disconnected," + module + ",empty,wrong,[]) of _->error(accepted) catch error:badarg:Stack->[{erlang,spawn_request,[disconnected," + module + ",empty,wrong,[]],_}|_]=Stack,ok end"
        );
        Add("four-argument-local-redispatch", "R=spawn_request(node()," + module + ",empty,wrong)" + disconnected);

        Add(
            "callback-two-inherits-cause",
            "F=fun()->ok end,try spawn_request(F,wrong) of _->error(accepted) catch error:badarg:Stack->[{erlang,spawn_request,[F,wrong],Extra}|_]=Stack,{error_info,#{module:=erl_erts_errors,cause:=badopt}}=lists:keyfind(error_info,1,Extra),ok end"
        );
        Add(
            "callback-three-drops-cause",
            "N=node(),F=fun()->ok end,try spawn_request(N,F,wrong) of _->error(accepted) catch error:badarg:Stack->[{erlang,spawn_request,[N,F,wrong],Extra}|_]=Stack,{error_info,Info}=lists:keyfind(error_info,1,Extra),#{module:=erl_erts_errors}=Info,false=maps:is_key(cause,Info),ok end"
        );

        return cases.AsReadOnly();
    }
}
