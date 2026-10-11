namespace Erlang.Differential;

public static class SupervisorRetryCases
{
    private static Term A(string value) => Term.A(value);

    private static CompiledModuleCase Case(string name, string body, Term expected) => new(
        "otp-supervisor-retry-" + name,
        "-module(oracle_retry). -export([run/0,init/1,child/1,handle_call/3,handle_cast/2,terminate/2]). run()->" + body + ". " +
        "init({worker,S})->process_flag(trap_exit,true),{ok,S}; init({flags,F,C})->{ok,{F,C}}. " +
        "child({P,Id,F,Result})->K={attempt,Id},N=case get(K) of undefined->1; V->V+1 end,put(K,N)," +
        "case N>1 andalso N=<F+1 of true->P!{failed,Id,N,self()},receive go->ok end," +
        "case Result of {throw,T}->throw(T); {exit,T}->exit(T); T->T end; " +
        "false->{ok,C}=gen_server:start_link(oracle_retry,{worker,{P,Id,N}},[]),R=case Result of extra->{ok,C,extra}; _->{ok,C} end,P!{started,Id,N,C},R end. " +
        "handle_call(get,_,{P,Id,N}=S)->{reply,N,S}. handle_cast(_,S)->{noreply,S}. terminate(R,{P,Id,N})->P!{terminated,Id,N,R}.",
        Term.Tuple(A("ok"), expected)
    );

    private const string Setup = "S=#{id=>first,start=>{oracle_retry,child,[{self(),first,1,{error,failed}}]}},{ok,P}=supervisor:start_link(oracle_retry,{flags,#{intensity=>5},[S]}),C=receive {started,first,1,C0}->C0 after 1000->missing end,exit(C,kill),receive {failed,first,2,P}->ok after 1000->missing end,";
    private const string Query = "R=make_ref(),P!{'$gen_call',{self(),R},which_children},";
    private const string Marker = "receive {R,[{first,restarting,worker,[oracle_retry]}]}->ok after 1000->missing end,";

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = [
        Case(
            "state-and-recovery",
            Setup + Query +
            "K=make_ref(),P!{'$gen_call',{self(),K},count_children},B=make_ref(),P!{'$gen_call',{self(),B},{restart_child,first}},D=make_ref(),P!{'$gen_call',{self(),D},{delete_child,first}},E=make_ref(),P!{'$gen_call',{self(),E},{start_child,S}},P!go," + Marker +
            "N=receive {K,V}->V after 1000->missing end,receive {B,{error,restarting}}->ok after 1000->missing end,receive {D,{error,restarting}}->ok after 1000->missing end,receive {E,{error,already_present}}->ok after 1000->missing end,Q=receive {started,first,3,Q0}->Q0 after 1000->missing end,3=gen_server:call(Q,get),ok=gen_server:stop(P),N",
            Term.List(
                Term.Tuple(A("specs"),Term.I(1)),
                Term.Tuple(A("active"),Term.I(0)),
                Term.Tuple(A("supervisors"),Term.I(0)),
                Term.Tuple(A("workers"),Term.I(1))
            )
        ),
        Case(
            "terminate-cancels",
            Setup + Query +
            "K=make_ref(),P!{'$gen_call',{self(),K},{terminate_child,first}},P!go," + Marker +
            "receive {K,ok}->ok after 1000->missing end,[{first,undefined,worker,_}]=supervisor:which_children(P),{ok,Q}=supervisor:restart_child(P,first),3=gen_server:call(Q,get),receive {started,first,3,Q}->ok after 1000->missing end,[{first,Q,worker,_}]=supervisor:which_children(P),ok=gen_server:stop(P),ok",
            A("ok")
        ),
        Case(
            "delete-and-replace",
            Setup +
            "K=make_ref(),P!{'$gen_call',{self(),K},{terminate_child,first}},D=make_ref(),P!{'$gen_call',{self(),D},{delete_child,first}},P!go,receive {K,ok}->ok after 1000->missing end,receive {D,ok}->ok after 1000->missing end,[] =supervisor:which_children(P),{ok,Q}=supervisor:start_child(P,S),3=gen_server:call(Q,get),receive {started,first,3,Q}->ok after 1000->missing end,ok=gen_server:stop(P),ok",
            A("ok")
        ),
        Case(
            "shutdown-between-attempts",
            Setup +
            "M=monitor(process,P),R=make_ref(),P!{system,{self(),R},{terminate,normal}},P!go,receive {'DOWN',M,process,P,normal}->done after 1000->missing end",
            A("done")
        ),
        Case(
            "intensity-exhausted",
            "S=#{id=>first,start=>{oracle_retry,child,[{self(),first,9,{error,failed}}]}},{ok,P}=supervisor:start_link(oracle_retry,{flags,#{intensity=>2},[S]}),M=monitor(process,P),process_flag(trap_exit,true),C=receive {started,first,1,C0}->C0 after 1000->missing end,exit(C,kill),receive {failed,first,2,P}->P!go after 1000->missing end,receive {failed,first,3,P}->P!go after 1000->missing end,receive {'DOWN',M,process,P,shutdown}->done after 1000->missing end",
            A("done")
        ),
        Case(
            "ignore-clears-marker",
            "S=#{id=>first,start=>{oracle_retry,child,[{self(),first,1,ignore}]}},{ok,P}=supervisor:start_link(oracle_retry,{flags,#{intensity=>5},[S]}),C=receive {started,first,1,C0}->C0 after 1000->missing end,exit(C,kill),receive {failed,first,2,P}->ok after 1000->missing end," + Query +
            "P!go,receive {R,[{first,undefined,worker,_}]}->ok after 1000->missing end,{error,already_present}=supervisor:start_child(P,S),{ok,Q}=supervisor:restart_child(P,first),3=gen_server:call(Q,get),ok=gen_server:stop(P),ok",
            A("ok")
        ),
        Case(
            "two-failures",
            "S=#{id=>first,start=>{oracle_retry,child,[{self(),first,2,bad}]}},{ok,P}=supervisor:start_link(oracle_retry,{flags,#{intensity=>5},[S]}),C=receive {started,first,1,C0}->C0 after 1000->missing end,exit(C,kill),receive {failed,first,2,P}->ok after 1000->missing end," + Query + "P!go," + Marker +
            "receive {failed,first,3,P}->ok after 1000->missing end,K=make_ref(),P!{'$gen_call',{self(),K},which_children},P!go,receive {K,[{first,restarting,worker,_}]}->ok after 1000->missing end,Q=receive {started,first,4,Q0}->Q0 after 1000->missing end,V=gen_server:call(Q,get),ok=gen_server:stop(P),V",
            Term.I(4)
        ),
        Recovery("throw", "{throw,{error,failed}}"),
        Recovery("exit", "{exit,failed}"),
        Recovery("extra", "extra"),
        Group("one-for-all", "one_for_all", 3),
        Group("rest-for-one", "rest_for_one", 2),
        Case(
            "forged-tag",
            Setup +
            "P!{'$gen_cast',{try_again_restart,make_ref(),first}}," + Query + "P!go," + Marker +
            "Q=receive {started,first,3,Q0}->Q0 after 1000->missing end,3=gen_server:call(Q,get),ok=gen_server:stop(P),ok",
            A("ok")
        ),
        Case(
            "zero-intensity",
            "S=#{id=>first,start=>{oracle_retry,child,[{self(),first,1,{error,failed}}]}},{ok,P}=supervisor:start_link(oracle_retry,{flags,#{intensity=>0},[S]}),M=monitor(process,P),process_flag(trap_exit,true),C=receive {started,first,1,C0}->C0 after 1000->missing end,exit(C,kill),receive {'DOWN',M,process,P,shutdown}->done after 1000->missing end",
            A("done")
        )
    ];

    private static CompiledModuleCase Recovery(string name, string result) => Case(
        name,
        "S=#{id=>first,start=>{oracle_retry,child,[{self(),first,1," + result + "}]}},{ok,P}=supervisor:start_link(oracle_retry,{flags,#{intensity=>5},[S]}),C=receive {started,first,1,C0}->C0 after 1000->missing end,exit(C,kill),receive {failed,first,2,P}->ok after 1000->missing end," + Query + "P!go," + Marker +
        "Q=receive {started,first,3,Q0}->Q0 after 1000->missing end,V=gen_server:call(Q,get),ok=gen_server:stop(P),V",
        Term.I(3)
    );

    private static CompiledModuleCase Group(string name, string strategy, int firstAttempts) => Case(
        name,
        "A=#{id=>first,start=>{oracle_retry,child,[{self(),first,0,ok}]}},B=#{id=>second,start=>{oracle_retry,child,[{self(),second,1,{error,failed}}]}},D=#{id=>third,start=>{oracle_retry,child,[{self(),third,0,ok}]}},T=#{id=>temp,start=>{oracle_retry,child,[{self(),temp,0,ok}]},restart=>temporary}," +
        "{ok,P}=supervisor:start_link(oracle_retry,{flags,#{strategy=>" + strategy + ",intensity=>5},[A,B,D,T]}),C=receive {started,first,1,C0}->C0 after 1000->missing end,receive {started,second,1,_}->ok after 1000->missing end,receive {started,third,1,_}->ok after 1000->missing end,receive {started,temp,1,_}->ok after 1000->missing end,exit(C,kill),receive {failed,second,2,P}->ok after 1000->missing end," + Query +
        "K=make_ref(),P!{'$gen_call',{self(),K},count_children},P!go,receive {R,[{third,undefined,worker,_},{second,restarting,worker,_},{first,_,worker,_}]}->ok after 1000->missing end,receive {K,[{specs,3},{active,1},{supervisors,0},{workers,3}]}->ok after 1000->missing end,Q=receive {started,second,3,Q0}->Q0 after 1000->missing end,U=receive {started,third,2,U0}->U0 after 1000->missing end,[{third,U,worker,_},{second,Q,worker,_},{first,F,worker,_}]=supervisor:which_children(P),R0={gen_server:call(F,get),gen_server:call(Q,get),gen_server:call(U,get)},ok=gen_server:stop(P),R0",
        Term.Tuple(Term.I(firstAttempts), Term.I(3), Term.I(2))
    );
}
