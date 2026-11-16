#!/usr/bin/env escript
-mode(compile).
main([Root,Before,After,Name]) ->
    {Path,M,Functions,Calls}=scenario(Name),
    Ref=filename:join(Root,Path), Prior=filename:join(Before,Path), Current=filename:join(After,Path),
    lists:foreach(fun({Tag,File})->show(Tag,File,Functions) end,[{otp,Ref},{previous,Prior},{current,Current}]),
    R=execute(Ref,M,Calls), B=execute(Prior,M,Calls), A=execute(Current,M,Calls),
    io:format("~B observations, previous vs OTP ~B differences, current vs OTP ~B differences, previous vs current ~B differences~n",
        [length(R),diff(R,B),diff(R,A),diff(B,A)]),
    true=B=:=A.

scenario("erl_erts_errors") ->
    Values=[undefined,[],0,1,-1,3.25,<<"a">>,{value,1},#{},[2]],
    Pairs=[{abs,1},{adler32,1},{atom_to_binary,1},{atom_to_list,1},{bit_size,1},
           {binary_to_atom,1},{binary_to_float,1},{binary_to_integer,1},
           {binary_to_list,1},{bitstring_to_list,1},{byte_size,1},{ceil,1},
           {atom_to_binary,2},{binary_to_integer,2},{binary_to_atom,2},{binary_part,2},{append_element,2}],
    Cases=[{erlang,F,A} || {F,N}<-Pairs,A<-arguments(N,Values)]
          ++ [{atomics,F,[R,I,V]} || F<-[add,add_get,put,exchange],R<-[atomics:new(2,[]),invalid],I<-[1,3,undefined],V<-Values]
          ++ [{counters,F,[R,I,V]} || F<-[add,put,sub],R<-[counters:new(2,[]),invalid],I<-[1,3,undefined],V<-Values],
    Calls=[{format_error,[Reason,[{Module,F,A,[{error_info,#{cause=>Cause}}]}]]}
        || {Module,F,A}<-Cases, Reason<-[badarg,system_limit], Cause<-[none,badopt]],
    {"lib/kernel-11.0.4/ebin/erl_erts_errors.beam",erl_erts_errors,[{format_erlang_error,3}],Calls};
scenario("xmerl_sax_parser_utf8") ->
    Texts=["","plain","&amp;","&apos;","&lt;","&gt;","&quot;","&#65;","&#x41;","&#0;","&missing;","&","&#x;"],
    Fragments=[P++T++S || P<-["","<child>","<!-- comment -->"], T<-Texts,S<-["","</child>","<empty/>"]],
    XML=["<root>"++T++"</root>" || T<-Fragments]++["<root", "<root a='&amp;'/>","<!DOCTYPE r [<!ENTITY a 'local'>]><r>&a;</r>"],
    Calls=[{stream,[unicode:characters_to_binary(Text),[]]} || Text<-XML],
    {"lib/xmerl-2.2/ebin/xmerl_sax_parser_utf8.beam",xmerl_sax_parser_utf8,[{parse_entity_content,4}],Calls}.

arguments(1,Values) -> [[X] || X<-Values];
arguments(2,Values) -> [[X,Y] || X<-Values,Y<-Values].

execute(File,M,Calls) ->
    code:unstick_mod(M),
    code:purge(M),
    {ok,Binary}=file:read_file(File),
    {module,M}=code:load_binary(M,File,Binary),
    [try apply(case M of xmerl_sax_parser_utf8 -> xmerl_sax_parser; _ -> M end,F,A) of
        Value->{ok,Value} catch Class:Error->{Class,Error}
     end || {F,A}<-Calls].

show(Tag,File,Functions) ->
    {beam_file,_,_,_,_,Fs}=beam_disasm:file(File),
    lists:foreach(fun({F,A}) ->
        [Code]=[C || {function,F0,A0,_,C}<-Fs,F0=:=F,A0=:=A],
        Keys=returns(Code),
        Scopes=[element(1,I) || I<-Code,is_tuple(I),
                              lists:member(element(1,I),['catch','try',catch_end,try_end,try_case])],
        io:format("~p ~p/~B: ~B excess return blocks, ~B exception scope instructions~n",
          [Tag,F,A,length(Keys)-length(lists:usort(Keys)),length(Scopes)])
    end,Functions).

returns([{label,_},{move,S,{x,0}},{deallocate,N},return|T]) -> [{S,N}|returns(T)];
returns([{label,_},{deallocate,N},return|T]) -> [{{x,0},N}|returns(T)];
returns([_|T]) -> returns(T);
returns([]) -> [].
diff(L,R) -> length([ok || {A,B}<-lists:zip(L,R),A=/=B]).