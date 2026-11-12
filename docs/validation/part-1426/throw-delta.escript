#!/usr/bin/env escript
-mode(compile).

main([Root, Old, Current, Paths]) ->
    {ok,Bin}=file:read_file(Paths),
    Files=[unicode:characters_to_list(P)||P<-binary:split(Bin,<<10>>,[global]),P=/=<<>>],
    Rows=lists:append([diff(Root,Old,Current,P)||P<-Files]),
    lists:foreach(fun(Row)->io:format("~0p~n",[Row]) end, Rows),
    io:format("Changed throw/raise counts: ~p; candidate raises exceed OTP in ~p.~n",
              [length(Rows), length([ok || {_,_,{_,R},{_,_},{_,N}}<-Rows,N>R])]).

diff(Root, Old, Current, Path) ->
    Ref = read(filename:join(Root,Path)),
    Before = read(filename:join(Old,Path)),
    After = read(filename:join(Current,Path)),
    [{Path,K,maps:get(K,Ref,{0,0}),B,A} || {K,A}<-maps:to_list(After),
        B<-[maps:get(K,Before,{0,0})], A=/=B].

read(P) ->
    case beam_disasm:file(P) of
      {beam_file,_,_,_,_,Fs}->maps:from_list([{{F,A},{count(throw,1,Code),count(raise,3,Code)}}
          || {function,F,A,_,Code}<-Fs]);
      _->#{}
    end.

count(Name,Arity,Code) ->
    length([I||I<-Code,
           is_tuple(I),tuple_size(I)>=3,
           element(3,I)=:={extfunc,erlang,Name,Arity}]).
