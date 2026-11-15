-mode(compile).

main([Directory]) ->
    Module = beam_guarded_frame_prefix,
    Otp = image(Directory,"otp",Module),
    Before = image(Directory,"old",Module),
    After = image(Directory,"new",Module),
    {2,1} = placement(Otp,Module),
    {1,2} = placement(Before,Module),
    {2,1} = placement(After,Module),
    Values = [0,-42,1 bsl 128],
    Expected = [{{ok,V+1},V} || V <- Values],
    Expected = run(Otp,Module,Values),
    Expected = run(Before,Module,Values),
    Expected = run(After,Module,Values),
    io:format("Both predicates precede allocation. The fast return, framed results and function_clause arguments match OTP.~n").

image(Directory,Version,Module) ->
    filename:join([Directory,Version,atom_to_list(Module)++".beam"]).

placement(File,Module) ->
    {beam_file,Module,_,_,_,Functions} = beam_disasm:file(File),
    {function,run,2,_,Code} = lists:keyfind(run,2,Functions),
    {Before,_} = lists:splitwith(fun(I)->op(I) =/= allocate end,Code),
    {length([ok || {test,_,_,_} <- Before]),length([ok || {deallocate,_} <- Code])}.

run(File,Module,Values) ->
    {ok,Binary} = file:read_file(File),
    {module,Module} = code:load_binary(Module,File,Binary),
    Results = [{Module:run(V,refresh),Module:run(V,skip)} || V <- Values],
    try Module:run(invalid,refresh) of
        Unexpected -> error({unexpected,Unexpected})
    catch error:function_clause:Stack ->
        [{Module,run,[invalid,refresh],_}|_] = Stack
    end,
    true = code:delete(Module),
    _ = code:purge(Module),
    Results.

op(T) when is_tuple(T) -> element(1,T);
op(A) -> A.
