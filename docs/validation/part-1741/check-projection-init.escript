-mode(compile).
main([Directory]) ->
    Module = beam_projection_initialization,
    Beam = atom_to_list(Module) ++ ".beam",
    Otp = filename:join([Directory,"otp",Beam]),
    Before = filename:join([Directory,"before",Beam]),
    After = filename:join([Directory,"after",Beam]),
    [{init_yregs,{list,[{y,1},{y,2}]}},{move,{x,0},{y,0}}|_] = setup(Before,Module),
    [{move,{x,0},{y,0}},{get_tuple_element,{y,0},0,{y,1}},
     {get_tuple_element,{y,0},1,{y,2}}|_] = setup(After,Module),
    Inputs = [[], [{left,right}], [{lists:seq(1,100),{nested,<<1234:64>>}},{second,<<5678:64>>}]],
    Output = run(Otp,Module,Inputs),
    Output = run(Before,Module,Inputs),
    Output = run(After,Module,Inputs),
    io:format("Tuple projections initialize Y1 and Y2 before garbage collection. Three inputs match OTP, including heap-allocated fields.~n").

setup(Path,Module) ->
    {beam_file,Module,_,_,_,Fs} = beam_disasm:file(Path),
    {function,'-run/1-fun-0-',1,_,Code} = lists:keyfind('-run/1-fun-0-',2,Fs),
    [{allocate,3,1}|Setup] = lists:dropwhile(fun(I) -> I =/= {allocate,3,1} end,Code),
    Setup.

run(Path, Module, Inputs) ->
    {ok,Binary} = file:read_file(Path),
    {module,Module} = code:load_binary(Module,Path,Binary),
    Results = [apply(Module,run,[Input]) || Input <- Inputs],
    true = code:delete(Module),
    _ = code:purge(Module),
    Results.
