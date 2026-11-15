-mode(compile).

main([Directory]) ->
    Module = beam_bif_predicate_frame,
    Otp = image(Directory,"otp",Module),
    Before = image(Directory,"old",Module),
    After = image(Directory,"new",Module),
    {true,1} = placement(Otp,Module),
    {false,2} = placement(Before,Module),
    {true,1} = placement(After,Module),
    Result = {created,existing,{case_clause,other}},
    Result = run(Otp,Module),
    Result = run(Before,Module),
    Result = run(After,Module),
    io:format("Predicate precedes allocation and only one branch deallocates, as in OTP. State and case_clause agree.~n").

image(Directory,Version,Module) ->
    filename:join([Directory,Version,atom_to_list(Module)++".beam"]).

placement(File,Module) ->
    {beam_file,Module,_,_,_,Functions} = beam_disasm:file(File),
    {function,run,0,_,Code} = lists:keyfind(run,2,Functions),
    {BeforeFrame,_} = lists:splitwith(fun(I)->element(1,I) =/= allocate end,Code),
    HasPredicate = lists:any(fun(I)->lists:member(element(1,I),[test,select_val]) end,BeforeFrame),
    {HasPredicate,length([ok || {deallocate,_} <- Code])}.

run(File,Module) ->
    {ok,Binary} = file:read_file(File),
    {module,Module} = code:load_binary(Module,File,Binary),
    erase(Module),
    First = Module:run(),
    Second = Module:run(),
    populated = put(Module,other),
    Error = try Module:run() catch error:{case_clause,other} -> {case_clause,other} end,
    erase(Module),
    true = code:delete(Module),
    _ = code:purge(Module),
    {First,Second,Error}.
