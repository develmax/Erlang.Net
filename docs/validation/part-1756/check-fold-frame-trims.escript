-mode(compile).

main([Official,Generated]) ->
    ok = check(Official),
    ok = check(Generated),
    io:format("OTP and generated BEAM agree on smaller entry frames, inter-call trims, bad patterns and dictionary effects.~n").

check(Directory) ->
    ok = load(Directory,beam_pretrim),
    ok = load(Directory,beam_linear_frame_trim),
    Left = #{tag => [a,b,c]},
    Right = {one,two},
    put(saved,old),
    {old,Left,Right} = beam_pretrim:assign({Left,Right},new),
    new = erase(saved),
    try beam_pretrim:assign(invalid,new) of
        _ -> error(expected_function_clause)
    catch
        error:function_clause -> ok
    end,
    {keep,{3,last}} = beam_linear_frame_trim:run(-3,last,keep),
    {keep,[$-,$3,last]} = beam_linear_frame_trim:run_ext(-3,[last],keep),
    ok = unload(beam_linear_frame_trim),
    ok = unload(beam_pretrim).

load(Directory,Module) ->
    File = filename:join(Directory, atom_to_list(Module)++".beam"),
    {ok,Bytes} = file:read_file(File),
    {module,Module} = code:load_binary(Module, File, Bytes),
    ok.

unload(Module) ->
    true = code:delete(Module),
    false = code:purge(Module),
    ok.
