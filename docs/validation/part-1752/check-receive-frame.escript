-mode(compile).

main([Official,Generated]) ->
    ok = check(Official),
    ok = check(Generated),
    io:format("OTP and generated BEAM agree on selective scan, timed resumption, timeout, dictionary updates, and the skipped message.~n").

check(Directory) ->
    Module = beam_receive_slots,
    File = filename:join(Directory, "beam_receive_slots.beam"),
    {ok,Bytes} = file:read_file(File),
    {module,Module} = code:load_binary(Module, File, Bytes),
    erase(ref),
    erase(result),
    put(x, previous_x),
    put(y, previous_y),
    Tag = make_ref(),
    self() ! skipped,
    self() ! {Tag,found},
    {ok,found} = Module:notify(Tag,0),
    found = get(result),
    {timeout,Ref} = Module:notify(Tag,0),
    true = is_reference(Ref),
    Ref = erase(ref),
    found = erase(result),
    erlang:send_after(10,self(),{Tag,x}),
    {chosen,previous_x} = Module:pick(Tag,1000),
    self() ! {Tag,second,y},
    {second,previous_y} = Module:pick(Tag,0),
    none = Module:pick(Tag,0),
    x = erase(x),
    y = erase(y),
    receive skipped -> ok after 0 -> error(lost_skipped_message) end,
    true = code:delete(Module),
    false = code:purge(Module),
    ok.
