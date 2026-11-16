-mode(compile).

main([Official,Generated]) ->
    ok = check(Official),
    ok = check(Generated),
    io:format("OTP and generated BEAM agree on the deferred field, both replacement values and projection after two calls, dictionary state, and function_clause.~n").

check(Directory) ->
    Module = beam_projection_reuse,
    File = filename:join(Directory, "beam_projection_reuse.beam"),
    {ok,Bytes} = file:read_file(File),
    {module,Module} = code:load_binary(Module, File, Bytes),
    Value = #{tag => [a,b,c]},
    Tuple = {tag,Value},
    Key = make_ref(),
    put(last,old),
    {Tuple,old,Tuple} = Module:remember(Tuple,Key),
    Value = erase(last),
    Tuple = erase(Key),
    put(next,prior),
    Packed = {Tuple,Value},
    {prior,Packed} = Module:pack(Tuple,Key),
    Packed = erase(next),
    Tuple = erase(Key),
    First = make_ref(),
    Target = make_ref(),
    put(First,Target),
    put(Target,old_result),
    {Tuple,old_result} = Module:later(First,Tuple,Key),
    Tuple = erase(First),
    Tuple = erase(Key),
    Value = erase(Target),
    try Module:remember(invalid,Key) of
        _ -> error(expected_function_clause)
    catch
        error:function_clause -> ok
    end,
    true = code:delete(Module),
    false = code:purge(Module),
    ok.
