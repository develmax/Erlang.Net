-mode(compile).

main([Directory]) ->
    Module = beam_entry_bif_placement,
    Otp = image(Directory,"otp",Module),
    Before = image(Directory,"old",Module),
    After = image(Directory,"new",Module),
    [{0,1},{1,2}] = prefix(Otp,Module),
    [] = prefix(Before,Module),
    [{0,1},{1,2}] = prefix(After,Module),
    Inputs = [0,-1,1 bsl 128],
    Expected = [{{ok,N+1},{N,N+1}} || N <- Inputs],
    Expected = run(Otp,Module,Inputs),
    Expected = run(Before,Module,Inputs),
    Expected = run(After,Module,Inputs),
    io:format("OTP and C# agree on the pre-frame BIF and live X registers, three results, and badarith frames and lines.~n").

image(Directory,Version,Module) ->
    filename:join([Directory,Version,atom_to_list(Module)++".beam"]).

prefix(File,Module) ->
    {beam_file,Module,_,_,_,Functions} = beam_disasm:file(File),
    [{Reg,Live} || Name <- [run,keep],
                   {function,_,1,_,Code} <- [lists:keyfind(Name,2,Functions)],
                   {Reg,Live} <- initial_bif(Code)].

initial_bif([{gc_bif,'+',{f,0},1,[{x,0},{integer,1}],{x,Reg}},
             {allocate,_,Live}|_]) -> [{Reg,Live}];
initial_bif([{allocate,_,_}|_]) -> [];
initial_bif([_|Tail]) -> initial_bif(Tail);
initial_bif([]) -> [].

run(File,Module,Inputs) ->
    {ok,Binary} = file:read_file(File),
    {module,Module} = code:load_binary(Module,File,Binary),
    Results = [{Module:run(N),Module:keep(N)} || N <- Inputs],
    check_error(Module,run,5),
    check_error(Module,keep,10),
    true = code:delete(Module),
    _ = code:purge(Module),
    Results.

check_error(Module,Name,Line) ->
    try Module:Name(invalid) of
        Other -> error({unexpected_result,Other})
    catch
        error:badarith:Stack ->
            [{erlang,'+',[invalid,1],_},{Module,Name,1,Location}|_] = Stack,
            Line = proplists:get_value(line,Location),
            ok
    end.

