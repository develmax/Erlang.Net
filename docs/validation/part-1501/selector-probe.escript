#!/usr/bin/env escript
-mode(compile).
main(["fixture",Source,Before,After]) ->
    {ok,M,R}=compile:file(Source,[binary,deterministic]),
    {ok,B}=file:read_file(Before), {ok,A}=file:read_file(After),
    Names=[integer_result,mixed_result],
    lists:foreach(fun(N) ->
        OTP=selector(R,N,1), Old=selector(B,N,1), New=selector(A,N,1),
        io:format("~p OTP ~0p, previous ~0p, current ~0p~n",[N,OTP,Old,New]),
        true=OTP=:=New
    end,Names),
    Inputs=[<<I:16/signed>> || I<-lists:seq(-128,128)],
    Mixed=[42, 1.0, alpha, [], {a,1}, <<0>>, <<255>>, <<>>, <<1,2>>],
    check(M,[{integer_result,[I]}||I<-Inputs]++[{mixed_result,[I]}||I<-Inputs++Mixed],R,B,A);
main(["asn1",Root,Before,After]) ->
    M='AlgorithmInformation-2009',
    Path="lib/public_key-1.21.7/ebin/AlgorithmInformation-2009.beam",
    [R,B,A]=[begin {ok,I}=file:read_file(filename:join(D,Path)),I end||D<-[Root,Before,After]],
    io:format("dec_ParamOptions/2 OTP ~0p, previous ~0p, current ~0p~n",
        [selector(R,dec_ParamOptions,2),selector(B,dec_ParamOptions,2),selector(A,dec_ParamOptions,2)]),
    true=selector(R,dec_ParamOptions,2)=:=selector(A,dec_ParamOptions,2),
    Inputs=[<<10,1,I:8/signed>>||I<-lists:seq(-128,127)]++[<<>>,<<10,2,1>>,<<10,2,0,128>>,<<2,1,0>>],
    check(M,[{decode,['ParamOptions',I]}||I<-Inputs],R,B,A).

selector(Binary,Name,Arity) ->
    {beam_file,_,_,_,_,Functions}=beam_disasm:file(Binary),
    [Code]=[C||{function,N,A,_,C}<-Functions,N=:=Name,A=:=Arity],
    [Operand||{select_val,Operand,_,_}<-Code].

check(M,Calls,R,B,A) ->
    Reference=run(M,R,Calls),
    Old=run(M,B,Calls),
    New=run(M,A,Calls),
    io:format("~B observations, previous vs OTP ~B differences, current vs OTP ~B differences, previous vs current ~B differences~n",
        [length(Calls),diff(Reference,Old),diff(Reference,New),diff(Old,New)]),
    lists:foreach(fun({Call,{O,N}}) ->
        if O=/=N -> io:format("~0p previous ~0p current ~0p~n",[Call,O,N]); true -> ok end
    end, lists:zip(Calls,lists:zip(Old,New))),
    Old=New.

run(M,B,Calls) ->
    code:unstick_mod(M),
    code:purge(M),
    {module,M}=code:load_binary(M,"selector-probe.beam",B),
    [try apply(M,F,A) of V->{ok,without_probe_frames(V)} catch C:E->{C,E} end||{F,A}<-Calls].

without_probe_frames({error,{asn1,{Reason,Stack}}}) when is_list(Stack) ->
    {error,{asn1,{Reason,lists:takewhile(fun(Frame)->element(1,Frame)=/=?MODULE end,Stack)}}};
without_probe_frames(Value) -> Value.
diff(L,R) -> length([ok||{A,B}<-lists:zip(L,R),A=/=B]).