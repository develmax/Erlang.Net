namespace Erlang.Differential;

public static class SupervisorDynamicCases
{
    private static Term A(string value) => Term.A(value);

    private static Term Error(string reason) => Term.Tuple(A("error"), A(reason));

    private static CompiledModuleCase Case(string name, string body, Term expected) => new(
        "otp-supervisor-dynamic-" + name,
        "-module(oracle_dynamic). -export([run/0,init/1,child/1,handle_call/3,handle_cast/2,terminate/2]). run()->" + body + ". " +
        "init({worker,{notify,P,S}})->process_flag(trap_exit,true),{ok,{notify,P,S}}; init({worker,S})->{ok,S}; init({flags,F,C})->{ok,{F,C}}; init(C)->{ok,{#{},C}}. " +
        "child(ignore)->ignore; child({error,R})->{error,R}; child({exit,R})->exit(R); child({raw,R})->R; child({throw,R})->throw(R); " +
        "child({extra,S})->{ok,P}=gen_server:start_link(oracle_dynamic,{worker,S},[]),{ok,P,extra}; " +
        "child(retry)->case get(attempt) of undefined->put(attempt,1),gen_server:start_link(oracle_dynamic,{worker,1},[]); 1->put(attempt,2),{error,failed}; _->gen_server:start_link(oracle_dynamic,{worker,3},[]) end; " +
        "child({notify,P,S})->R=gen_server:start_link(oracle_dynamic,{worker,{notify,P,S}},[]),P!{started,S,R},R; " +
        "child(S)->gen_server:start_link(oracle_dynamic,{worker,S},[]). " +
        "handle_call(get,_,S)->{reply,S,S}. handle_cast(_,S)->{noreply,S}. " +
        "terminate(R,{notify,P,S})->P!{terminated,S,R}; terminate(_,_)->ok.",
        Term.Tuple(A("ok"), expected)
    );

    private static Term FailedStart(Term argument, Term reason) => Term.Tuple(
        A("error"),
        Term.Tuple(
            reason,
            Term.Tuple(
                A("child"),
                A("undefined"),
                A("first"),
                Term.Tuple(A("oracle_dynamic"), A("child"), Term.List(argument)),
                A("permanent"),
                A("false"),
                Term.I(5000),
                A("worker"),
                Term.List(A("oracle_dynamic"))
            )
        )
    );

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = [
        Case(
            "already-started-arbitrary-reason",
            "{ok,P}=supervisor:start_link(oracle_dynamic,[]),R=supervisor:start_child(P,#{id=>first,start=>{oracle_dynamic,child,[{error,{already_started,other}}]}}),[] =supervisor:which_children(P),ok=gen_server:stop(P),R",
            Term.Tuple(A("error"),Term.Tuple(A("already_started"),A("other")))
        ),
        Case(
            "temporary-natural-exit",
            "{ok,P}=supervisor:start_link(oracle_dynamic,[]),S=#{id=>first,start=>{oracle_dynamic,child,[1]},restart=>temporary},{ok,C}=supervisor:start_child(P,S),ok=gen_server:stop(C),[] =supervisor:which_children(P),{ok,D}=supervisor:start_child(P,S),R=C=/=D,ok=gen_server:stop(P),R",
            A("true")
        ),
        Case(
            "transient-normal-exit",
            "{ok,P}=supervisor:start_link(oracle_dynamic,[]),S=#{id=>first,start=>{oracle_dynamic,child,[1]},restart=>transient},{ok,C}=supervisor:start_child(P,S),ok=gen_server:stop(C),[{first,undefined,worker,_}]=supervisor:which_children(P),{error,already_present}=supervisor:start_child(P,S),{ok,D}=supervisor:restart_child(P,first),R=C=/=D,ok=gen_server:stop(P),R",
            A("true")
        ),
        Case(
            "static-ignore-temporary",
            "S=#{id=>first,start=>{oracle_dynamic,child,[ignore]},restart=>temporary},{ok,P}=supervisor:start_link(oracle_dynamic,[S]),{ok,undefined}=supervisor:start_child(P,S),R=supervisor:delete_child(P,first),ok=gen_server:stop(P),R",
            Error("not_found")
        ),
        Case(
            "one-for-all",
            "{ok,P}=supervisor:start_link(oracle_dynamic,{flags,#{strategy=>one_for_all,intensity=>5},[]}),{ok,C}=supervisor:start_child(P,#{id=>first,start=>{oracle_dynamic,child,[{notify,self(),1}]}}),{ok,D}=supervisor:start_child(P,#{id=>second,start=>{oracle_dynamic,child,[{notify,self(),2}]}}),{ok,_}=supervisor:start_child(P,#{id=>third,start=>{oracle_dynamic,child,[3]},restart=>temporary}),receive {started,1,_}->ok after 1000->missing end,receive {started,2,_}->ok after 1000->missing end,exit(C,kill),N=receive {started,1,{ok,N0}}->N0 after 1000->missing end,Q=receive {started,2,{ok,Q0}}->Q0 after 1000->missing end,[{second,Q,worker,_},{first,N,worker,_}]=supervisor:which_children(P),R={C=/=N,D=/=Q},ok=gen_server:stop(P),R",
            Term.Tuple(A("true"),A("true"))
        ),
        Case(
            "rest-for-one",
            "{ok,P}=supervisor:start_link(oracle_dynamic,{flags,#{strategy=>rest_for_one,intensity=>5},[]}),{ok,C}=supervisor:start_child(P,#{id=>first,start=>{oracle_dynamic,child,[1]}}),{ok,D}=supervisor:start_child(P,#{id=>second,start=>{oracle_dynamic,child,[{notify,self(),2}]}}),{ok,E}=supervisor:start_child(P,#{id=>third,start=>{oracle_dynamic,child,[{notify,self(),3}]}}),receive {started,2,_}->ok after 1000->missing end,receive {started,3,_}->ok after 1000->missing end,exit(D,kill),N=receive {started,2,{ok,N0}}->N0 after 1000->missing end,Q=receive {started,3,{ok,Q0}}->Q0 after 1000->missing end,[{third,Q,worker,_},{second,N,worker,_},{first,C,worker,_}]=supervisor:which_children(P),R={D=/=N,E=/=Q,gen_server:call(C,get)},ok=gen_server:stop(P),R",
            Term.Tuple(A("true"),A("true"),Term.I(1))
        ),
        Case(
            "lifecycle",
            "{ok,P}=supervisor:start_link(oracle_dynamic,[]),S=#{id=>first,start=>{oracle_dynamic,child,[7]}},{ok,C}=supervisor:start_child(P,S),7=gen_server:call(C,get),{error,{already_started,C}}=supervisor:start_child(P,S),{error,running}=supervisor:restart_child(P,first),{error,running}=supervisor:delete_child(P,first),M=monitor(process,C),ok=supervisor:terminate_child(P,first),receive {'DOWN',M,process,C,shutdown}->ok after 1000->missing end,[{first,undefined,worker,[oracle_dynamic]}]=supervisor:which_children(P),{error,already_present}=supervisor:start_child(P,S),ok=supervisor:terminate_child(P,first),{ok,D}=supervisor:restart_child(P,first),V=gen_server:call(D,get),ok=supervisor:terminate_child(P,first),ok=supervisor:delete_child(P,first),[] =supervisor:which_children(P),ok=gen_server:stop(P),{V,C=/=D}",
            Term.Tuple(Term.I(7),A("true"))
        ),
        Case(
            "missing",
            "{ok,P}=supervisor:start_link(oracle_dynamic,[]),R={supervisor:terminate_child(P,missing),supervisor:restart_child(P,missing),supervisor:delete_child(P,missing)},ok=gen_server:stop(P),R",
            Term.Tuple(Error("not_found"),Error("not_found"),Error("not_found"))
        ),
        Case(
            "legacy",
            "{ok,P}=supervisor:start_link(oracle_dynamic,[]),{ok,C}=supervisor:start_child(P,{first,{oracle_dynamic,child,[8]},transient,5000,worker,[oracle_dynamic]}),V=gen_server:call(C,get),ok=gen_server:stop(P),V",
            Term.I(8)
        ),
        Case(
            "ignore-permanent",
            "{ok,P}=supervisor:start_link(oracle_dynamic,[]),S=#{id=>first,start=>{oracle_dynamic,child,[ignore]}},{ok,undefined}=supervisor:start_child(P,S),{error,already_present}=supervisor:start_child(P,S),R=supervisor:restart_child(P,first),ok=supervisor:delete_child(P,first),ok=gen_server:stop(P),R",
            Term.Tuple(A("ok"),A("undefined"))
        ),
        Case(
            "ignore-temporary",
            "{ok,P}=supervisor:start_link(oracle_dynamic,[]),S=#{id=>first,start=>{oracle_dynamic,child,[ignore]},restart=>temporary},{ok,undefined}=supervisor:start_child(P,S),{ok,undefined}=supervisor:start_child(P,S),R={supervisor:which_children(P),supervisor:restart_child(P,first)},ok=gen_server:stop(P),R",
            Term.Tuple(Nil.Value,Error("not_found"))
        ),
        Case(
            "extra",
            "{ok,P}=supervisor:start_link(oracle_dynamic,[]),{ok,C,extra}=supervisor:start_child(P,#{id=>first,start=>{oracle_dynamic,child,[{extra,9}]}}),ok=supervisor:terminate_child(P,first),{ok,D,extra}=supervisor:restart_child(P,first),R={C=/=D,gen_server:call(D,get)},ok=gen_server:stop(P),R",
            Term.Tuple(A("true"),Term.I(9))
        ),
        Case(
            "temporary-termination",
            "{ok,P}=supervisor:start_link(oracle_dynamic,[]),S=#{id=>first,start=>{oracle_dynamic,child,[1]},restart=>temporary},{ok,C}=supervisor:start_child(P,S),ok=supervisor:terminate_child(P,first),{error,not_found}=supervisor:restart_child(P,first),{error,not_found}=supervisor:delete_child(P,first),{ok,D}=supervisor:start_child(P,S),R={C=/=D,gen_server:call(D,get)},ok=gen_server:stop(P),R",
            Term.Tuple(A("true"),Term.I(1))
        ),
        Case(
            "replacement",
            "{ok,P}=supervisor:start_link(oracle_dynamic,[]),{ok,C}=supervisor:start_child(P,#{id=>first,start=>{oracle_dynamic,child,[1]}}),ok=supervisor:terminate_child(P,first),ok=supervisor:delete_child(P,first),{ok,D}=supervisor:start_child(P,#{id=>first,start=>{oracle_dynamic,child,[2]}}),R={C=/=D,gen_server:call(D,get)},ok=gen_server:stop(P),R",
            Term.Tuple(A("true"),Term.I(2))
        ),
        Case(
            "restart-failure-retry",
            "{ok,P}=supervisor:start_link(oracle_dynamic,[]),{ok,_}=supervisor:start_child(P,#{id=>first,start=>{oracle_dynamic,child,[retry]}}),ok=supervisor:terminate_child(P,first),{error,failed}=supervisor:restart_child(P,first),[{first,undefined,worker,_}]=supervisor:which_children(P),{ok,D}=supervisor:restart_child(P,first),V=gen_server:call(D,get),ok=gen_server:stop(P),V",
            Term.I(3)
        ),
        Case(
            "order-and-counts",
            "{ok,P}=supervisor:start_link(oracle_dynamic,[]),S=#{id=>first,start=>{oracle_dynamic,child,[ignore]}},T=#{id=>second,start=>{oracle_dynamic,child,[ignore]},type=>supervisor},{ok,undefined}=supervisor:start_child(P,S),{ok,undefined}=supervisor:start_child(P,T),[{second,undefined,supervisor,_},{first,undefined,worker,_}]=supervisor:which_children(P),ok=supervisor:delete_child(P,first),{ok,undefined}=supervisor:start_child(P,S),[{first,undefined,worker,_},{second,undefined,supervisor,_}]=supervisor:which_children(P),N=supervisor:count_children(P),ok=gen_server:stop(P),N",
            Term.List(
                Term.Tuple(A("specs"),Term.I(2)),
                Term.Tuple(A("active"),Term.I(0)),
                Term.Tuple(A("supervisors"),Term.I(1)),
                Term.Tuple(A("workers"),Term.I(1))
            )
        ),
        Case(
            "validate-before-duplicate",
            "{ok,P}=supervisor:start_link(oracle_dynamic,[#{id=>first,start=>{oracle_dynamic,child,[ignore]}}]),R=supervisor:start_child(P,#{id=>first}),ok=gen_server:stop(P),R",
            Error("missing_start")
        ),
        Case(
            "invalid-spec",
            "{ok,P}=supervisor:start_link(oracle_dynamic,[]),R=supervisor:start_child(P,bad),[] =supervisor:which_children(P),ok=gen_server:stop(P),R",
            Term.Tuple(A("error"),Term.Tuple(A("invalid_child_spec"),A("bad")))
        ),
        Case(
            "failed-start",
            "{ok,P}=supervisor:start_link(oracle_dynamic,[]),R=supervisor:start_child(P,#{id=>first,start=>{oracle_dynamic,child,[{error,failed}]}}),[] =supervisor:which_children(P),{ok,C}=supervisor:start_child(P,#{id=>first,start=>{oracle_dynamic,child,[1]}}),1=gen_server:call(C,get),ok=gen_server:stop(P),R",
            FailedStart(Term.Tuple(A("error"),A("failed")),A("failed"))
        ),
        Case(
            "exit-start",
            "{ok,P}=supervisor:start_link(oracle_dynamic,[]),R=supervisor:start_child(P,#{id=>first,start=>{oracle_dynamic,child,[{exit,failed}]}}),ok=gen_server:stop(P),R",
            FailedStart(Term.Tuple(A("exit"),A("failed")),Term.Tuple(A("EXIT"),A("failed")))
        ),
        Case(
            "raw-start",
            "{ok,P}=supervisor:start_link(oracle_dynamic,[]),R=supervisor:start_child(P,#{id=>first,start=>{oracle_dynamic,child,[{raw,bad}]}}),ok=gen_server:stop(P),R",
            FailedStart(Term.Tuple(A("raw"),A("bad")),A("bad"))
        ),
        Case(
            "throw-return",
            "{ok,P}=supervisor:start_link(oracle_dynamic,[]),R=supervisor:start_child(P,#{id=>first,start=>{oracle_dynamic,child,[{throw,ignore}]}}),ok=supervisor:delete_child(P,first),ok=gen_server:stop(P),R",
            Term.Tuple(A("ok"),A("undefined"))
        ),
        Case(
            "already-started-return",
            "{ok,C}=gen_server:start_link(oracle_dynamic,{worker,1},[]),{ok,P}=supervisor:start_link(oracle_dynamic,[]),{error,{already_started,C}}=supervisor:start_child(P,#{id=>first,start=>{oracle_dynamic,child,[{error,{already_started,C}}]}}),[] =supervisor:which_children(P),ok=gen_server:stop(P),ok=gen_server:stop(C),ok",
            A("ok")
        ),
        Case(
            "shutdown-ack",
            "{ok,P}=supervisor:start_link(oracle_dynamic,[]),{ok,_}=supervisor:start_child(P,#{id=>first,start=>{oracle_dynamic,child,[{notify,self(),1}]}}),receive {started,1,_}->ok after 1000->missing end,ok=supervisor:terminate_child(P,first),R=receive {terminated,1,shutdown}->done after 1000->missing end,ok=gen_server:stop(P),R",
            A("done")
        ),
        Case(
            "named",
            "{ok,_}=supervisor:start_link({local,oracle_dynamic_name},oracle_dynamic,[]),{ok,C}=supervisor:start_child(oracle_dynamic_name,#{id=>first,start=>{oracle_dynamic,child,[5]}}),5=gen_server:call(C,get),ok=supervisor:terminate_child(oracle_dynamic_name,first),ok=supervisor:delete_child(oracle_dynamic_name,first),ok=gen_server:stop(oracle_dynamic_name),ok",
            A("ok")
        ),
        Case(
            "missing-supervisor",
            "try supervisor:delete_child(oracle_missing,first) catch exit:R->R end",
            Term.Tuple(
                A("noproc"),
                Term.Tuple(A("gen_server"),A("call"),Term.List(A("oracle_missing"),Term.Tuple(A("delete_child"),A("first")),A("infinity")))
            )
        )
    ];
}
