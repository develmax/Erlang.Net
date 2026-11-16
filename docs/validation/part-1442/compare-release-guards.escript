#!/usr/bin/env escript
-mode(compile).

main([Reference, Previous, Generated]) ->
    Targets = [
        {"lib/asn1-5.5.2/ebin/asn1ct.beam", scan_pass, 1},
        {"lib/asn1-5.5.2/ebin/asn1ct.beam", set_scan_parse_pass_1, 2},
        {"lib/asn1-5.5.2/ebin/asn1ct.beam", maybe_saved_sindex, 2}
    ],
    lists:foreach(fun({Path, Function, Arity}) ->
        io:format("~s ~p/~p~n", [Path, Function, Arity]),
        lists:foreach(fun({Label, Base}) ->
            {beam_file, _, _, _, _, Functions} = beam_disasm:file(filename:join(Base, Path)),
            [Instructions] = [I || {function, N, A, _, I} <- Functions, N =:= Function, A =:= Arity],
            io:format("~s ~0p~n", [Label, Instructions])
        end, [{"OTP", Reference}, {"Before", Previous}, {"After", Generated}])
    end, Targets).
