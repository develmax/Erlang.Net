-mode(compile).

main([Directory]) ->
    Module = beam_direct_call_projections,
    Otp = beam(Directory,"otp",Module),
    Before = beam(Directory,"old",Module),
    After = beam(Directory,"new",Module),
    {1,true} = argument_setup(Otp,Module),
    {2,false} = argument_setup(Before,Module),
    {1,true} = argument_setup(After,Module),
    Inputs = [{tag,left,right},
              {tag,{left,[1,2]},<<3:64>>},
              {tag,#{left => true},[right]}],
    Expected = [{{Tuple,{ok,{Left,Right}}},{kept,{ok,{Left,Right}}}} ||
                   {tag,Left,Right}=Tuple <- Inputs],
    Expected = run(Otp,Module,Inputs),
    Expected = run(Before,Module,Inputs),
    Expected = run(After,Module,Inputs),
    io:format("Direct X projections and one Y slot match OTP. Both functions agree on three inputs, including heap values.~n").

beam(Directory,Version,Module) ->
    filename:join([Directory,Version,atom_to_list(Module)++".beam"]).

argument_setup(File,Module) ->
    {beam_file,Module,_,_,_,Functions} = beam_disasm:file(File),
    {function,run,1,_,Code} = lists:keyfind(run,2,Functions),
    [{allocate,Slots,1}] = [Alloc || {allocate,_,_}=Alloc <- Code],
    HasPair = fun HasPair([{get_tuple_element,_,1,{x,0}},
                           {get_tuple_element,_,2,{x,1}}|_]) -> true;
                      HasPair([_|Tail]) -> HasPair(Tail);
                      HasPair([]) -> false
              end,
    {Slots,HasPair(Code)}.

run(File,Module,Inputs) ->
    {ok,Binary} = file:read_file(File),
    {module,Module} = code:load_binary(Module,File,Binary),
    Results = [{Module:run(Input),Module:keep(Input)} || Input <- Inputs],
    true = code:delete(Module),
    _ = code:purge(Module),
    Results.
