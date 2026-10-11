namespace Erlang.Differential;

public static class SupervisorLifecycleCases
{
    private static Term A(string value) => Term.A(value);

    private static CompiledModuleCase Case(string name, string body) => new(
        "otp-supervisor-lifecycle-" + name,
        "-module(oracle_life). -export([run/0,init/1,child/1]). run()->" + body + ". " +
        "init(C)->{ok,{#{intensity=>4},C}}. " +
        "child(ignore)->ignore; child({existing,P})->{ok,P}; child({extra,P})->{ok,P,extra}; " +
        "child(dead)->{P,M}=spawn_monitor(fun()->ok end),receive {'DOWN',M,process,P,normal}->{ok,P} end; " +
        "child(linked_dead)->{P,M}=spawn_monitor(fun()->receive finish->ok end end),link(P),P!finish,receive {'DOWN',M,process,P,normal}->receive E={'EXIT',P,normal}->self()!E,{ok,P} end end; " +
        "child(unlinked)->{ok,spawn(fun()->receive {ping,Q}->Q!pong,receive finish->ok end; finish->ok end end)}; " +
        "child(detached)->S=self(),P=spawn_link(fun()->unlink(S),S!{ready,self()},receive finish->ok end end),receive {ready,P}->{ok,P} end; " +
        "child({stubborn,Parent})->S=self(),P=spawn_link(fun()->process_flag(trap_exit,true),S!{ready,self()},receive {'EXIT',S,shutdown}->Parent!{shutdown_started,self()},receive finish->ok end end end),receive {ready,P}->{ok,P} end; " +
        "child({manual,D})->case get(attempt) of undefined->put(attempt,1),ignore; _->{ok,D,extra} end; " +
        "child({automatic,Parent,D})->case get(attempt) of undefined->put(attempt,1),P=spawn_link(fun()->receive finish->ok end end),Parent!{started,P},{ok,P}; _->Parent!{returned,D},{ok,D,extra} end; " +
        "child({shared,Parent})->N=case get(count) of undefined->1; V->V+1 end,put(count,N),P=case get(shared) of undefined->C=spawn_link(fun()->receive finish->ok end end),put(shared,C),C; C->C end,Parent!{started,N},{ok,P}.",
        Term.Tuple(A("ok"), A("done"))
    );

    private const string Dead = "{D,M}=spawn_monitor(fun()->ok end),receive {'DOWN',M,process,D,normal}->ok after 1000->missing end,";
    private const string ZeroActive = "[{specs,1},{active,0},{supervisors,0},{workers,1}]=supervisor:count_children(P),";
    private const string Stop = "ok=gen_server:stop(P),done";

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } =
    [
        Case(
            "static-dead",
            "S=#{id=>first,start=>{oracle_life,child,[dead]}},{ok,P}=supervisor:start_link(oracle_life,[S]),[{first,C,worker,_}]=supervisor:which_children(P)," + ZeroActive +
            "{error,{already_started,C}}=supervisor:start_child(P,S),{error,running}=supervisor:restart_child(P,first),{error,running}=supervisor:delete_child(P,first),ok=supervisor:terminate_child(P,first),[{first,undefined,worker,_}]=supervisor:which_children(P),ok=supervisor:delete_child(P,first),[] =supervisor:which_children(P)," + Stop
        ),
        Case(
            "dynamic-dead-extra",
            Dead + "{ok,P}=supervisor:start_link(oracle_life,[]),S=#{id=>first,start=>{oracle_life,child,[{extra,D}]}},{ok,D,extra}=supervisor:start_child(P,S),[{first,D,worker,_}]=supervisor:which_children(P)," + ZeroActive +
            "ok=supervisor:terminate_child(P,first),ok=supervisor:delete_child(P,first)," + Stop
        ),
        Case(
            "manual-dead-extra",
            Dead + "S=#{id=>first,start=>{oracle_life,child,[{manual,D}]}},{ok,P}=supervisor:start_link(oracle_life,[S]),[{first,undefined,worker,_}]=supervisor:which_children(P),{ok,D,extra}=supervisor:restart_child(P,first),[{first,D,worker,_}]=supervisor:which_children(P)," + ZeroActive +
            "ok=supervisor:terminate_child(P,first)," + Stop
        ),
        Case(
            "automatic-dead-extra",
            Dead + "S=#{id=>first,start=>{oracle_life,child,[{automatic,self(),D}]}},{ok,P}=supervisor:start_link(oracle_life,[S]),C=receive {started,C0}->C0 after 1000->missing end,exit(C,kill),receive {returned,D}->ok after 1000->missing end,[{first,D,worker,_}]=supervisor:which_children(P)," + ZeroActive +
            "{error,running}=supervisor:restart_child(P,first),ok=supervisor:terminate_child(P,first)," + Stop
        ),
        Case(
            "linked-dead-transient",
            "S=#{id=>first,start=>{oracle_life,child,[linked_dead]},restart=>transient},{ok,P}=supervisor:start_link(oracle_life,[S]),[{first,undefined,worker,_}]=supervisor:which_children(P)," + ZeroActive + Stop
        ),
        Case(
            "linked-dead-temporary",
            "S=#{id=>first,start=>{oracle_life,child,[linked_dead]},restart=>temporary},{ok,P}=supervisor:start_link(oracle_life,[S]),[] =supervisor:which_children(P),[{specs,0},{active,0},{supervisors,0},{workers,0}]=supervisor:count_children(P)," + Stop
        ),
        Case(
            "detached-at-start",
            "S=#{id=>first,start=>{oracle_life,child,[detached]}},{ok,P}=supervisor:start_link(oracle_life,[S]),[{first,C,worker,_}]=supervisor:which_children(P),M=monitor(process,C),exit(C,kill),receive {'DOWN',M,process,C,killed}->ok after 1000->missing end,[{first,C,worker,_}]=supervisor:which_children(P)," + ZeroActive +
            "ok=supervisor:terminate_child(P,first)," + Stop
        ),
        Case(
            "existing-unlinked-dynamic",
            "C=spawn(fun()->receive finish->ok end end),M=monitor(process,C),{ok,P}=supervisor:start_link(oracle_life,[]),S=#{id=>first,start=>{oracle_life,child,[{existing,C}]}},{ok,C}=supervisor:start_child(P,S),exit(C,kill),receive {'DOWN',M,process,C,killed}->ok after 1000->missing end,[{first,C,worker,_}]=supervisor:which_children(P)," + ZeroActive +
            "{error,{already_started,C}}=supervisor:start_child(P,S),ok=supervisor:terminate_child(P,first)," + Stop
        ),
        Case(
            "unlinked-survives-supervisor-kill",
            "process_flag(trap_exit,true),S=#{id=>first,start=>{oracle_life,child,[unlinked]}},{ok,P}=supervisor:start_link(oracle_life,[S]),[{first,C,worker,_}]=supervisor:which_children(P),M=monitor(process,P),N=monitor(process,C),exit(P,kill),receive {'DOWN',M,process,P,killed}->ok after 1000->missing end,C!{ping,self()},receive pong->ok after 1000->missing end,exit(C,kill),receive {'DOWN',N,process,C,killed}->done after 1000->missing end"
        ),
        Shutdown(
            "unlinked-infinite",
            "unlinked",
            "infinity",
            "shutdown"
        ),
        Shutdown(
            "detached",
            "detached",
            "infinity",
            "shutdown"
        ),
        Shutdown(
            "stubborn-timeout",
            "{stubborn,self()}",
            "0",
            "killed"
        ),
        Shutdown(
            "brutal-kill",
            "{stubborn,self()}",
            "brutal_kill",
            "killed"
        ),
        Case(
            "shared-pid-exit-flush",
            "A=#{id=>first,start=>{oracle_life,child,[{shared,self()}]}},B=#{id=>second,start=>{oracle_life,child,[{shared,self()}]}},{ok,P}=supervisor:start_link(oracle_life,[A,B]),receive {started,1}->ok after 1000->missing end,receive {started,2}->ok after 1000->missing end,[{second,C,worker,_},{first,C,worker,_}]=supervisor:which_children(P),ok=supervisor:terminate_child(P,first),[{second,C,worker,_},{first,undefined,worker,_}]=supervisor:which_children(P),[{specs,2},{active,0},{supervisors,0},{workers,2}]=supervisor:count_children(P),ok=receive {started,3}->unexpected after 0->ok end,ok=supervisor:terminate_child(P,second)," + Stop
        ),
        Case(
            "selective-shutdown-queue",
            "S=#{id=>first,start=>{oracle_life,child,[{stubborn,self()}]},restart=>transient,shutdown=>infinity},{ok,P}=supervisor:start_link(oracle_life,[S]),[{first,C,worker,_}]=supervisor:which_children(P),K=make_ref(),P!{'$gen_call',{self(),K},{terminate_child,first}},receive {shutdown_started,C}->ok after 1000->missing end,Q=make_ref(),P!{'$gen_call',{self(),Q},which_children},C!finish,receive {K,ok}->ok after 1000->missing end,receive {Q,[{first,undefined,worker,_}]}->ok after 1000->missing end," + Stop
        )
    ];

    private static CompiledModuleCase Shutdown(
        string name,
        string start,
        string shutdown,
        string reason
    ) => Case(
        name,
        "S=#{id=>first,start=>{oracle_life,child,[" + start + "]},shutdown=>" + shutdown + "},{ok,P}=supervisor:start_link(oracle_life,[S]),[{first,C,worker,_}]=supervisor:which_children(P),M=monitor(process,C),ok=supervisor:terminate_child(P,first),receive {'DOWN',M,process,C," + reason + "}->ok after 1000->missing end,[{first,undefined,worker,_}]=supervisor:which_children(P)," + Stop
    );
}
