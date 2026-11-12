#!/usr/bin/env escript
-mode(compile).
main(["fixture",Source,Before,After]) ->
    {ok,M,R}=compile:file(Source,[binary,deterministic]),
    {ok,B}=file:read_file(Before), {ok,A}=file:read_file(After),
    lists:foreach(fun(Name) ->
        io:format("~p/2 OTP ~0p, previous ~0p, current ~0p~n",
            [Name,operand(R,Name,2),operand(B,Name,2),operand(A,Name,2)])
    end,[route,exposed,referenced]),
    [{tr,{x,0},{t_integer,{0,4}}}]=operand(A,route,2),
    [{x,0}]=operand(A,exposed,2),
    [{x,0}]=operand(A,referenced,2),
    Inputs=[[State,V]||State<-[0,2,4,5,0.0,bad],V<-[0,hello,{value,42},[]]],
    check(M,[{closed,I}||I<-Inputs]++[{exposed,I}||I<-Inputs]++[{escaped,I}||I<-Inputs]++[{referenced_seed,I}||I<-Inputs],R,B,A);
main([Name,Root,Before,After]) ->
    {M,Path,Calls}=parser(Name),
    [R,B,A]=[begin {ok,Binary}=file:read_file(filename:join(D,Path)),Binary end||D<-[Root,Before,After]],
    Counts=[goto_ranges(Binary)||Binary<-[R,B,A]],
    io:format("Typed goto range inventory OTP ~0p, previous ~0p, current ~0p~n",Counts),
    check(M,Calls,R,B,A).

parser("core_parse") ->
    Bodies=["1","'ok'","[]","{1,2}","[1|[]]","fun () -> 1","let <X> = 5 in X",
            "case 1 of <1> when 'true' -> 2 end","call 'erlang':'+'(1,2)","{1,}","let X = in X"],
    Sources=["module 'sample' ['f'/0]\n attributes []\n'f'/0 = fun () -> "++Body++"\nend" || Body<-Bodies],
    Tokens=[Ts||S<-Sources,{ok,Ts,_}<-[core_scan:string(S)]],
    {core_parse,"lib/compiler-10.0.6/ebin/core_parse.beam",[{parse,[Ts]}||Ts<-Tokens]};
parser("erl_parse") ->
    Terms=["0","1","nil","[]","{1,a}","[2,3]","<<1,2>>","#{a => 1}","A","fun () -> ok end"],
    Exprs=Terms++[A++Op++B||A<-Terms,B<-Terms,Op<-[" + "," ++ "," =:= "]]
        ++["case "++T++" of X -> {ok,X} end"||T<-Terms]++["{a,}","[a,b","case X of end"],
    Tokens=[Ts||E<-Exprs,{ok,Ts,_}<-[erl_scan:string(E++".")]],
    {erl_parse,"lib/stdlib-8.1/ebin/erl_parse.beam",[{parse_exprs,[Ts]}||Ts<-Tokens]}.

operand(Binary,Name,Arity) ->
    {beam_file,_,_,_,_,Functions}=beam_disasm:file(Binary),
    [Operand||{function,N,A,_,Code}<-Functions,N=:=Name,A=:=Arity,{select_val,Operand,_,_}<-Code].

goto_ranges(Binary) ->
    {beam_file,_,_,_,_,Functions}=beam_disasm:file(Binary),
    lists:foldl(fun(Range,Acc)->maps:update_with(Range,fun(N)->N+1 end,1,Acc) end,#{},
       [Range||{function,N,_,_,Code}<-Functions,lists:prefix("yeccgoto_",atom_to_list(N)),
              {select_val,{tr,_,{t_integer,Range}},_,_}<-Code]).

check(M,Calls,R,B,A) ->
    Ref=run(M,R,Calls),
    Old=run(M,B,Calls),
    New=run(M,A,Calls),
    io:format("~B observations, previous vs OTP ~B differences, current vs OTP ~B differences, previous vs current ~B differences~n",
        [length(Calls),diff(Ref,Old),diff(Ref,New),diff(Old,New)]),
    Old=New.

run(M,B,Calls) ->
    code:unstick_mod(M),
    code:purge(M),
    {module,M}=code:load_binary(M,"range-probe.beam",B),
    [try invoke(M,F,A) of V->{ok,V} catch C:E->{C,E} end||{F,A}<-Calls].

invoke(M,escaped,A) -> erlang:apply(M:escaped(),A);
invoke(M,F,A) -> apply(M,F,A).
diff(L,R) -> length([ok||{A,B}<-lists:zip(L,R),A=/=B]).
