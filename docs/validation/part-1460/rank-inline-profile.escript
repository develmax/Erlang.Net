#!/usr/bin/env escript
-mode(compile).

main([Reference, Generated]) ->
    Root = filename:split(filename:absname(Generated)),
    Files = filelib:fold_files(Generated, "[.]beam$", true, fun(Path, Acc) -> [Path|Acc] end, []),
    Candidates = lists:sort(lists:filtermap(fun(Path) ->
        Relative = filename:join(lists:nthtail(length(Root), filename:split(filename:absname(Path)))),
        case beam_lib:chunks(filename:join(Reference, Relative), [compile_info]) of
            {ok, {_, [{compile_info, Info}]}} ->
                case lists:member(inline, proplists:get_value(options, Info, [])) of
                    true -> {true, Relative};
                    false -> false
                end;
            _ -> false
        end
    end, Files)),
    io:format("~B differing images record the OTP inline option.~n", [length(Candidates)]),
    lists:foreach(fun(Path) -> io:format("~s~n", [Path]) end, Candidates).
