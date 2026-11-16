-mode(compile).

main([Directory]) ->
    Module = beam_heap_call_result,
    Beam = atom_to_list(Module) ++ ".beam",
    Reference = filename:join([Directory,"otp",Beam]),
    Before = filename:join([Directory,"before",Beam]),
    After = filename:join([Directory,"after",Beam]),
    [{move,{x,1},{y,0}}|_] = setup(Reference, Module),
    [{init_yregs,{list,[{y,0}]}},{move,{x,1},{y,0}}|_] = setup(Before, Module),
    [{move,{x,1},{y,0}}|_] = setup(After, Module),
    Values = [value, {nested, lists:seq(1,100), <<123456789:64>>}],
    Inputs = [[Value,Mode] || Value <- Values, Mode <- [left,right,fallback]],
    Results = run(Reference, Module, Inputs),
    Results = run(Before, Module, Inputs),
    Results = run(After, Module, Inputs),
    io:format("The argument copy initializes Y0 after allocation, matching OTP. Six values agree through explicit garbage collection.~n").

setup(Path,Module) ->
    {beam_file,Module,_,_,_,Functions} = beam_disasm:file(Path),
    {function,run,2,_,Code} = lists:keyfind(run,2,Functions),
    [{allocate,1,2}|Setup] = lists:dropwhile(fun(I) -> I =/= {allocate,1,2} end, Code),
    Setup.

run(Path, Module, Inputs) ->
    {ok,Binary} = file:read_file(Path),
    {module,Module} = code:load_binary(Module,Path,Binary),
    Results = [apply(Module,run,Args) || Args <- Inputs],
    true = code:delete(Module),
    _ = code:purge(Module),
    Results.
