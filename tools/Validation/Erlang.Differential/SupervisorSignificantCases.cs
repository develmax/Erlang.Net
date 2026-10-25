namespace Erlang.Differential;

public static class SupervisorSignificantCases
{
    private static CompiledModuleCase Case(string name, string body) => new(
        "otp-supervisor-significant-" + name,
        "-module(oracle_sig). -export([run/0,init/1,child/1]). run()->process_flag(trap_exit,true)," + body + ". " +
        "init({Flags,C}) when is_map(Flags)->{ok,{Flags,C}}; init({Mode,C})->{ok,{#{auto_shutdown=>Mode,intensity=>5},C}}. " +
        "child(ignore)->ignore; child(fail)->{error,failed}; " +
        "child({R,Id})->P=spawn_link(fun()->receive {finish,V}->exit(V) end end),R!{started,Id,P},{ok,P}. " +
        "spec(Id,R,Restart,Significant)->#{id=>Id,start=>{oracle_sig,child,[{R,Id}]},restart=>Restart,significant=>Significant}. " +
        "start(Mode,C)->supervisor:start_link(oracle_sig,{Mode,C}). " +
        "gone(P,Id)->case supervisor:which_children(P) of [{Id,undefined,worker,_}]->ok; _->gone(P,Id) end. " +
        "wait_second(P,D)->case supervisor:which_children(P) of [{second,D,worker,_}]->ok; _->wait_second(P,D) end. " +
        "wait_empty(P)->case supervisor:which_children(P) of []->ok; _->wait_empty(P) end. " +
        "down(M,P,Reason)->receive {'DOWN',M,process,P,Reason}->ok after 2000->missing end. " +
        "alive(P)->supervisor:count_children(P),true. stop(P)->ok=gen_server:stop(P),done.",
        Term.Tuple(Term.A("ok"), Term.A("done"))
    );

    private static CompiledModuleCase Shutdown(
        string name,
        string policy,
        string restart,
        string reason
    ) => Case(
        name,
        "R=self(),S=spec(first,R," + restart + ",true),T=spec(other,R,permanent,false),{ok,P}=start(" + policy + ",[S,T])," +
        "receive {started,first,C}->ok end,receive {started,other,D}->ok end,M=monitor(process,P),N=monitor(process,D),C!{finish," + reason + "}," +
        "ok=down(M,P,shutdown),ok=down(N,D,shutdown),done"
    );

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } =
    [
        Case(
            "check-policy",
            "S=spec(first,self(),transient,true),ok=supervisor:check_childspecs([S]),ok=supervisor:check_childspecs([S],undefined),ok=supervisor:check_childspecs([S],any_significant),ok=supervisor:check_childspecs([S],all_significant),{error,{bad_combination,[{auto_shutdown,never},{significant,true}]}}=supervisor:check_childspecs([S],never),done"
        ),
        Case(
            "check-permanent",
            "S=spec(first,self(),permanent,true),{error,{bad_combination,[{restart,permanent},{significant,true}]}}=supervisor:check_childspecs([S]),{error,{bad_combination,[{restart,permanent},{significant,true}]}}=supervisor:check_childspecs([S],any_significant),{error,{bad_combination,[{auto_shutdown,never},{significant,true}]}}=supervisor:check_childspecs([S],never),done"
        ),
        Case(
            "check-invalid-boolean",
            "S=spec(first,self(),temporary,wrong),{error,{invalid_significant,wrong}}=supervisor:check_childspecs([S],any_significant),done"
        ),
        Case(
            "check-invalid-policy-order",
            "{error,{badarg,wrong}}=supervisor:check_childspecs([],wrong),{error,{badarg,not_list}}=supervisor:check_childspecs(not_list,wrong),{error,{badarg,wrong}}=supervisor:check_childspecs([bad|tail],wrong),{error,{badarg,wrong}}=supervisor:check_childspecs([bad],wrong),done"
        ),
        Case(
            "check-empty-and-legacy",
            "ok=supervisor:check_childspecs([],all_significant),ok=supervisor:check_childspecs([{first,{oracle_sig,child,[ignore]},permanent,5000,worker,[oracle_sig]}],any_significant),done"
        ),
        Case(
            "startup-never-error",
            "S=spec(first,self(),transient,true),{error,{start_spec,{bad_combination,[{auto_shutdown,never},{significant,true}]}}}=start(never,[S]),done"
        ),
        Case(
            "startup-permanent-error",
            "S=spec(first,self(),permanent,true),{error,{start_spec,{bad_combination,[{restart,permanent},{significant,true}]}}}=start(any_significant,[S]),done"
        ),
        Case("startup-invalid-policy", "{error,{supervisor_data,{invalid_auto_shutdown,wrong}}}=start(wrong,[]),done"),
        Case("startup-empty-any", "{ok,P}=start(any_significant,[]),[]=supervisor:which_children(P),stop(P)"),
        Case("startup-empty-all", "{ok,P}=start(all_significant,[]),[]=supervisor:which_children(P),stop(P)"),
        Shutdown(
            "any-transient-normal",
            "any_significant",
            "transient",
            "normal"
        ),
        Shutdown(
            "any-transient-shutdown",
            "any_significant",
            "transient",
            "shutdown"
        ),
        Shutdown(
            "any-transient-shutdown-tuple",
            "any_significant",
            "transient",
            "{shutdown,detail}"
        ),
        Shutdown(
            "any-temporary-normal",
            "any_significant",
            "temporary",
            "normal"
        ),
        Shutdown(
            "any-temporary-abnormal",
            "any_significant",
            "temporary",
            "failed"
        ),
        Shutdown(
            "all-last-transient",
            "all_significant",
            "transient",
            "normal"
        ),
        Shutdown(
            "all-last-temporary",
            "all_significant",
            "temporary",
            "failed"
        ),
        Case(
            "all-waits-for-last",
            "R=self(),A=spec(first,R,temporary,true),B=spec(second,R,transient,true),{ok,P}=start(all_significant,[A,B]),receive {started,first,C}->ok end,receive {started,second,D}->ok end,M=monitor(process,P),C!{finish,normal},wait_second(P,D),true=alive(P),D!{finish,shutdown},ok=down(M,P,shutdown),done"
        ),
        Case(
            "transient-abnormal-restarts",
            "R=self(),S=spec(first,R,transient,true),{ok,P}=start(any_significant,[S]),receive {started,first,C}->ok end,C!{finish,failed},receive {started,first,D}->true=C=/=D end,[{first,D,worker,_}]=supervisor:which_children(P),true=alive(P),stop(P)"
        ),
        Case(
            "non-significant-normal",
            "R=self(),S=spec(first,R,transient,false),{ok,P}=start(any_significant,[S]),receive {started,first,C}->ok end,C!{finish,normal},ok=gone(P,first),true=alive(P),stop(P)"
        ),
        Case(
            "non-significant-temporary",
            "R=self(),S=spec(first,R,temporary,false),{ok,P}=start(all_significant,[S]),receive {started,first,C}->ok end,C!{finish,failed},wait_empty(P),true=alive(P),stop(P)"
        ),
        Case(
            "manual-terminate-keeps-supervisor",
            "R=self(),S=spec(first,R,transient,true),{ok,P}=start(any_significant,[S]),receive {started,first,_}->ok end,ok=supervisor:terminate_child(P,first),ok=gone(P,first),true=alive(P),{ok,D}=supervisor:restart_child(P,first),receive {started,first,D}->ok end,M=monitor(process,P),D!{finish,normal},ok=down(M,P,shutdown),done"
        ),
        Case(
            "manual-delete-keeps-supervisor",
            "R=self(),S=spec(first,R,temporary,true),{ok,P}=start(all_significant,[S]),receive {started,first,_}->ok end,ok=supervisor:terminate_child(P,first),[]=supervisor:which_children(P),true=alive(P),stop(P)"
        ),
        Case(
            "dynamic-significant-child",
            "R=self(),{ok,P}=start(any_significant,[]),S=spec(first,R,temporary,true),{ok,C}=supervisor:start_child(P,S),receive {started,first,C}->ok end,M=monitor(process,P),C!{finish,normal},ok=down(M,P,shutdown),done"
        ),
        Case(
            "dynamic-significant-invalid",
            "R=self(),{ok,P}=start(never,[]),S=spec(first,R,transient,true),{error,{bad_combination,[{auto_shutdown,never},{significant,true}]}}=supervisor:start_child(P,S),[]=supervisor:which_children(P),stop(P)"
        ),
        Case(
            "ignored-startup",
            "S=#{id=>first,start=>{oracle_sig,child,[ignore]},restart=>temporary,significant=>true},{ok,P}=start(any_significant,[S]),[]=supervisor:which_children(P),true=alive(P),stop(P)"
        ),
        Case(
            "ignored-dynamic",
            "S=#{id=>first,start=>{oracle_sig,child,[ignore]},restart=>transient,significant=>true},{ok,P}=start(all_significant,[]),{ok,undefined}=supervisor:start_child(P,S),[{first,undefined,worker,_}]=supervisor:which_children(P),true=alive(P),stop(P)"
        ),
        Case(
            "inactive-significant-not-counted",
            "R=self(),A=spec(first,R,transient,true),B=#{id=>ignored,start=>{oracle_sig,child,[ignore]},restart=>transient,significant=>true},{ok,P}=start(all_significant,[A,B]),receive {started,first,C}->ok end,M=monitor(process,P),C!{finish,normal},ok=down(M,P,shutdown),done"
        ),
        Case(
            "failed-dynamic-record",
            "{ok,P}=start(any_significant,[]),S=#{id=>first,start=>{oracle_sig,child,[fail]},restart=>transient,significant=>true},{error,{failed,{child,undefined,first,{oracle_sig,child,[fail]},transient,true,5000,worker,[oracle_sig]}}}=supervisor:start_child(P,S),stop(P)"
        ),
        Case(
            "check-improper-invalid-head",
            "{error,{invalid_child_spec,bad}}=supervisor:check_childspecs([bad|tail],any_significant),done"
        ),
        Case(
            "check-improper-valid-prefix",
            "S=spec(first,self(),temporary,false),done=try supervisor:check_childspecs([S|tail],any_significant) catch error:function_clause->done end,done"
        ),
        Case(
            "group-restart-no-auto-shutdown",
            "R=self(),A=spec(first,R,permanent,false),B=spec(second,R,transient,true),{ok,P}=start(#{strategy=>one_for_all,auto_shutdown=>any_significant,intensity=>5},[A,B]),receive {started,first,C}->ok end,receive {started,second,_}->ok end,C!{finish,failed},receive {started,first,_}->ok end,receive {started,second,D}->ok end,true=alive(P),M=monitor(process,P),D!{finish,normal},ok=down(M,P,shutdown),done"
        )
    ];
}
