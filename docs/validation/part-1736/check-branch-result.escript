-mode(compile).

main([Directory]) ->
    Values = [value, {nested, lists:seq(1,100), <<123456789:64>>}],
    Inputs = [[Value,Mode] || Value <- Values, Mode <- [left,right,fallback]],
    check(Directory, beam_branch_frame_0, Inputs, 1, 2, 2, false),
    check(Directory, beam_branch_frame_3, Inputs, 2, 2, 1, true).

check(Directory, Module, Inputs, ReferenceFrame, OldFrame, NewFrame, ReferenceSpill) ->
    Beam = atom_to_list(Module) ++ ".beam",
    Reference = filename:join([Directory,"otp",Beam]),
    Before = filename:join([Directory,"before",Beam]),
    After = filename:join([Directory,"after",Beam]),
    {ReferenceFrame,ReferenceSpill} = layout(Reference, Module),
    {OldFrame,true} = layout(Before, Module),
    {NewFrame,false} = layout(After, Module),
    Values = run(Reference, Module, Inputs),
    Values = run(After, Module, Inputs),
    io:format("~p: seed result stays in X0 at dispatch. Frame ~B -> ~B, OTP ~B. All ~B cases match OTP across three branches through explicit garbage collection.~n",
              [Module,OldFrame,NewFrame,ReferenceFrame,length(Values)]).

layout(Path, Module) ->
    {beam_file,Module,_,_,_,Functions} = beam_disasm:file(Path),
    {function,run,2,_,Code} = lists:keyfind(run,2,Functions),
    [Frame] = [Y || {allocate,Y,2} <- Code],
    Starts = lists:dropwhile(fun(I) -> I =/= {call,1,{Module,seed,1}} end,Code),
    case Starts of
        [_,{move,{x,0},{y,_}},{select_val,_,_,_}|_] -> {Frame,true};
        [_,{select_val,_,_,_}|_] -> {Frame,false}
    end.

run(Path, Module, Inputs) ->
    {ok, Binary} = file:read_file(Path),
    {module, Module} = code:load_binary(Module, Path, Binary),
    Values = [apply(Module, run, Args) || Args <- Inputs],
    true = code:delete(Module),
    _ = code:purge(Module),
    Values.
