-mode(compile).

main([Directory]) ->
    Mod = beam_call_result_callback,
    Base = filename:join(Directory,atom_to_list(Mod)),
    {beam_file,Mod,_,_,_,Functions} = beam_disasm:file(Base ++ ".after.beam"),
    {function,run,3,_,Instructions} = lists:keyfind(run,2,Functions),
    [{call_fun,1},{move,{x,0},{x,1}}|_] = lists:dropwhile(fun(I) -> I =/= {call_fun,1} end, Instructions),
    F = fun(V) -> put(callback_effect,V), {selected,V} end,
    Inputs = [[F,left,right], [F,17,{tuple,a}], [not_fun,left,right]],
    Values = run(Base ++ ".beam", Mod, Inputs),
    Values = run(Base ++ ".after.beam", Mod, Inputs),
    io:format("Direct X0-to-X1 forwarding before an ordinary call. ~B outcomes and effects match OTP.~n", [length(Inputs)]).

run(Path, Mod, Inputs) ->
    {ok,Beam} = file:read_file(Path),
    {module,Mod} = code:load_binary(Mod,Path,Beam),
    Results = [result(Mod,Args) || Args <- Inputs],
    true = code:delete(Mod),
    _ = code:purge(Mod),
    Results.

result(Mod, Args) ->
    Outcome = try apply(Mod,run,Args) of
        V -> {value,V}
    catch
        Class:Reason -> {Class,Reason}
    end,
    {Outcome,erase(callback_effect)}.
