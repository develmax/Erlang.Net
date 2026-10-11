namespace Erlang.Differential;

public static class SupervisorSimpleCases
{
    private static CompiledModuleCase Case(string name, string body) => new(
        "otp-supervisor-simple-" + name,
        "-module(oracle_simple). -export([run/0,init/1,child/3]). run()->process_flag(trap_exit,true)," + body + ". " +
        "init({Flags,C})->{ok,{Flags,C}}. " +
        "child(_,_,ignore)->ignore; child(_,_,fail)->{error,failed}; child(_,_,{return,P})->{ok,P}; child(_,_,{extra,P})->{ok,P,extra}; " +
        "child(Base,R,{recover,Id,Ref})->N=case get(Id) of undefined->1;V->V+1 end,put(Id,N),case N of 2->self()!{'$gen_call',{R,Ref},count_children},{error,failed};_->worker(Base,R,Id) end; " +
        "child(Base,R,{restart_ignore,Id})->case get(Id) of undefined->put(Id,1),worker(Base,R,Id);_->R!ignored,ignore end; " +
        "child(Base,R,{stubborn,Id})->S=self(),P=spawn_link(fun()->process_flag(trap_exit,true),S!{ready,self()},receive {'EXIT',S,shutdown}->R!{shutdown,Id,self()},receive finish->ok end end end),receive {ready,P}->ok end,R!{started,Id,Base,P},{ok,P}; " +
        "child(Base,R,Id)->worker(Base,R,Id). " +
        "worker(Base,R,Id)->P=spawn_link(fun()->receive {finish,Reason}->exit(Reason) end end),R!{started,Id,Base,P},{ok,P}. " +
        "template(Restart)->#{id=>template,start=>{oracle_simple,child,[prefix]},restart=>Restart}. " +
        "start(S)->supervisor:start_link(oracle_simple,{#{strategy=>simple_one_for_one,intensity=>4},[S]}). " +
        "down(M,P,Reason)->receive {'DOWN',M,process,P,Reason}->ok after 2000->missing end. " +
        "empty(P)->case supervisor:which_children(P) of []->ok;_->empty(P) end. " +
        "stop(P)->ok=supervisor:stop(P),done.",
        Term.Tuple(Term.A("ok"), Term.A("done"))
    );

    private const string Zero = "[{specs,1},{active,0},{supervisors,0},{workers,0}]=supervisor:count_children(P),";
    private const string One = "[{specs,1},{active,1},{supervisors,0},{workers,1}]=supervisor:count_children(P),";
    private const string Dead = "{D,M}=spawn_monitor(fun()->ok end),ok=down(M,D,normal),";

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } =
    [
        Case(
            "empty-template",
            "{ok,P}=start(template(permanent)),[]=supervisor:which_children(P)," + Zero + "receive {started,_,_,_}->unexpected after 0->ok end,stop(P)"
        ),
        Case(
            "argument-append",
            "R=self(),{ok,P}=start(template(permanent)),{ok,C}=supervisor:start_child(P,[R,first]),receive {started,first,prefix,C}->ok end,[{undefined,C,worker,[oracle_simple]}]=supervisor:which_children(P)," + One + "ok=supervisor:terminate_child(P,C)," + Zero + "stop(P)"
        ),
        Case(
            "multiple-children",
            "R=self(),{ok,P}=start(template(temporary)),{ok,C}=supervisor:start_child(P,[R,first]),{ok,D}=supervisor:start_child(P,[R,second]),[{specs,1},{active,2},{supervisors,0},{workers,2}]=supervisor:count_children(P),ok=supervisor:terminate_child(P,C),[{undefined,D,worker,_}]=supervisor:which_children(P),ok=supervisor:terminate_child(P,D),stop(P)"
        ),
        Case(
            "extra-return",
            "R=self(),C=spawn(fun()->receive finish->ok end end),{ok,P}=start(template(permanent)),{ok,C,extra}=supervisor:start_child(P,[R,{extra,C}])," + One + "ok=supervisor:terminate_child(P,C),stop(P)"
        ),
        Case(
            "ignore",
            "{ok,P}=start(template(permanent)),{ok,undefined}=supervisor:start_child(P,[self(),ignore])," + Zero + "[]=supervisor:which_children(P),stop(P)"
        ),
        Case(
            "raw-start-error",
            "{ok,P}=start(template(permanent)),{error,failed}=supervisor:start_child(P,[self(),fail])," + Zero + "stop(P)"
        ),
        Case(
            "dead-pid-count",
            Dead + "{ok,P}=start(template(permanent)),{ok,D}=supervisor:start_child(P,[self(),{return,D}]),[{undefined,D,worker,_}]=supervisor:which_children(P)," + One + "ok=supervisor:terminate_child(P,D)," + Zero + "stop(P)"
        ),
        Case(
            "duplicate-pid-overwrites",
            "C=spawn(fun()->receive finish->ok end end),{ok,P}=start(template(permanent)),{ok,C}=supervisor:start_child(P,[self(),{return,C}]),{ok,C}=supervisor:start_child(P,[self(),{return,C}])," + One + "stop(P)"
        ),
        Case(
            "invalid-control-id",
            "{ok,P}=start(template(permanent)),{error,simple_one_for_one}=supervisor:terminate_child(P,template),{error,simple_one_for_one}=supervisor:restart_child(P,template),{error,simple_one_for_one}=supervisor:delete_child(P,template),stop(P)"
        ),
        Case(
            "restart-delete-pid-invalid",
            "{ok,P}=start(template(temporary)),{ok,C}=supervisor:start_child(P,[self(),first]),{error,simple_one_for_one}=supervisor:restart_child(P,C),{error,simple_one_for_one}=supervisor:delete_child(P,C),stop(P)"
        ),
        Case(
            "terminate-unrelated-alive",
            "C=spawn(fun()->receive finish->ok end end),{ok,P}=start(template(permanent)),{error,not_found}=supervisor:terminate_child(P,C),C!finish,stop(P)"
        ),
        Case("terminate-unrelated-dead", Dead + "{ok,P}=start(template(permanent)),ok=supervisor:terminate_child(P,D)," + Zero + "stop(P)"),
        Case(
            "template-validation-empty",
            "{error,{bad_start_spec,[]}}=supervisor:start_link(oracle_simple,{#{strategy=>simple_one_for_one},[]}),done"
        ),
        Case(
            "template-validation-multiple",
            "S=template(permanent),{error,{bad_start_spec,[S,S]}}=supervisor:start_link(oracle_simple,{#{strategy=>simple_one_for_one},[S,S]}),done"
        ),
        Case(
            "template-validation-child",
            "{error,{start_spec,missing_id}}=supervisor:start_link(oracle_simple,{#{strategy=>simple_one_for_one},[#{start=>{oracle_simple,child,[prefix]}}]}),done"
        ),
        Case(
            "auto-shutdown-invalid-any",
            "{error,{supervisor_data,{bad_combination,[{auto_shutdown,any_significant},{strategy,simple_one_for_one}]}}}=supervisor:start_link(oracle_simple,{#{strategy=>simple_one_for_one,auto_shutdown=>any_significant},[]}),done"
        ),
        Case(
            "auto-shutdown-invalid-all",
            "{error,{supervisor_data,{bad_combination,[{auto_shutdown,all_significant},{strategy,simple_one_for_one}]}}}=supervisor:start_link(oracle_simple,{#{strategy=>simple_one_for_one,auto_shutdown=>all_significant},[]}),done"
        ),
        Case(
            "legacy-flags",
            "{ok,P}=supervisor:start_link(oracle_simple,{{simple_one_for_one,2,5},[template(temporary)]})," + Zero + "stop(P)"
        ),
        Case(
            "permanent-normal-restart",
            "R=self(),{ok,P}=start(template(permanent)),{ok,C}=supervisor:start_child(P,[R,first]),receive {started,first,prefix,C}->ok end,C!{finish,normal},receive {started,first,prefix,D}->true=C=/=D end,[{undefined,D,worker,_}]=supervisor:which_children(P),stop(P)"
        ),
        Case(
            "transient-abnormal-restart",
            "R=self(),{ok,P}=start(template(transient)),{ok,C}=supervisor:start_child(P,[R,first]),receive {started,first,prefix,C}->ok end,C!{finish,failed},receive {started,first,prefix,D}->true=C=/=D end,[{undefined,D,worker,_}]=supervisor:which_children(P),stop(P)"
        ),
        Case(
            "transient-normal-removes",
            "{ok,P}=start(template(transient)),{ok,C}=supervisor:start_child(P,[self(),first]),C!{finish,normal},ok=empty(P)," + Zero + "stop(P)"
        ),
        Case(
            "temporary-abnormal-removes",
            "{ok,P}=start(template(temporary)),{ok,C}=supervisor:start_child(P,[self(),first]),C!{finish,failed},ok=empty(P)," + Zero + "stop(P)"
        ),
        Case(
            "automatic-retry-arguments",
            "R=self(),Ref=make_ref(),{ok,P}=start(template(permanent)),{ok,C}=supervisor:start_child(P,[R,{recover,first,Ref}]),receive {started,first,prefix,C}->ok end,C!{finish,failed},receive {Ref,[{specs,1},{active,0},{supervisors,0},{workers,1}]}->ok end,receive {started,first,prefix,D}->true=C=/=D end,[{undefined,D,worker,_}]=supervisor:which_children(P),stop(P)"
        ),
        Case(
            "automatic-ignore-removes",
            "R=self(),{ok,P}=start(template(permanent)),{ok,C}=supervisor:start_child(P,[R,{restart_ignore,first}]),receive {started,first,prefix,C}->ok end,C!{finish,failed},receive ignored->ok end,ok=empty(P)," + Zero + "stop(P)"
        ),
        Case(
            "which-child",
            "{ok,P}=start(template(temporary)),{ok,C}=supervisor:start_child(P,[self(),first]),{ok,{undefined,C,worker,[oracle_simple]}}=supervisor:which_child(P,C),{error,simple_one_for_one}=supervisor:which_child(P,template),ok=supervisor:terminate_child(P,C),{error,not_found}=supervisor:which_child(P,C),stop(P)"
        ),
        Case(
            "which-child-unrelated-dead",
            Dead + "{ok,P}=start(template(permanent)),{error,not_found}=supervisor:which_child(P,D),stop(P)"
        ),
        Case(
            "get-childspec",
            "S=template(transient),{ok,P}=start(S),{ok,C}=supervisor:start_child(P,[self(),first]),{ok,#{id:=template,start:={oracle_simple,child,[prefix]},restart:=transient,significant:=false,shutdown:=5000,type:=worker,modules:=[oracle_simple]}}=supervisor:get_childspec(P,C),{ok,#{id:=template}}=supervisor:get_childspec(P,template),stop(P)"
        ),
        Case(
            "get-childspec-unrelated-dead",
            Dead + "{ok,P}=start(template(permanent)),{ok,#{id:=template}}=supervisor:get_childspec(P,D),stop(P)"
        ),
        Case(
            "get-childspec-unrelated-alive",
            "C=spawn(fun()->receive finish->ok end end),{ok,P}=start(template(permanent)),{error,not_found}=supervisor:get_childspec(P,C),C!finish,stop(P)"
        ),
        Case(
            "supervisor-child-count",
            "S=(template(temporary))#{type=>supervisor},{ok,P}=start(S),{ok,C}=supervisor:start_child(P,[self(),first]),[{specs,1},{active,1},{supervisors,1},{workers,0}]=supervisor:count_children(P),ok=supervisor:terminate_child(P,C),stop(P)"
        ),
        Case(
            "stop-three",
            "{ok,P}=start(template(temporary)),M=monitor(process,P),ok=supervisor:stop(P,custom,infinity),ok=down(M,P,custom),done"
        ),
        Case(
            "bulk-shutdown",
            "R=self(),S=(template(temporary))#{shutdown=>infinity},{ok,P}=start(S),{ok,C}=supervisor:start_child(P,[R,{stubborn,first}]),{ok,D}=supervisor:start_child(P,[R,{stubborn,second}]),M=monitor(process,P),spawn(fun()->supervisor:stop(P) end),receive {shutdown,first,C}->ok after 2000->missing end,receive {shutdown,second,D}->ok after 2000->missing end,C!finish,D!finish,ok=down(M,P,normal),done"
        ),
        Case(
            "bulk-timeout-kill",
            "R=self(),S=(template(temporary))#{shutdown=>0},{ok,P}=start(S),{ok,C}=supervisor:start_child(P,[R,{stubborn,first}]),{ok,D}=supervisor:start_child(P,[R,{stubborn,second}]),M=monitor(process,C),N=monitor(process,D),ok=supervisor:stop(P),ok=down(M,C,killed),ok=down(N,D,killed),done"
        ),
        Case(
            "ordinary-query-spec",
            "S=template(transient),{ok,P}=supervisor:start_link(oracle_simple,{#{},[S#{start=>{oracle_simple,child,[prefix,self(),first]}}]}),receive {started,first,prefix,C}->ok end,{ok,{template,C,worker,[oracle_simple]}}=supervisor:which_child(P,template),{ok,#{id:=template,significant:=false}}=supervisor:get_childspec(P,template),{error,not_found}=supervisor:get_childspec(P,missing),stop(P)"
        ),
        Case(
            "ordinary-dynamic-temporary-args",
            "{ok,P}=supervisor:start_link(oracle_simple,{#{},[]}),S=(template(temporary))#{start=>{oracle_simple,child,[prefix,self(),first]}},{ok,_}=supervisor:start_child(P,S),{ok,#{start:={oracle_simple,child,undefined}}}=supervisor:get_childspec(P,template),stop(P)"
        )
    ];
}
