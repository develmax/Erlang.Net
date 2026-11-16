-mode(compile).

main([Official,Generated]) ->
    ok = check(Official),
    ok = check(Generated),
    ok = check_guard(Official),
    ok = check_guard(Generated),
    io:format("OTP and generated BEAM agree on guarded branches, argument reuse across calls, dictionary effects, case_clause and guarded frame behavior.~n").

check(Directory) ->
    Module = beam_entry_reload,
    File = filename:join(Directory, "beam_entry_reload.beam"),
    {ok,Bytes} = file:read_file(File),
    {module,Module} = code:load_binary(Module, File, Bytes),
    Value = #{tag => [a,b,c]},
    put(one,old_one),
    put(two,old_two),
    {one,Value,old_one} = Module:guarded(one,Value),
    {two,Value,old_two} = Module:guarded(two,Value),
    Value = erase(one),
    Value = erase(two),
    Key = make_ref(),
    Value = Module:two(Key,Value),
    Value = erase(Key),
    try Module:guarded(unknown,Value) of
        _ -> error(expected_case_clause)
    catch
        error:{case_clause,unknown} -> ok
    end,
    true = code:delete(Module),
    false = code:purge(Module),
    ok.

check_guard(Directory) ->
    Module = beam_guarded_frame_prefix,
    File = filename:join(Directory, "beam_guarded_frame_prefix.beam"),
    {ok,Bytes} = file:read_file(File),
    {module,Module} = code:load_binary(Module, File, Bytes),
    42 = Module:run(42,cached),
    {ok,43} = Module:run(42,refresh),
    try Module:run(invalid,refresh) of
        _ -> error(expected_function_clause)
    catch
        error:function_clause -> ok
    end,
    true = code:delete(Module),
    false = code:purge(Module),
    ok.