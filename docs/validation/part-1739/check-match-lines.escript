-mode(compile).

main([Directory]) ->
    Module = beam_match_failure_line,
    Beam = atom_to_list(Module) ++ ".beam",
    Otp = filename:join([Directory,"otp",Beam]),
    Before = filename:join([Directory,"before",Beam]),
    After = filename:join([Directory,"after",Beam]),
    {6,Expected} = line_table(Otp,Module),
    {7,_} = line_table(Before,Module),
    {6,Expected} = line_table(After,Module),
    [{first,6},{second,6}] = entry_lines(After,Module),
    Results = run(Otp,Module),
    Results = run(Before,Module),
    Results = run(After,Module),
    io:format("Line instructions 7 -> 6. The full Line chunk matches OTP. The two function entries retain line 6 and badmatch retains line 4.~n").

line_table(Path,Module) ->
    {ok,{Module,[{"Line",Table}]}} = beam_lib:chunks(Path,["Line"]),
    <<0:32,0:32,Count:32,_/binary>> = Table,
    {beam_file,Module,_,_,_,Fs} = beam_disasm:file(Path),
    Count = length([L || {function,_,_,_,Code} <- Fs, {line,L} <- Code]),
    {Count,Table}.

entry_lines(Path,Module) ->
    {beam_file,Module,_,_,_,Fs} = beam_disasm:file(Path),
    [{Name,Line} || {function,Name,0,_,[_,{line,[{location,_,Line}]}|_]} <- Fs,
                    Name =:= first orelse Name =:= second].

run(Path,Module) ->
    {ok,Beam} = file:read_file(Path),
    {module,Module} = code:load_binary(Module,Path,Beam),
    Value = {example,[1,2,3]},
    Value = Module:run(fun(X) -> {ok,X} end, Value),
    Error = try Module:run(fun(X) -> {missing,X} end, Value) of
        _ -> erlang:error(expected_badmatch)
    catch
        error:{badmatch,{missing,Value}}:Stack ->
            [{Module,run,2,Location}|_] = Stack,
            4 = proplists:get_value(line, Location),
            Location
    end,
    true = code:delete(Module),
    _ = code:purge(Module),
    {Value,Error}.
