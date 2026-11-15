-mode(compile).

main([Directory]) ->
    Module = beam_heap_call_result,
    Beam = atom_to_list(Module) ++ ".beam",
    Reference = filename:join([Directory,"otp",Beam]),
    Before = filename:join([Directory,"before",Beam]),
    After = filename:join([Directory,"after",Beam]),
    {1,[1,1]} = layout(Reference, Module),
    {2,[1,0]} = layout(Before, Module),
    {1,[1,1]} = layout(After, Module),
    Values = [value, {nested, lists:seq(1,100), <<123456789:64>>}],
    Inputs = [[Value,Mode] || Value <- Values, Mode <- [left,right,fallback]],
    Results = run(Reference, Module, Inputs),
    Results = run(After, Module, Inputs),
    io:format("Frame 2 -> 1 and both tuple heap tests use one live X register, matching OTP. Six results match OTP through explicit garbage collection.~n").

layout(Path, Module) ->
    {beam_file,Module,_,_,_,Functions} = beam_disasm:file(Path),
    {function,run,2,_,Code} = lists:keyfind(run,2,Functions),
    [Frame] = [Y || {allocate,Y,2} <- Code],
    {Frame,[Live || {test_heap,3,Live} <- Code]}.

run(Path, Module, Inputs) ->
    {ok,Binary} = file:read_file(Path),
    {module,Module} = code:load_binary(Module,Path,Binary),
    Values = [apply(Module,run,Args) || Args <- Inputs],
    true = code:delete(Module),
    _ = code:purge(Module),
    Values.
