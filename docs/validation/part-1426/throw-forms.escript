#!/usr/bin/env escript
-mode(compile).

main([Root, Generated]) ->
    Files = filelib:wildcard("lib/*/ebin/*.beam", Generated)
        ++ filelib:wildcard("erts-*/ebin/*.beam", Generated),
    Rows = lists:append([candidates(Root, Generated, Path) || Path <- Files]),
    lists:foreach(fun(Op) ->
        Sub=[{P,F} || {P,F,Ops}<-Rows, lists:member(Op,Ops)],
        io:format("~p ~p functions in ~p modules.~n",[Op,length(Sub),length(lists:usort([P || {P,_}<-Sub]))])
    end,[call_ext,call_ext_only,call_ext_last]),
    io:format("Groups overlap when a function contains several call forms.~n").

candidates(Root, Generated, Path) ->
    Original = functions(filename:join(Root, Path)),
    Current = functions(filename:join(Generated, Path)),
    [{Path, MFA, Ops} || {MFA, Code} <- maps:to_list(Original),
        has_lowered_throw(Code, []),
        Ops <- [lists:usort([element(1,I)||I<-maps:get(MFA,Current,[]),throws(I)])], Ops=/=[]].

functions(Path) ->
    case beam_disasm:file(Path) of
        {beam_file, _, _, _, _, Fs} ->
            maps:from_list([{{F,A}, Code} || {function,F,A,_,Code} <- Fs]);
        _ -> #{}
    end.

throws({call_ext,1,{extfunc,erlang,throw,1}}) -> true;
throws({call_ext_only,1,{extfunc,erlang,throw,1}}) -> true;
throws({call_ext_last,1,{extfunc,erlang,throw,1},_}) -> true;
throws(_) -> false.

has_lowered_throw([Instruction|Rest], Previous) ->
    case is_raise(Instruction)
        andalso lists:member({move,{atom,throw},{x,0}}, Previous)
        andalso lists:member({move,nil,{x,2}}, Previous) of
        true -> true;
        false -> has_lowered_throw(Rest, lists:sublist([Instruction|Previous], 6))
    end;
has_lowered_throw([], _) -> false.

is_raise({call_ext,3,{extfunc,erlang,raise,3}}) -> true;
is_raise({call_ext_only,3,{extfunc,erlang,raise,3}}) -> true;
is_raise({call_ext_last,3,{extfunc,erlang,raise,3},_}) -> true;
is_raise(_) -> false.
