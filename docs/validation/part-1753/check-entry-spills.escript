-mode(compile).

main([Official,Generated]) ->
    ok = check(Official),
    ok = check(Generated),
    ok = check_projection(Official),
    ok = check_projection(Generated),
    io:format("OTP and generated BEAM agree on both branches, call preservation, case_clause and field values across garbage collection.~n").

check(Directory) ->
    Module = beam_entry_spills,
    File = filename:join(Directory, "beam_entry_spills.beam"),
    {ok,Bytes} = file:read_file(File),
    {module,Module} = code:load_binary(Module, File, Bytes),
    Argument = #{tag => [a,b,c]},
    erase(captured),
    undefined = Module:emit(Argument,yes),
    Argument = Module:emit(captured,no),
    Argument = erase(captured),
    put(key, before),
    {before,Argument} = Module:across(Argument, after_call),
    after_call = erase(key),
    try Module:emit(Argument, unknown) of
        _ -> error(expected_case_clause)
    catch
        error:{case_clause,unknown} -> ok
    end,
    true = code:delete(Module),
    false = code:purge(Module),
    ok.

check_projection(Directory) ->
    Module = beam_projection_initialization,
    File = filename:join(Directory, "beam_projection_initialization.beam"),
    {ok,Bytes} = file:read_file(File),
    {module,Module} = code:load_binary(Module, File, Bytes),
    Left = #{k => [a,b,c]},
    Right = {one,two},
    [{Right,Left},{Left,Right}] = Module:run([{Left,Right},{Right,Left}]),
    true = code:delete(Module),
    false = code:purge(Module),
    ok.