namespace Erlang.Differential;

public static class ProcessTraceCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    public static string? FixtureName(string function, int arity) => function switch
    {
        ProcessTraceNames.Configure => "erlang-process-trace-send",
        ProcessTraceNames.Info => "erlang-process-trace-config-query",
        ProcessTraceNames.Pattern => "erlang-process-trace-call-" + arity,
        ProcessTraceNames.Delivered => "erlang-process-trace-delivered",
        _ => null
    };

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var result = new List<CompiledModuleCase>();

        void Add(string name, string body) => result.Add(new(
            "erlang-process-trace-" + name,
            "-module(process_trace_contract). -export([run/0,target/1,identity/1]). run()->" + body + ",done. target(S)->receive {send,V}->S!V;{call,V}->S!process_trace_contract:identity(V);{timeout,V}->receive never->ok after 0->S!V end;{dead,Q}->Q!payload,S!sent;stop->ok end. identity(fail)->error(trace_failure);identity(V)->V.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));
        const string setup = "S=self(),P=spawn(process_trace_contract,target,[S])";
        const string barrier = "R=erlang:trace_delivered(P),receive {trace_delivered,P,R}->ok end";
        Add(
            "config-query",
            setup + ",1=erlang:trace(P,true,[send,'receive',{tracer,S}]),{flags,['receive',send]}=erlang:trace_info(P,flags),{tracer,S}=erlang:trace_info(P,tracer),1=erlang:trace(P,false,[send]),{flags,['receive']}=erlang:trace_info(P,flags),1=erlang:trace(P,false,['receive']),{flags,[]}=erlang:trace_info(P,flags),{tracer,[]}=erlang:trace_info(P,tracer),P!stop"
        );
        Add(
            "send",
            setup + ",1=erlang:trace(P,true,[send,{tracer,S}]),P!{send,payload},receive {trace,P,send,payload,S}->ok end,receive payload->ok end," + barrier
        );
        Add(
            "receive-admission",
            setup + ",1=erlang:trace(P,true,['receive',{tracer,S}]),P!unmatched," + barrier + ",receive {trace,P,'receive',unmatched}->ok end,P!stop"
        );
        Add(
            "missing-send",
            setup + ",{Q,M}=spawn_monitor(fun()->ok end),receive {'DOWN',M,process,Q,normal}->ok end,1=erlang:trace(P,true,[send,{tracer,S}]),P!{dead,Q},receive {trace,P,send_to_non_existing_process,payload,Q}->ok end,receive {trace,P,send,sent,S}->ok end,receive sent->ok end," + barrier
        );
        Add(
            "receive-timeout",
            setup + ",1=erlang:trace(P,true,['receive',{tracer,S}]),P!{timeout,payload},receive {trace,P,'receive',{timeout,payload}}->ok end,receive {trace,P,'receive',timeout}->ok end,receive payload->ok end," + barrier
        );
        Add(
            "send-receive-order",
            setup + ",1=erlang:trace(P,true,[send,'receive',{tracer,S}]),P!{send,payload},receive {trace,P,'receive',{send,payload}}->ok end,receive {trace,P,send,payload,S}->ok end,receive payload->ok end," + barrier
        );
        foreach (int arity in new[] { 2, 3 })
            Add(
                "call-" + arity,
                setup + ",1=erlang:trace_pattern({process_trace_contract,identity,1},true" + (arity == 3 ? ",[global]" : "") + "),1=erlang:trace(P,true,[call,{tracer,S}]),P!{call,payload},receive {trace,P,call,{process_trace_contract,identity,[payload]}}->ok end,receive payload->ok end," + barrier + ",1=erlang:trace_pattern({process_trace_contract,identity,1},false)"
            );
        Add(
            "call-arity",
            setup + ",1=erlang:trace_pattern({process_trace_contract,identity,1},[]),1=erlang:trace(P,true,[call,arity,{tracer,S}]),P!{call,payload},receive {trace,P,call,{process_trace_contract,identity,1}}->ok end,receive payload->ok end," + barrier
        );
        Add(
            "function-query",
            "{traced,false}=erlang:trace_info({process_trace_contract,identity,1},traced),{match_spec,false}=erlang:trace_info({process_trace_contract,identity,1},match_spec),1=erlang:trace_pattern({process_trace_contract,identity,1},true),{traced,global}=erlang:trace_info({process_trace_contract,identity,1},traced),{match_spec,[]}=erlang:trace_info({process_trace_contract,identity,1},match_spec),1=erlang:trace_pattern({process_trace_contract,identity,1},false),{traced,undefined}=erlang:trace_info({process_trace_contract,missing,1},traced),0=erlang:trace_pattern({process_trace_contract,missing,1},true)"
        );
        Add(
            "event-query",
            "{match_spec,true}=erlang:trace_info(send,match_spec),1=erlang:trace_pattern(send,false),{match_spec,false}=erlang:trace_info(send,match_spec),1=erlang:trace_pattern(send,true),{match_spec,true}=erlang:trace_info(send,match_spec),1=erlang:trace_pattern('receive',[]),{match_spec,true}=erlang:trace_info('receive',match_spec)"
        );
        Add(
            "event-disabled",
            setup + ",1=erlang:trace_pattern(send,false),1=erlang:trace(P,true,[send,{tracer,S}]),P!{send,payload},receive payload->ok end," + barrier + ",receive {trace,P,send,_,_}->unexpected after 0->ok end"
        );
        Add(
            "delivered",
            "S=self(),R=erlang:trace_delivered(S),true=is_reference(R),receive {trace_delivered,S,R}->ok end,A=erlang:trace_delivered(all),true=(A=/=R),receive {trace_delivered,all,A}->ok end"
        );
        Add(
            "mfa-arity-boundaries",
            "0=erlang:trace_pattern({process_trace_contract,identity,-1},true),0=erlang:trace_pattern({process_trace_contract,identity,576460752303423487},true),{traced,undefined}=erlang:trace_info({process_trace_contract,identity,-1},traced),{traced,undefined}=erlang:trace_info({process_trace_contract,identity,576460752303423487},traced),ok=try erlang:trace_pattern({process_trace_contract,identity,576460752303423488},true) of _->unexpected catch error:badarg->ok end"
        );
        Add(
            "badopt-metadata",
            "try erlang:trace(self(),true,[wrong]) of _->error(accepted) catch error:badarg:S->[{erlang,trace,[_,_,_],L}|_]=S,{error_info,#{module:=erl_erts_errors,cause:=badopt}}=lists:keyfind(error_info,1,L) end"
        );
        Add(
            "pattern-metadata",
            "try erlang:trace_pattern({process_trace_contract,identity,1},wrong) of _->error(accepted) catch error:badarg:S->[{erlang,trace_pattern,[_,_],L}|_]=S,{error_info,#{module:=erl_erts_errors,cause:=match_spec}}=lists:keyfind(error_info,1,L) end"
        );
        foreach (string flag in new[] { "timestamp", "monotonic_timestamp", "strict_monotonic_timestamp" })
        {
            string shape = flag == "timestamp" ? "{A,B,C}" : flag == "strict_monotonic_timestamp" ? "{A,B}" : "A";
            string check = flag == "timestamp" ? "true=is_integer(A),true=is_integer(B),true=is_integer(C)" : flag == "strict_monotonic_timestamp" ? "true=is_integer(A),true=is_integer(B)" : "true=is_integer(A)";
            Add(
                "send-" + flag,
                setup + ",1=erlang:trace(P,true,[send," + flag + ",{tracer,S}]),P!{send,payload},receive {trace_ts,P,send,payload,S," + shape + "}->" + check + " end,receive payload->ok end," + barrier
            );
        }
        foreach (string invocation in new[] { "erlang:trace(self(),'maybe',[])", "erlang:trace(self(),true,[unknown])", "erlang:trace(self(),true,[send|tail])", "erlang:trace(not_a_process,true,[])", "erlang:trace_info(self(),unknown)", "erlang:trace_pattern({process_trace_contract,identity,1},true,[unknown])", "erlang:trace_pattern({process_trace_contract,'_',1},true)", "erlang:trace_pattern({process_trace_contract,identity,1},not_a_spec)", "erlang:trace_delivered(not_a_process)" })
            Add("badarg-" + result.Count, "ok=try " + invocation + " of _->unexpected catch error:badarg->ok end");

        Add(
            "call-match-guard-message-return",
            setup + ",MS=[{['$1'],[{is_integer,'$1'},{'>','$1',3}],[{message,{{matched,'$1'}}},{return_trace}]}],1=erlang:trace_pattern({process_trace_contract,identity,1},MS),{match_spec,MS}=erlang:trace_info({process_trace_contract,identity,1},match_spec),1=erlang:trace(P,true,[call,{tracer,S}]),P!{call,7},receive {trace,P,call,{process_trace_contract,identity,[7]},{matched,7}}->ok end,receive {trace,P,return_from,{process_trace_contract,identity,1},7}->ok end,receive 7->ok end," + barrier
        );
        Add(
            "call-match-guard-miss",
            setup + ",MS=[{['$1'],[{is_integer,'$1'}],[]}],1=erlang:trace_pattern({process_trace_contract,identity,1},MS),1=erlang:trace(P,true,[call,{tracer,S}]),P!{call,payload},receive payload->ok end," + barrier + ",receive {trace,P,call,_}->error(unexpected) after 0->ok end"
        );
        Add(
            "call-message-false-return",
            setup + ",1=erlang:trace_pattern({process_trace_contract,identity,1},[{'_',[],[{message,false},{return_trace}]}]),1=erlang:trace(P,true,[call,{tracer,S}]),P!{call,payload},receive {trace,P,return_from,{process_trace_contract,identity,1},payload}->ok end,receive payload->ok end," + barrier + ",receive {trace,P,call,_}->error(unexpected) after 0->ok end"
        );
        Add(
            "call-exception-trace",
            setup + ",M=monitor(process,P),1=erlang:trace_pattern({process_trace_contract,identity,1},[{'_',[],[{exception_trace}]}]),1=erlang:trace(P,true,[call,{tracer,S}]),P!{call,fail},receive {trace,P,call,{process_trace_contract,identity,[fail]}}->ok end,receive {trace,P,exception_from,{process_trace_contract,identity,1},{error,trace_failure}}->ok end,receive {'DOWN',M,process,P,{trace_failure,_}}->ok end," + barrier
        );
        Add(
            "call-exception-trace-normal-return",
            setup + ",1=erlang:trace_pattern({process_trace_contract,identity,1},[{'_',[],[{exception_trace}]}]),1=erlang:trace(P,true,[call,{tracer,S}]),P!{call,payload},receive {trace,P,call,{process_trace_contract,identity,[payload]}}->ok end,receive {trace,P,return_from,{process_trace_contract,identity,1},payload}->ok end,receive payload->ok end," + barrier
        );
        Add(
            "send-match-message",
            setup + ",MS=[{['_',{tag,'$1'}],[{is_integer,'$1'}],[{message,'$1'}]}],1=erlang:trace_pattern(send,MS),{match_spec,MS}=erlang:trace_info(send,match_spec),1=erlang:trace(P,true,[send,{tracer,S}]),P!{send,{tag,7}},receive {trace,P,send,{tag,7},S,7}->ok end,receive {tag,7}->ok end," + barrier
        );
        Add(
            "send-match-guard-miss",
            setup + ",1=erlang:trace_pattern(send,[{['_','$1'],[{is_integer,'$1'}],[]}]),1=erlang:trace(P,true,[send,{tracer,S}]),P!{send,payload},receive payload->ok end," + barrier + ",receive {trace,P,send,_,_}->error(unexpected) after 0->ok end"
        );
        Add(
            "receive-match-sender-message",
            setup + ",MS=[{['$1','$2',{payload,'$3'}],[{is_pid,'$2'}],[{message,{{origin,'$1','$2','$3'}}}]}],1=erlang:trace_pattern('receive',MS),{match_spec,MS}=erlang:trace_info('receive',match_spec),1=erlang:trace(P,true,['receive',{tracer,S}]),P!{payload,9},N=node(),receive {trace,P,'receive',{payload,9},{origin,N,S,9}}->ok end," + barrier + ",P!stop"
        );
        Add(
            "receive-match-clock-service",
            setup + ",1=erlang:trace_pattern('receive',[{[clock_service,undefined,timeout],[],[{message,clock}]}]),1=erlang:trace(P,true,['receive',{tracer,S}]),P!{timeout,payload},receive {trace,P,'receive',timeout,clock}->ok end,receive payload->ok end," + barrier
        );
        Add(
            "pattern-exact-numeric-match",
            setup + ",1=erlang:trace_pattern({process_trace_contract,identity,1},[{[1],[],[]}]),1=erlang:trace(P,true,[call,{tracer,S}]),P!{call,1.0},receive 1.0->ok end," + barrier + ",receive {trace,P,call,_}->error(unexpected) after 0->ok end"
        );
        Add(
            "pattern-wildcard-export-count",
            "5=erlang:trace_pattern({process_trace_contract,'_','_'},true),{traced,global}=erlang:trace_info({process_trace_contract,run,0},traced),{traced,global}=erlang:trace_info({process_trace_contract,target,1},traced),5=erlang:trace_pattern({process_trace_contract,'_','_'},false)"
        );
        Add(
            "default-self-tracer",
            "S=self(),{flags,[]}=erlang:trace_info(S,flags),{tracer,[]}=erlang:trace_info(S,tracer),1=erlang:trace(S,true,[send]),{tracer,S}=erlang:trace_info(S,tracer),S!payload,receive payload->ok end,R=erlang:trace_delivered(S),receive {trace_delivered,S,R}->ok end,receive {trace,_,_,_,_}->error(unexpected) after 0->ok end"
        );
        Add(
            "tracer-pid-exact-identity",
            setup + ",1=erlang:trace(P,true,[send,{tracer,S}]),T=list_to_pid(pid_to_list(S)),1=erlang:trace(P,true,[send,{tracer,T}]),{tracer,S}=erlang:trace_info(P,tracer),1=erlang:trace(P,false,['receive',{tracer,P}]),{tracer,P}=erlang:trace_info(P,tracer),{flags,[send]}=erlang:trace_info(P,flags),P!stop"
        );
        Add(
            "dead-tracer-clears-query",
            setup + ",{T,M}=spawn_monitor(fun()->receive stop->ok end end),1=erlang:trace(P,true,[send,{tracer,T}]),T!stop,receive {'DOWN',M,process,T,normal}->ok end,{flags,[]}=erlang:trace_info(P,flags),{tracer,[]}=erlang:trace_info(P,tracer),1=erlang:trace(P,true,[send,{tracer,S}]),P!{send,payload},receive {trace,P,send,payload,S}->ok end,receive payload->ok end," + barrier
        );
        Add(
            "dead-tracee-barrier",
            "{P,M}=spawn_monitor(fun()->ok end),receive {'DOWN',M,process,P,normal}->ok end,undefined=erlang:trace_info(P,flags),ok=try erlang:trace(P,true,[]) of _->unexpected catch error:badarg->ok end,R=erlang:trace_delivered(P),receive {trace_delivered,P,R}->ok end"
        );
        Add("existing-no-flags", "0=erlang:trace(existing,true,[]),0=erlang:trace(existing_processes,false,[])");
        Add(
            "event-return-action-rejected",
            "MS=[{'_',[],[{return_trace}]}],1=erlang:trace_pattern(send,MS),{match_spec,MS}=erlang:trace_info(send,match_spec),1=erlang:trace_pattern(send,false)"
        );

        Add(
            "foreign-creation-barrier-rejected",
            "B=term_to_binary(self()),N=byte_size(B)-4,<<Prefix:N/binary,C:32>>=B,F=binary_to_term(<<Prefix/binary,(C bxor 1):32>>),true=is_pid(F),true=(F=/=self()),undefined=erlang:trace_info(F,flags),ok=try erlang:trace_delivered(F) of _->unexpected catch error:badarg->ok end"
        );

        Add(
            "barrier-receive-system-sequence-preservation",
            "S=self(),T=spawn(fun()->receive X->S!X end end),1=erlang:trace_pattern('receive',[{[system,undefined,{trace_delivered,'$1','$2'}],[],[{message,{{barrier,'$1','$2'}}}]}]),1=erlang:trace(S,true,['receive',{tracer,T}]),0=erlang:seq_trace(label,label),R=erlang:trace_delivered(S),receive {trace_delivered,S,R}->{label,label}=erlang:seq_trace_info(label) end,receive {trace,S,'receive',{trace_delivered,S,R},{barrier,S,R}}->ok end,[]=erlang:seq_trace_info(label)"
        );

        return result.AsReadOnly();
    }
}
