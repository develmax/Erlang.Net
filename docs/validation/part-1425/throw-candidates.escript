#!/usr/bin/env escript
-mode(compile).

main([Root, Generated]) ->
    Files = filelib:wildcard("lib/*/ebin/*.beam", Generated)
        ++ filelib:wildcard("erts-*/ebin/*.beam", Generated),
    Rows = lists:append([candidates(Root, Generated, Path) || Path <- Files]),
    io:format("Throw lowering candidates: ~p functions across ~p modules.~n",
              [length(Rows), length(lists:usort([Path || {Path, _} <- Rows]))]),
    lists:foreach(fun({Path, MFA}) -> io:format("~s ~p~n",[Path,MFA]) end, lists:sort(Rows)).

candidates(Root, Generated, Path) ->
    Original = functions(filename:join(Root, Path)),
    Current = functions(filename:join(Generated, Path)),
    [{Path, MFA} || {MFA, Code} <- maps:to_list(Original),
                   lists:any(fun throws/1, maps:get(MFA, Current, [])),
                   has_lowered_throw(Code, [])].

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
