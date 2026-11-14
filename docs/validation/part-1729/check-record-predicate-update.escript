-mode(compile).

main([Official, Candidate]) ->
    Mod = beam_record_predicate_update,
    {beam_file, Mod, _, _, _, Functions} = beam_disasm:file(Candidate),
    {function, guard, 3, _, Guard} = lists:keyfind(guard, 2, Functions),
    [{y,1},{y,1}] = [Reg || {test,is_tagged_tuple,_,[Reg,_,_]} <- Guard],
    {function, update, 3, _, Update} = lists:keyfind(update, 2, Functions),
    [{y,1}] = [Reg || {test,is_tagged_tuple,_,[Reg,_,_]} <- Update],
    [{y,1}] = [Reg || {update_record,_,_,{tr,Reg,_},_,_} <- Update],
    io:format("Both guard tests and the update read the saved record register.~n"),
    Cases = [{guard, [left,V,right]} || V <- inputs()] ++
            [{predicate, [left,V]} || V <- inputs()] ++
            [{update, [left,V,next]} || V <- inputs()],
    Values = run(Official, Mod, Cases),
    Values = run(Candidate, Mod, Cases),
    io:format("OTP and candidate match for ~p outcomes and effects.~n~p~n", [length(Values),Values]).

inputs() -> [{item,old}, {other,old,kind}, wrong, {item,old,extra}, {item,next}].

run(Path, Mod, Cases) ->
    {ok, Beam} = file:read_file(Path),
    {module, Mod} = code:load_binary(Mod, Path, Beam),
    Values = [outcome(Mod, Function, Args) || {Function,Args} <- Cases],
    true = code:delete(Mod),
    _ = code:purge(Mod),
    Values.

outcome(Mod, Function, Args) ->
    Result = try apply(Mod, Function, Args) of
        Value -> {value, Value}
    catch
        Class:Reason -> {Class, Reason}
    end,
    {Function, Result, Mod:effects()}.
