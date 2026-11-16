-mode(compile).

main([Directory]) ->
    Module = beam_branch_frame_3,
    Inputs = [[value,left],[value,right],[value,fallback]],
    Beam = "beam_branch_frame_3.beam",
    Reference = filename:join([Directory,"otp",Beam]),
    Before = filename:join([Directory,"before",Beam]),
    After = filename:join([Directory,"after",Beam]),
    {y,1} = selector(Reference, Module),
    {x,0} = selector(Before, Module),
    {y,1} = selector(After, Module),
    Results = run(Reference, Module, Inputs),
    Results = run(After, Module, Inputs),
    io:format("Selector uses X0 before and Y1 after, matching OTP. Both explicit branches and the fallback match OTP through garbage collection.~n").

selector(Path, Module) ->
    {beam_file,Module,_,_,_,Functions} = beam_disasm:file(Path),
    {function,run,2,_,Code} = lists:keyfind(run,2,Functions),
    [Selector] = [Src || {select_val,Src,_,_} <- Code],
    Selector.

run(Path, Module, Inputs) ->
    {ok, Binary} = file:read_file(Path),
    {module, Module} = code:load_binary(Module, Path, Binary),
    Results = [apply(Module, run, Args) || Args <- Inputs],
    true = code:delete(Module),
    _ = code:purge(Module),
    Results.
