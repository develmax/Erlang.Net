-module(loader_contrast_wrapper).
-export([run/3,direct/1,dynamic_apply/3]).
-compile(no_inline).

direct(M) -> erlang:call_on_load_function(M).

dynamic_apply(Module,Function,Arguments) -> erlang:apply(Module,Function,Arguments).

run(Name,Root,Out) ->
    put(root,Root),put(out,Out),put(sequence,0),
    save("stage.txt",<<"entered">>),
    try
        Actual=scenario(Name),Expected=expected(Name),
        term("actual",Actual),term("expected",Expected),
        text("actual.txt",Actual),text("expected.txt",Expected),
        true=(Actual=:=Expected),
        save("stage.txt",<<"completed">>),halt(0)
    catch C:R:S ->
        term("failure",{C,R,S}),text("failure.txt",{C,R,S}),
        save("stage.txt",<<"failed">>),halt(3)
    end.

save(Name,Bytes) -> ok=file:write_file(filename:join(get(out),Name),Bytes).
term(Name,Value) -> save(Name++".etf",term_to_binary(Value)).
text(Name,Value) -> save(Name,iolist_to_binary(io_lib:format("~tp~n",[Value]))).
value(Tag,F) ->
    N=get(sequence),put(sequence,N+1),
    Name=integer_to_list(N)++"-"++Tag,
    save("stage.txt",list_to_binary(Name)),
    V=try F() of X -> {returned,X} catch C:R:S -> {raised,C,R,S} end,
    term(Name,V),text(Name++".txt",V),
    case V of {returned,X1}->X1; {raised,C1,R1,_}->{exception,C1,R1} end.
reason({exception,error,R}) -> R;
reason(Other) -> {unexpected,Other}.
beam(Path) -> {ok,B}=file:read_file(filename:join(get(root),Path)),B.
constant() -> beam("tests/Erlang.Tests/Resources/Beam/beam_fixture_constant.beam").
add() -> beam("tests/Erlang.Tests/Resources/Beam/beam_fixture_add.beam").
fresh() -> beam("tests/Erlang.Tests/Resources/BeamOnLoad/fresh-success/new/beam_on_load_fresh_success.beam").
replacement(Version) -> beam("tests/Erlang.Tests/Resources/BeamOnLoad/replacement-failure/"++Version++"/beam_on_load_replace_failure.beam").
prepare(M,B) -> value("prepare",fun()->erlang:prepare_loading(M,B) end).
finish(L) -> value("finish",fun()->erlang:finish_loading(L) end).
flag(P) -> value("flag",fun()->erlang:has_prepared_code_on_load(P) end).
invoke(M) -> value("invoke",fun()->M:run() end).

scenario("ordinary-consumption") ->
    P=prepare(beam_fixture_constant,constant()),
    Copy=value("etf-copy",fun()->binary_to_term(term_to_binary(P)) end),
    {flag(P),finish([P]),invoke(beam_fixture_constant),flag(Copy),finish([Copy])};
scenario("prepare-type-and-module-errors") ->
    {reason(prepare(42,constant())),reason(prepare(beam_fixture_constant,not_binary)),
     reason(prepare(beam_fixture_constant,<<"FOR1",0:1>>)),
     prepare(beam_fixture_constant,<<>>),prepare(loader_wrong_module,constant())};
scenario("wrapper-gzip-and-real-load-raw") -> load_constant(constant());
scenario("wrapper-gzip-and-real-load-gzip") ->
    G=value("gzip",fun()->zlib:gzip(constant()) end),save("input-gzip.beam",G),load_constant(G);
scenario("batch-list-before-onload-limit") ->
    P=prepare(beam_fixture_constant,constant()),Q=prepare(beam_on_load_fresh_success,fresh()),
    {finish([]),reason(finish([P|wrong])),reason(finish([Q,make_ref()])),
     reason(finish([Q,Q])),reason(finish([P,Q])),flag(P),flag(Q)};
scenario("duplicate-batch-order") ->
    A1=prepare(beam_fixture_constant,constant()),B1=prepare(beam_fixture_add,add()),
    A2=prepare(beam_fixture_constant,constant()),B2=prepare(beam_fixture_add,add()),
    D=finish([A1,B1,A2,B2]),
    VisibleA=erlang:function_exported(beam_fixture_constant,run,0),
    VisibleB=erlang:function_exported(beam_fixture_add,run,0),
    Result=finish([A1,B1]),FlagA=flag(A2),FlagB=flag(B2),
    false=FlagA,false=FlagB,
    {D,VisibleA,VisibleB,Result,invoke(beam_fixture_constant),invoke(beam_fixture_add)};
scenario("third-generation-not-purged") ->
    A=prepare(beam_fixture_constant,constant()),ok=finish([A]),
    B=prepare(beam_fixture_constant,constant()),ok=finish([B]),
    C=prepare(beam_fixture_constant,constant()),D=finish([C]),V=invoke(beam_fixture_constant),
    F1=flag(A),F2=flag(B),F3=flag(C),
    Purged=value("purge",fun()->code:purge(beam_fixture_constant) end),
    {D,V,F1,F2,F3,Purged,finish([C])};
scenario("local-etf-magic-extra-word") ->
    P=prepare(beam_fixture_constant,constant()),E=term_to_binary(P),save("original-reference.etf",E),
    <<131,90,3:16,Rest/binary>>=E,
    E0 = <<131,90,4:16,Rest/binary,0:32>>,E1 = <<131,90,4:16,Rest/binary,1:32>>,
    save("padded-zero-reference.etf",E0),save("padded-one-reference.etf",E1),
    P0=value("padded-zero",fun()->binary_to_term(E0) end),
    P1=value("padded-one",fun()->binary_to_term(E1) end),
    {P0=:=P,flag(P0),P1=:=P,flag(P1),finish([P]),flag(P0),flag(P1),reason(finish([P1]))};
scenario("fresh-onload-private-commit") ->
    true=register(beam_on_load_observer,self()),M=beam_on_load_fresh_success,
    P=prepare(M,fresh()),F0=flag(P),Load=finish([P]),F1=flag(P),
    Exported=erlang:function_exported(M,run,0),CallbackReason=callback(M,success),
    Kept=value("finalize",fun()->erlang:finish_after_on_load(M,true) end),
    {F0,Load,F1,Exported,CallbackReason,Kept,invoke(M),flag(P),
     reason(value("finalize-again",fun()->erlang:finish_after_on_load(M,true) end))};
scenario("replacement-onload-reject") ->
    true=register(beam_on_load_observer,self()),M=beam_on_load_replace_failure,
    Old=value("old-load",fun()->erlang:load_module(M,replacement("old")) end),
    New=value("pending-load",fun()->erlang:load_module(M,replacement("new")) end),
    Before=invoke(M),CallbackReason=callback(M,failure),
    Drop=value("discard",fun()->erlang:finish_after_on_load(M,false) end),
    After=invoke(M),Again=value("discard-again",fun()->erlang:finish_after_on_load(M,false) end),
    {Old,New,Before,CallbackReason,Drop,After,Again};
scenario("direct-v-apply-callback-and-finalize-types") ->
    M=beam_on_load_fresh_success,P=prepare(M,fresh()),{on_load,[M]}=finish([P]),
    Flavor=value("backend",fun()->erlang:system_info(emu_flavor) end),jit=Flavor,
    {reason(value("apply",fun()->?MODULE:dynamic_apply(erlang,call_on_load_function,[M]) end)),
     reason(value("boolean",fun()->erlang:finish_after_on_load(M,wrong) end)),
     reason(value("missing",fun()->erlang:finish_after_on_load(loader_missing,true) end))}.

load_constant(B) ->
    {value("load-module",fun()->erlang:load_module(beam_fixture_constant,B) end),invoke(beam_fixture_constant)}.
callback(M,Kind) ->
    {_Worker,Ref}=spawn_monitor(fun()->exit(loader_direct_caller:direct(M)) end),
    Notice=receive N={on_load_notice,M,_,_,_}->N;
        Early={'DOWN',Ref,process,_,_}->term("early-down",Early),error({missing_notice,Early})
        after 10000->error(notice_timeout) end,
    term("notice",Notice),text("notice.txt",Notice),
    {on_load_notice,M,Pid,_,Kind}=Notice,true=is_pid(Pid),
    Pid!{on_load_continue,self()},
    Down=receive D={'DOWN',Ref,process,Pid,_}->D after 10000->error(callback_timeout) end,
    term("callback-down",Down),text("callback-down.txt",Down),
    {'DOWN',Ref,process,Pid,Reason}=Down,Reason.

expected("ordinary-consumption") -> {false,ok,{ok,42,[alpha,beta]},false,{exception,error,badarg}};
expected("prepare-type-and-module-errors") -> {badarg,badarg,badarg,{error,badfile},{error,badfile}};
expected("wrapper-gzip-and-real-load-raw") -> {{module,beam_fixture_constant},{ok,42,[alpha,beta]}};
expected("wrapper-gzip-and-real-load-gzip") -> expected("wrapper-gzip-and-real-load-raw");
expected("batch-list-before-onload-limit") -> {ok,badarg,badarg,system_limit,system_limit,false,true};
expected("duplicate-batch-order") -> {{duplicated,[beam_fixture_add,beam_fixture_constant]},false,false,ok,{ok,42,[alpha,beta]},42};
expected("third-generation-not-purged") -> {{not_purged,[beam_fixture_constant]},{ok,42,[alpha,beta]},false,false,false,false,ok};
expected("local-etf-magic-extra-word") -> {true,false,true,false,ok,false,false,badarg};
expected("fresh-onload-private-commit") -> {true,{on_load,[beam_on_load_fresh_success]},true,false,ok,true,new_image,true,badarg};
expected("replacement-onload-reject") -> {{module,beam_on_load_replace_failure},{error,on_load},old_image,not_ok,true,old_image,true};
expected("direct-v-apply-callback-and-finalize-types") -> {badarg,badarg,badarg}.
