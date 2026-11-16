-mode(compile).

main([Official,Generated]) ->
    ok = check(Official),
    ok = check(Generated),
    io:format("OTP and generated BEAM agree on field values across collection and calls, call results, early field uses, and dictionary state.~n").

check(Directory) ->
    ok = load(Directory,beam_projection_initialization),
    ok = load(Directory,beam_delayed_fields),
    Left = #{k => [a,b,c]},
    Right = {one,two},
    [{Right,Left},{Left,Right}] = beam_projection_initialization:run([{Left,Right},{Right,Left}]),
    Tuple = {tag,Left},
    {Tuple,Left} = beam_delayed_fields:keep(Tuple),
    {Left,Right} = beam_delayed_fields:early({Left,Right}),
    Left = erase(tag),
    put(saved,existing),
    {existing,Left,Right} = beam_delayed_fields:assign({Left,Right},new),
    new = erase(saved),
    Key = make_ref(),
    put(Key,old),
    {old,Tuple,Left} = beam_delayed_fields:reuse(Tuple,Key),
    Tuple = erase(Key),
    ok = unload(beam_delayed_fields),
    ok = unload(beam_projection_initialization).

load(Directory,Module) ->
    File = filename:join(Directory, atom_to_list(Module)++".beam"),
    {ok,Bytes} = file:read_file(File),
    {module,Module} = code:load_binary(Module, File, Bytes),
    ok.

unload(Module) ->
    true = code:delete(Module),
    false = code:purge(Module),
    ok.
