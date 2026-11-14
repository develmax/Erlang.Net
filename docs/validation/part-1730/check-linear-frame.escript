-mode(compile).

main([Directory]) ->
    Callback = fun(Value) -> true = erlang:garbage_collect(), {observed,Value} end,
    Cases = [{beam_reused_fun_frame, 2, [[one,Callback], [two,Callback]]},
             {beam_reused_list_frame, 3, [[one,two,three], [four,five,six]]},
             {beam_reused_closure_frame, 3, [[one,two,three], [four,five,six]]}],
    [check(Directory, Mod, Limit, Inputs) || {Mod,Limit,Inputs} <- Cases].

check(Directory, Mod, Limit, Inputs) ->
    Base = filename:join(Directory, atom_to_list(Mod)),
    Arity = length(hd(Inputs)),
    Old = frame(Base ++ ".before.beam", Mod, Arity),
    New = frame(Base ++ ".after.beam", Mod, Arity),
    true = New < Old andalso New =< Limit,
    Values = run(Base ++ ".beam", Mod, Inputs),
    Values = run(Base ++ ".after.beam", Mod, Inputs),
    io:format("~p frame ~p -> ~p, ~p results match OTP.~n", [Mod, Old, New, length(Values)]).

frame(Path, Mod, Arity) ->
    {beam_file, Mod, _, _, _, Functions} = beam_disasm:file(Path),
    {function, run, Arity, _, Code} = lists:keyfind(run, 2, Functions),
    [Size] = [element(2,I) || I <- Code, is_tuple(I), lists:member(element(1,I),[allocate,allocate_heap])],
    Size.

run(Path, Mod, Inputs) ->
    {ok, Beam} = file:read_file(Path),
    {module, Mod} = code:load_binary(Mod, Path, Beam),
    Values = [apply(Mod, run, Args) || Args <- Inputs],
    true = code:delete(Mod),
    _ = code:purge(Mod),
    Values.
