#!/usr/bin/env escript
-mode(compile).

main([Reference, Previous, Generated]) ->
    Targets = [
        {"lib/inets-9.8/ebin/mod_head.beam", do, 1},
        {"lib/inets-9.8/ebin/mod_head.beam", do_head, 1}
    ],
    lists:foreach(fun({Path, Function, Arity}) ->
        io:format("~s ~p/~p~n", [Path, Function, Arity]),
        lists:foreach(fun({Label, Base}) ->
            {beam_file, _, _, _, _, Functions} = beam_disasm:file(filename:join(Base, Path)),
            [Instructions] = [I || {function, N, A, _, I} <- Functions, N =:= Function, A =:= Arity],
            io:format("~s ~0p~n", [Label, Instructions])
        end, [{"OTP", Reference}, {"Before", Previous}, {"After", Generated}])
    end, Targets).
