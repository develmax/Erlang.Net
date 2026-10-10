namespace Erlang.Differential;

public static class OtpBehaviourCases
{
    private static Term A(string value) => Term.A(value);

    private static CompiledModuleCase Server(
        string name,
        string body,
        Term expected,
        string callbacks = ""
    ) => new(
        name,
        "-module(oracle_otp_server). -export([run/0,init/1,handle_call/3,handle_cast/2,handle_info/2,handle_continue/2,terminate/2]). run()->" + body + ". " +
        "init(ignore)->ignore; init({stop,R})->{stop,R}; init({error,R})->{error,R}; init({throw,V})->throw(V); init({action,S,A})->{ok,S,A}; init(S)->{ok,S}. " +
        "handle_call(get,_,S)->{reply,S,S}; handle_call(self,_,S)->{reply,self(),S}; handle_call({stop,R},_,S)->{stop,R,done,S}; handle_call({deferred,V},From,S)->gen_server:reply(From,V),{noreply,S}; handle_call({throw,V},_,_)->throw(V); handle_call({return,V},_,_)->V. " +
        "handle_cast({set,S},_)->{noreply,S}; handle_cast({stop,R},S)->{stop,R,S}. handle_info(timeout,{notify,P})->P!timed,{noreply,timed}; handle_info(timeout,_)->{noreply,timed}; handle_info({set,S},_)->{noreply,S}; handle_info(_,S)->{noreply,S}. " +
        "handle_continue(S,_)->{noreply,S}. terminate(R,{notify,P})->P!{terminated,R}; terminate(_,_) ->ok. " + callbacks,
        Term.Tuple(A("ok"), expected)
    );

    private static CompiledModuleCase Supervisor(string name, string body, Term expected) => new(
        name,
        "-module(oracle_otp_supervisor). -export([run/0,init/1,child/1,handle_call/3,handle_cast/2]). run()->" + body + ". " +
        "init({server,S})->{ok,S}; init(ignore)->ignore; init({raw,R})->R; init({flags,F,C})->{ok,{F,C}}; init(C)->{ok,{#{},C}}. " +
        "child({notify,P,N})->R=gen_server:start_link(oracle_otp_supervisor,{server,N},[]),P!{started,N,R},R; child(ignore)->ignore; child({error,R})->{error,R}; child(S)->gen_server:start_link(oracle_otp_supervisor,{server,S},[]). " +
        "handle_call(get,_,S)->{reply,S,S}. handle_cast(_,S)->{noreply,S}.",
        Term.Tuple(A("ok"), expected)
    );

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = [
        Server(
            "otp-server-timeout-action",
            "{ok,P}=gen_server:start(oracle_otp_server,{action,{notify,self()},0},[]),receive timed->ok after 1000->missing end,V=gen_server:call(P,get),ok=gen_server:stop(P),V",
            A("timed")
        ),
        Supervisor(
            "otp-supervisor-invalid-restart",
            "supervisor:check_childspecs([#{id=>first,start=>{oracle_otp_supervisor,child,[]},restart=>wrong}])",
            Term.Tuple(A("error"),Term.Tuple(A("invalid_restart_type"),A("wrong")))
        ),
        Supervisor(
            "otp-supervisor-invalid-mfa",
            "supervisor:check_childspecs([#{id=>first,start=>wrong}])",
            Term.Tuple(A("error"),Term.Tuple(A("invalid_mfa"),A("wrong")))
        ),
        Supervisor(
            "otp-supervisor-invalid-shutdown",
            "supervisor:check_childspecs([#{id=>first,start=>{oracle_otp_supervisor,child,[]},shutdown=>-1}])",
            Term.Tuple(A("error"),Term.Tuple(A("invalid_shutdown"),Term.I(-1)))
        ),
        Supervisor(
            "otp-supervisor-invalid-modules",
            "supervisor:check_childspecs([#{id=>first,start=>{oracle_otp_supervisor,child,[]},modules=>[1]}])",
            Term.Tuple(A("error"),Term.Tuple(A("invalid_module"),Term.I(1)))
        ),
        Supervisor(
            "otp-supervisor-bad-init-return",
            "supervisor:start_link(oracle_otp_supervisor,{raw,wrong})",
            Term.Tuple(A("error"),Term.Tuple(A("bad_return"),Term.Tuple(A("oracle_otp_supervisor"),A("init"),A("wrong"))))
        ),
        new(
            "otp-server-optional-callbacks",
            "-module(oracle_otp_optional). -export([run/0,init/1,handle_call/3]). run()->{ok,P}=gen_server:start(oracle_otp_optional,8,[]),P!ignored,V=gen_server:call(P,get),ok=gen_server:stop(P),V. init(S)->{ok,S}. handle_call(get,_,S)->{reply,S,S}.",
            Term.Tuple(A("ok"),Term.I(8))
        ),
        Server(
            "otp-server-named-start-link-stop-three",
            "{ok,P}=gen_server:start_link({local,oracle_linked},oracle_otp_server,6,[]),V=gen_server:call(P,get,1000),ok=gen_server:stop(P,normal,1000),V",
            Term.I(6)
        ),
        Server(
            "otp-server-call-bad-return",
            "{ok,P}=gen_server:start(oracle_otp_server,0,[]),try gen_server:call(P,{return,wrong}) catch exit:{R,{gen_server,call,[P,_]}}->R end",
            Term.Tuple(A("bad_return_value"),A("wrong"))
        ),
        Server(
            "otp-server-init-bad-return",
            "gen_server:start(oracle_otp_server,{throw,wrong},[])",
            Term.Tuple(A("error"),Term.Tuple(A("bad_return_value"),A("wrong")))
        ),
        Supervisor(
            "otp-supervisor-named",
            "{ok,P}=supervisor:start_link({local,oracle_sup},oracle_otp_supervisor,[]),{error,{already_started,P}}=supervisor:start_link({local,oracle_sup},oracle_otp_supervisor,[]),[]=supervisor:which_children(oracle_sup),ok=gen_server:stop(P),whereis(oracle_sup)",
            A("undefined")
        ),
        Supervisor(
            "otp-supervisor-restart-permanent",
            "{ok,P}=supervisor:start_link(oracle_otp_supervisor,[#{id=>first,start=>{oracle_otp_supervisor,child,[{notify,self(),1}]}}]),receive {started,1,{ok,C}}->ok end,ok=gen_server:stop(C),D=receive {started,1,{ok,D0}}->D0 after 1000->missing end,7=gen_server:call(D,get)+6,ok=gen_server:stop(P),C=/=D",
            A("true")
        ),
        Supervisor(
            "otp-supervisor-child-query-order",
            "{ok,P}=supervisor:start_link(oracle_otp_supervisor,[#{id=>one,start=>{oracle_otp_supervisor,child,[1]}},#{id=>two,start=>{oracle_otp_supervisor,child,[2]}}]),[{two,_,_,_},{one,_,_,_}]=supervisor:which_children(P),N=supervisor:count_children(P),ok=gen_server:stop(P),N",
            Term.List(
                Term.Tuple(A("specs"),Term.I(2)),
                Term.Tuple(A("active"),Term.I(2)),
                Term.Tuple(A("supervisors"),Term.I(0)),
                Term.Tuple(A("workers"),Term.I(2))
            )
        ),
        Server(
            "otp-server-call-cast-info",
            "{ok,P}=gen_server:start(oracle_otp_server,1,[]),A=gen_server:call(P,get),gen_server:cast(P,{set,2}),B=gen_server:call(P,get,infinity),P!{set,3},C=gen_server:call(P,get),ok=gen_server:stop(P),{A,B,C}",
            Term.Tuple(Term.I(1),Term.I(2),Term.I(3))
        ),
        Server(
            "otp-server-init-continue",
            "{ok,P}=gen_server:start_link(oracle_otp_server,{action,0,{continue,7}},[]),V=gen_server:call(P,get),ok=gen_server:stop(P),V",
            Term.I(7)
        ),
        Server(
            "otp-server-deferred-reply",
            "{ok,P}=gen_server:start(oracle_otp_server,0,[]),V=gen_server:call(P,{deferred,8},1000),ok=gen_server:stop(P),V",
            Term.I(8)
        ),
        Server(
            "otp-server-callback-self",
            "{ok,P}=gen_server:start(oracle_otp_server,0,[]),V=gen_server:call(P,self),ok=gen_server:stop(P),V=:=P",
            A("true")
        ),
        Server("otp-server-init-ignore", "gen_server:start_link(oracle_otp_server,ignore,[])", A("ignore")),
        Server("otp-server-init-stop", "gen_server:start_link(oracle_otp_server,{stop,failed},[])", Term.Tuple(A("error"),A("failed"))),
        Server(
            "otp-server-init-error",
            "gen_server:start_link(oracle_otp_server,{error,failed},[])",
            Term.Tuple(A("error"),A("failed"))
        ),
        Server(
            "otp-server-init-throw-result",
            "{ok,P}=gen_server:start(oracle_otp_server,{throw,{ok,9}},[]),V=gen_server:call(P,get),ok=gen_server:stop(P),V",
            Term.I(9)
        ),
        Server(
            "otp-server-call-throw-result",
            "{ok,P}=gen_server:start(oracle_otp_server,0,[]),V=gen_server:call(P,{throw,{reply,yes,2}}),S=gen_server:call(P,get),ok=gen_server:stop(P),{V,S}",
            Term.Tuple(A("yes"),Term.I(2))
        ),
        Server(
            "otp-server-stop-reply-terminate-order",
            "{ok,P}=gen_server:start(oracle_otp_server,{notify,self()},[]),done=gen_server:call(P,{stop,normal}),receive {terminated,normal}->ok after 1000->missing end",
            A("ok")
        ),
        Server(
            "otp-server-named-start",
            "{ok,P}=gen_server:start({local,oracle_local},oracle_otp_server,5,[]),{error,{already_started,P}}=gen_server:start_link({local,oracle_local},oracle_otp_server,0,[]),V=gen_server:call(oracle_local,get),ok=gen_server:stop(oracle_local),{V,whereis(oracle_local)}",
            Term.Tuple(Term.I(5),A("undefined"))
        ),
        Server(
            "otp-server-missing-call",
            "try gen_server:call(oracle_missing,hi) catch exit:R->R end",
            Term.Tuple(A("noproc"),Term.Tuple(A("gen_server"),A("call"),Term.List(A("oracle_missing"),A("hi"))))
        ),
        Server("otp-server-missing-cast", "gen_server:cast(oracle_missing,hi)", A("ok")),
        Supervisor(
            "otp-supervisor-empty",
            "{ok,P}=supervisor:start_link(oracle_otp_supervisor,[]),C=supervisor:which_children(P),N=supervisor:count_children(P),ok=gen_server:stop(P),{C,N}",
            Term.Tuple(
                Nil.Value,
                Term.List(
                    Term.Tuple(A("specs"),Term.I(0)),
                    Term.Tuple(A("active"),Term.I(0)),
                    Term.Tuple(A("supervisors"),Term.I(0)),
                    Term.Tuple(A("workers"),Term.I(0))
                )
            )
        ),
        Supervisor("otp-supervisor-ignore", "supervisor:start_link(oracle_otp_supervisor,ignore)", A("ignore")),
        Supervisor(
            "otp-supervisor-map-child",
            "{ok,P}=supervisor:start_link(oracle_otp_supervisor,[#{id=>first,start=>{oracle_otp_supervisor,child,[7]}}]),[{first,C,worker,[oracle_otp_supervisor]}]=supervisor:which_children(P),V=gen_server:call(C,get),ok=gen_server:stop(P),V",
            Term.I(7)
        ),
        Supervisor(
            "otp-supervisor-legacy-child-flags",
            "{ok,P}=supervisor:start_link(oracle_otp_supervisor,{flags,{one_for_one,1,5},[{first,{oracle_otp_supervisor,child,[8]},temporary,5000,worker,[oracle_otp_supervisor]}]}),[{first,C,worker,_}]=supervisor:which_children(P),V=gen_server:call(C,get),ok=gen_server:stop(P),V",
            Term.I(8)
        ),
        Supervisor(
            "otp-supervisor-ignore-child",
            "{ok,P}=supervisor:start_link(oracle_otp_supervisor,[#{id=>first,start=>{oracle_otp_supervisor,child,[ignore]}}]),C=supervisor:which_children(P),ok=gen_server:stop(P),C",
            Term.List(Term.Tuple(
                A("first"),
                A("undefined"),
                A("worker"),
                Term.List(A("oracle_otp_supervisor"))
            ))
        ),
        Supervisor(
            "otp-supervisor-start-failure",
            "supervisor:start_link(oracle_otp_supervisor,[#{id=>first,start=>{oracle_otp_supervisor,child,[{error,failed}]}}])",
            Term.Tuple(A("error"),Term.Tuple(A("shutdown"),Term.Tuple(A("failed_to_start_child"),A("first"),A("failed"))))
        ),
        Supervisor(
            "otp-supervisor-invalid-intensity",
            "supervisor:start_link(oracle_otp_supervisor,{flags,#{intensity=>-1},[]})",
            Term.Tuple(A("error"),Term.Tuple(A("supervisor_data"),Term.Tuple(A("invalid_intensity"),Term.I(-1))))
        ),
        Supervisor(
            "otp-supervisor-missing-id",
            "supervisor:check_childspecs([#{start=>{oracle_otp_supervisor,child,[]}}])",
            Term.Tuple(A("error"),A("missing_id"))
        ),
        Supervisor("otp-supervisor-missing-start", "supervisor:check_childspecs([#{id=>first}])", Term.Tuple(A("error"),A("missing_start"))),
        Supervisor(
            "otp-supervisor-valid-check",
            "supervisor:check_childspecs([#{id=>first,start=>{oracle_otp_supervisor,child,[ignore]}}])",
            A("ok")
        ),
        Supervisor(
            "otp-supervisor-duplicate-id",
            "S=#{id=>first,start=>{oracle_otp_supervisor,child,[ignore]}},supervisor:check_childspecs([S,S])",
            Term.Tuple(A("error"),Term.Tuple(A("duplicate_child_name"),A("first")))
        )
    ];
}
