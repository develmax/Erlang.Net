-mode(compile).

main([Directory]) ->
    Module = beam_bif_result_store,
    Otp = beam(Directory,"otp",Module),
    Before = beam(Directory,"old",Module),
    After = beam(Directory,"new",Module),
    {{y,_},{x,_}} = destinations(Otp,Module),
    {{x,0},{x,0}} = destinations(Before,Module),
    {{y,_},{x,0}} = destinations(After,Module),
    Values = [0,41,1 bsl 128],
    Expected = [{V,{ok,V+1},{V+1,V+1,V}} || V <- Values],
    Expected = run(Otp,Module,Values),
    Expected = run(Before,Module,Values),
    Expected = run(After,Module,Values),
    io:format("BIF Y store matches OTP and leaves the live X argument alone. Three valid inputs and badarith agree.~n").

beam(Directory,Version,Module) ->
    filename:join([Directory,Version,atom_to_list(Module)++".beam"]).

destinations(File,Module) ->
    {beam_file,Module,_,_,_,Functions} = beam_disasm:file(File),
    {destination(run,Functions),destination(live,Functions)}.

destination(Name,Functions) ->
    {function,Name,1,_,Code} = lists:keyfind(Name,2,Functions),
    [Dst] = [D || {gc_bif,'+',{f,0},_,_,D} <- Code],
    Dst.

run(File,Module,Inputs) ->
    {ok,Binary} = file:read_file(File),
    {module,Module} = code:load_binary(Module,File,Binary),
    Results = [{V,Module:run(V),Module:live(V)} || V <- Inputs],
    badarith = Module:run(not_a_number),
    true = code:delete(Module),
    _ = code:purge(Module),
    Results.
