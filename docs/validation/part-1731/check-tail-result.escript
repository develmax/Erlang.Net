-mode(compile).

main([Directory]) ->
    Mod = beam_tail_result_apply,
    Base = filename:join(Directory, atom_to_list(Mod)),
    {beam_file, Mod, _, _, _, Functions} = beam_disasm:file(Base ++ ".after.beam"),
    {function, run, 1, _, Instructions} = lists:keyfind(run, 2, Functions),
    [{allocate,1,1}] = [I || I={allocate,_,_} <- Instructions],
    [{apply_last,1,1}] = [I || I={apply_last,_,_} <- Instructions],
    [_,{move,{x,0},{x,1}}|_] = lists:dropwhile(fun(I) -> I =/= {call,1,{Mod,select,1}} end, Instructions),
    io:format("The call result moves directly from X0 to X1, and apply_last deallocates one slot.~n"),
    Inputs = [{Mod,42}, {erlang,missing}, {7,invalid_module}, invalid_argument],
    Values = run(Base ++ ".beam", Mod, Inputs),
    Values = run(Base ++ ".after.beam", Mod, Inputs),
    io:format("OTP and candidate agree on ~B results and effects.~n~p~n", [length(Values), Values]).

run(Path, Mod, Inputs) ->
    {ok, Beam} = file:read_file(Path),
    {module, Mod} = code:load_binary(Mod, Path, Beam),
    Values = [outcome(Mod, Input) || Input <- Inputs],
    true = code:delete(Mod),
    _ = code:purge(Mod),
    Values.

outcome(Mod, Input) ->
    Result = try Mod:run(Input) of
        Value -> {value, Value}
    catch
        Class:Reason -> {Class, Reason}
    end,
    {Result, Mod:effect()}.
