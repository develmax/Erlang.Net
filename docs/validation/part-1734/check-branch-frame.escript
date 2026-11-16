-mode(compile).

main([Directory]) ->
    Module = beam_branch_frame_3,
    Inputs = [[value,left],[value,right],[value,fallback]],
    Reference = filename:join([Directory,"otp","beam_branch_frame_3.beam"]),
    Before = filename:join([Directory,"before","beam_branch_frame_3.beam"]),
    After = filename:join([Directory,"after","beam_branch_frame_3.beam"]),
    {2,2} = frame(Reference, Module),
    {4,2} = frame(Before, Module),
    {2,2} = frame(After, Module),
    Values = run(Reference, Module, Inputs),
    Values = run(After, Module, Inputs),
    io:format("run/2 frame size 4 -> 2, matching OTP. All three branches match OTP values through explicit garbage collection.~n").

frame(Path, Module) ->
    {beam_file,Module,_,_,_,Functions} = beam_disasm:file(Path),
    {function,run,2,_,Code} = lists:keyfind(run,2,Functions),
    [{Size,Live}] = [{element(2,I),element(tuple_size(I),I)} || I <- Code,
                    is_tuple(I), lists:member(element(1,I),[allocate,allocate_heap])],
    {Size,Live}.

run(Path, Module, Inputs) ->
    {ok, Binary} = file:read_file(Path),
    {module, Module} = code:load_binary(Module, Path, Binary),
    Results = [apply(Module, run, Args) || Args <- Inputs],
    true = code:delete(Module),
    _ = code:purge(Module),
    Results.
