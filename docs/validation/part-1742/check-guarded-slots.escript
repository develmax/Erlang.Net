-mode(compile).

main([Directory]) ->
    Module = beam_guarded_branch_stack_slots,
    Otp = beam(Directory, "otp", Module),
    Before = beam(Directory, "old", Module),
    After = beam(Directory, "new", Module),
    {1,2} = frame(Otp, Module),
    {3,2} = frame(Before, Module),
    {1,2} = frame(After, Module),
    Value = {boxed, [1,2,3], <<5:64>>},
    Inputs = [{Value,left},{Value,right},{Value,fallback},{Value,42}],
    Output = run(Otp, Module, Inputs),
    Output = run(Before, Module, Inputs),
    Output = run(After, Module, Inputs),
    [_,_,Value,{function_clause,Value,42}] = Output,
    io:format("OTP and C# allocate one slot after the guard. Three branches and function_clause arguments match.~n").

beam(Directory, Version, Module) ->
    filename:join([Directory,Version,atom_to_list(Module)++".beam"]).

frame(Path, Module) ->
    {beam_file,Module,_,_,_,Functions} = beam_disasm:file(Path),
    {function,run,2,Entry,Code} = lists:keyfind(run,2,Functions),
    [{label,Entry},{test,is_atom,{f,Failure},[{x,1}]},{allocate,Slots,Live}|_] =
        lists:dropwhile(fun(I) -> I =/= {label,Entry} end, Code),
    true = lists:member({label,Failure}, Code),
    {Slots,Live}.

run(Path, Module, Inputs) ->
    {ok, Binary} = file:read_file(Path),
    {module,Module} = code:load_binary(Module,Path,Binary),
    Results = [try Module:run(Value,Mode)
        catch error:function_clause:Stack ->
            [{Module,run,[Value,Mode],_}|_] = Stack,
            {function_clause,Value,Mode}
        end || {Value,Mode} <- Inputs],
    true = code:delete(Module),
    _ = code:purge(Module),
    Results.

