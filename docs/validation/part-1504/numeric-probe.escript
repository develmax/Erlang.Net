#!/usr/bin/env escript
-mode(compile).
main(["fixture",Source,Before,After]) ->
    {ok,M,R}=compile:file(Source,[binary,deterministic]),
    {ok,B}=file:read_file(Before), {ok,A}=file:read_file(After),
    lists:foreach(fun(N) ->
        io:format("~p/2 OTP ~0p, previous ~0p, current ~0p~n",[N,addition_types(R,N,2),addition_types(B,N,2),addition_types(A,N,2)]),
        true=addition_types(R,N,2)=:=addition_types(A,N,2)
    end,[numeric,mixed]),
    Values=[0,1,-2,3.25,-0.0,1 bsl 80,bad,[],<<>>],
    Calls=[{F,[{input,X,Y},tag]}||F<-[numeric,mixed],X<-Values,Y<-Values],
    check(M,Calls,R,B,A);
main(["megaco",Root,Before,After]) ->
    M=megaco_ber_media_gateway_control_v1,
    P="lib/megaco-4.9.2/ebin/megaco_ber_media_gateway_control_v1.beam",
    [R,B,A]=[begin {ok,I}=file:read_file(filename:join(D,P)),I end||D<-[Root,Before,After]],
    lists:foreach(fun({F,N}) ->
        io:format("~p/~B OTP ~0p, previous ~0p, current ~0p~n",[F,N,addition_types(R,F,N),addition_types(B,F,N),addition_types(A,F,N)]),
        true=addition_types(R,F,N)=:=addition_types(A,F,N)
    end,[{enc_AuditRequest,2},{encode_tags,3}]),
    Ids=[<<0:64>>,<<1:64>>,<<255:64>>,lists:duplicate(8,0),<<1>>,invalid],
    Bits=[asn1_NOVALUE,[],[muxToken],[mediaToken,eventsToken],[statsToken],invalid],
    Calls=[{encode,['AuditRequest',{'AuditRequest',{'TerminationID',[],I},{'AuditDescriptor',T}}]}||I<-Ids,T<-Bits],
    check(M,Calls,R,B,A);
main(["pkix",Root,Before,After]) ->
    M='PKIX1Implicit-2009',
    P="lib/public_key-1.21.7/ebin/PKIX1Implicit-2009.beam",
    [R,B,A]=[begin {ok,I}=file:read_file(filename:join(D,P)),I end||D<-[Root,Before,After]],
    F=enc_PolicyInformation,
    io:format("~p/2 OTP ~0p, previous ~0p, current ~0p~n",[F,addition_types(R,F,2),addition_types(B,F,2),addition_types(A,F,2)]),
    true=addition_types(R,F,2)=:=addition_types(A,F,2),
    Ids=[{1,2,3,4},{1,3,6,1,5,5,7}, {2,4,0,128}, {1,2}, invalid],
    Qualifiers=[asn1_NOVALUE,[],[{'PolicyQualifierInfo',{1,3,6,1,5,5,7,2,1},<<22,1,97>>}],invalid],
    Calls=[{encode,['PolicyInformation',{'PolicyInformation',I,Q}]}||I<-Ids,Q<-Qualifiers],
    check(M,Calls,R,B,A).

addition_types(Binary,Name,Arity) ->
    {beam_file,_,_,_,_,Functions}=beam_disasm:file(Binary),
    [[type(O)||O<-Args] || {function,N,A,_,Code}<-Functions,N=:=Name,A=:=Arity,
          {gc_bif,'+',_,_,Args,_}<-Code].

type({tr,_,T})->T;
type({integer,_})->literal;
type(_)->untyped.

check(M,Calls,R,B,A) ->
    Reference=run(M,R,Calls), Old=run(M,B,Calls), New=run(M,A,Calls),
    io:format("~B observations, previous vs OTP ~B differences, current vs OTP ~B differences, previous vs current ~B differences~n",
        [length(Calls),diff(Reference,Old),diff(Reference,New),diff(Old,New)]),
    Old=New.

run(M,B,Calls) ->
    code:unstick_mod(M),
    code:purge(M),
    {module,M}=code:load_binary(M,"numeric-probe.beam",B),
    [try apply(M,F,A) of V->{ok,without_probe_frames(V)} catch C:E->{C,E} end||{F,A}<-Calls].

without_probe_frames({error,{asn1,{Reason,Stack}}}) when is_list(Stack) ->
    {error,{asn1,{Reason,lists:takewhile(fun(Frame)->element(1,Frame)=/=?MODULE end,Stack)}}};
without_probe_frames(Value) -> Value.

diff(L,R) -> length([ok||{A,B}<-lists:zip(L,R),A=/=B]).
