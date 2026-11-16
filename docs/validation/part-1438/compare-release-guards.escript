#!/usr/bin/env escript
-mode(compile).

main([Reference, Generated]) ->
    Targets = [
        {"lib/inets-9.8/ebin/httpc.beam", cancel_request, 2},
        {"lib/wx-2.7/ebin/wxFont.beam", new, 1},
        {"lib/wx-2.7/ebin/wxLocale.beam", new, 1},
        {"lib/wx-2.7/ebin/wxLocale.beam", new, 2}
    ],
    lists:foreach(fun({Path, Function, Arity}) ->
        io:format("~s ~p/~p~n", [Path, Function, Arity]),
        lists:foreach(fun({Label, Base}) ->
            {beam_file, _, _, _, _, Functions} = beam_disasm:file(filename:join(Base, Path)),
            [Instructions] = [I || {function, N, A, _, I} <- Functions, N =:= Function, A =:= Arity],
            io:format("~s ~0p~n", [Label, Instructions])
        end, [{"OTP", Reference}, {"C#", Generated}])
    end, Targets).
