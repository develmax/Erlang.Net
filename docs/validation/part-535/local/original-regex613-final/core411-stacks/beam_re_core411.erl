-module(beam_re_core411).
-export([run/0,run_stacks/0,worker/2]).

-define(ANSI_REGEXP, <<"^[\e",194,155,"][[\\]()#;?]*(?:(?:(?:(?:;[-a-zA-Z\\d\\/#&.:=?%@~_]+)*|[a-zA-Z\\d]+(?:;[-a-zA-Z\\d\\/#&.:=?%@~_]*)*)?",7,")|(?:(?:\\d{1,4}(?:;\\d{0,4})*)?[\\dA-PR-TZcf-nq-uy=><~]))">>).

cases() -> [ansi_match,ansi_nomatch,ansi_csi,capture_groups,optional_capture,
    empty_pattern,embedded_nul,default_bytes,compile_missing_bracket,compile_bad_atom,
    compile_bad_options,compile_improper_options,run_bad_subject,run_bad_pattern,
    compiled_protocol,reuse_compiled].
run() -> [{Case,capture(Case,default)} || Case <- cases()].
run_stacks() -> [{Depth,[{Case,capture(Case,Depth)} || Case <- cases()]} || Depth <- [default,0,1,2,8]].
capture(Case,Depth) ->
    {Pid,Ref}=spawn_monitor(?MODULE,worker,[Case,Depth]),
    receive {'DOWN',Ref,process,Pid,{observation,Value}} -> Value;
            {'DOWN',Ref,process,Pid,Reason} -> {unexpected_down,Reason}
    after 10000 -> error(observer_timeout)
    end.
worker(Case,Depth) ->
    case Depth of default -> ok; _ -> erlang:system_flag(backtrace_depth,Depth) end,
    Value=try probe(Case) of Result -> {ok,Result}
          catch Class:Reason:Stack -> {exception,Class,Reason,Stack} end,
    exit({observation,Value}).
probe(ansi_match) -> {ok,MP}=re:compile(?ANSI_REGEXP,[unicode]),re:run(<<27,"[31mhello">>,MP);
probe(ansi_nomatch) -> {ok,MP}=re:compile(?ANSI_REGEXP,[unicode]),re:run(<<"plain">>,MP);
probe(ansi_csi) -> {ok,MP}=re:compile(?ANSI_REGEXP,[unicode]),re:run(<<194,155,"2Jtail">>,MP);
probe(capture_groups) -> {ok,MP}=re:compile(<<"(a)(b)">>,[]),re:run(<<"zab">>,MP);
probe(optional_capture) -> {ok,MP}=re:compile(<<"(a)?b">>,[]),re:run(<<"b">>,MP);
probe(empty_pattern) -> {ok,MP}=re:compile(<<>>,[]),re:run(<<>>,MP);
probe(embedded_nul) -> {ok,MP}=re:compile(<<0>>,[]),re:run(<<1,0,2>>,MP);
probe(default_bytes) -> {ok,MP}=re:compile(<<255>>,[]),re:run(<<0,255>>,MP);
probe(compile_missing_bracket) -> re:compile(<<"[">>,[]);
probe(compile_bad_atom) -> re:compile(invalid,[]);
probe(compile_bad_options) -> re:compile(<<"x">>,[not_a_re_option]);
probe(compile_improper_options) -> re:compile(<<"x">>,[unicode|invalid]);
probe(run_bad_subject) -> re:run(invalid,<<"x">>);
probe(run_bad_pattern) -> re:run(<<"x">>,invalid);
probe(compiled_protocol) ->
    {ok,{re_pattern,CaptureCount,Unicode,Crlf,Reference}}=re:compile(?ANSI_REGEXP,[unicode]),
    {CaptureCount,Unicode,Crlf,is_reference(Reference)};
probe(reuse_compiled) ->
    {ok,MP}=re:compile(<<"(x)">>,[]),
    {re:run(<<"x">>,MP),re:run(<<"no">>,MP),re:run(<<"xx">>,MP)}.
