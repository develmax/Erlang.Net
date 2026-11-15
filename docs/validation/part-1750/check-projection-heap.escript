-mode(compile).

main([Official,Generated]) ->
    Results = check(Official),
    Results = check(Generated),
    io:format("OTP and generated BEAM agree for the missing key, wrong tag and 1000 tuple rebuilds: ~0p~n",[Results]).

check(Directory) ->
    Module = beam_tuple_heap,
    File = filename:join(Directory, "beam_tuple_heap.beam"),
    {ok,Bytes} = file:read_file(File),
    {module,Module} = code:load_binary(Module, File, Bytes),
    type = Module:category(ref),
    put(ref,{other,0,m,n}),
    type = Module:category(ref),
    {garbage_collection,Before} = process_info(self(), garbage_collection),
    lists:foreach(fun(I) ->
        Value = lists:duplicate(50, I),
        put(ref,{definition,I,m,Value}),
        {class,{reference,I,m,Value}} = Module:category(ref)
    end, lists:seq(1,1000)),
    {garbage_collection,After} = process_info(self(), garbage_collection),
    true = proplists:get_value(minor_gcs, Before) =/= proplists:get_value(minor_gcs, After),
    erase(ref),
    true = code:delete(Module),
    false = code:purge(Module),
    {type,type,1000}.
