-mode(compile).

main([Official, Candidate]) ->
    Mod = beam_shared_entry_frame,
    {beam_file, Mod, _, _, _, Functions} = beam_disasm:file(Candidate),
    {function, run, 3, _, Code} = lists:keyfind(run, 2, Functions),
    Instructions = [I || I <- Code, not lists:member(opcode(I), [label, line, func_info])],
    [{test, is_eq_exact, _, [{x,0}, {atom,raw}]},
     {test, is_eq_exact, _, [{x,1}, {atom,short}]},
     {move, nil, {x,0}}, return, Allocation | _] = Instructions,
    [Allocation] = [I || I <- Instructions, lists:member(opcode(I), [allocate, allocate_heap])],
    io:format("Two entry tests and the short return precede the sole frame.~n"),
    Inputs = [[raw, short, item], [raw, full, item], [raw, other, item], [other, short, item]],
    Values = run(Official, Mod, Inputs),
    Values = run(Candidate, Mod, Inputs),
    io:format("OTP and candidate values match for all ~B clause inputs: ~p~n", [length(Inputs), Values]).

run(Path, Mod, Inputs) ->
    {ok, Beam} = file:read_file(Path),
    {module, Mod} = code:load_binary(Mod, Path, Beam),
    Values = [apply(Mod, run, Args) || Args <- Inputs],
    true = code:delete(Mod),
    _ = code:purge(Mod),
    Values.

opcode(I) when is_tuple(I) -> element(1, I);
opcode(I) -> I.
