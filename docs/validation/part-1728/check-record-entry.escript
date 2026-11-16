-mode(compile).

main([BodyOtp, BodyCandidate, GuardOtp, GuardCandidate]) ->
    Body = beam_record_entry_slot,
    Guard = beam_record_entry_guard,
    {beam_file, Body, _, _, _, Functions} = beam_disasm:file(BodyCandidate),
    {function, run, 2, _, Code} = lists:keyfind(run, 2, Functions),
    [{test, is_tagged_tuple, _, [{x,1}, 2, {atom,item}]} | _] =
        [I || I <- Code, not lists:member(opcode(I), [label, line, func_info])],
    io:format("The record tag is checked in X1 before frame allocation.~n"),
    Inputs = [[marker,{item,active}], [marker,{item,other}],
              [marker,atom], [marker,{different,active}], [marker,{item,active,extra}]],
    BodyValues = run(BodyOtp, Body, Inputs),
    BodyValues = run(BodyCandidate, Body, Inputs),
    GuardValues = run(GuardOtp, Guard, Inputs),
    GuardValues = run(GuardCandidate, Guard, Inputs),
    io:format("OTP and candidate matched ~B body and ~B guard outcomes.~n", [length(BodyValues), length(GuardValues)]),
    io:format("Body ~p~nGuard ~p~n", [BodyValues, GuardValues]).

run(Path, Mod, Inputs) ->
    {ok, Beam} = file:read_file(Path),
    {module, Mod} = code:load_binary(Mod, Path, Beam),
    Values = [outcome(Mod, Args) || Args <- Inputs],
    true = code:delete(Mod),
    _ = code:purge(Mod),
    Values.

outcome(Mod, Args) ->
    try apply(Mod, run, Args) of
        Value -> {value, Value}
    catch
        Class:Reason -> {Class, Reason}
    end.

opcode(I) when is_tuple(I) -> element(1, I);
opcode(I) -> I.
