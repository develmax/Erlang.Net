-mode(compile).

main([Official,Generated]) ->
    Results = check(Official),
    Results = check(Generated),
    io:format("OTP and generated BEAM agree on returned values, two messages and process dictionary state.~n").

check(Directory) ->
    Module = beam_pid_heap,
    File = filename:join(Directory, "beam_pid_heap.beam"),
    {ok,Bytes} = file:read_file(File),
    {module,Module} = code:load_binary(Module, File, Bytes),
    Key = make_ref(),
    Self = self(),
    Node = node(),
    Wrong = {wrong,Node},
    Started = {started,Self},
    Wrong = Module:notify(Key, Self),
    receive Wrong -> ok after 0 -> error(missing_node) end,
    put(Key, ready),
    Started = Module:notify(Key, Self),
    receive Started -> ok after 0 -> error(missing_pid) end,
    Key = erase(seen),
    ready = erase(Key),
    true = code:delete(Module),
    false = code:purge(Module),
    {Wrong,Started}.
