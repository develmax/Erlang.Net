-mode(compile).

main([Directory]) ->
    Module = beam_heap_call_result,
    Beam = atom_to_list(Module) ++ ".beam",
    Otp = filename:join([Directory,"otp",Beam]),
    Before = filename:join([Directory,"before",Beam]),
    After = filename:join([Directory,"after",Beam]),
    {8,Expected} = line_table(Otp,Module),
    {10,_} = line_table(Before,Module),
    {8,Expected} = line_table(After,Module),
    Locations = call_locations(Otp,Module),
    Locations = call_locations(Before,Module),
    Locations = call_locations(After,Module),
    io:format("Line instructions 10 -> 8. The whole Line chunk and all five run/2 call locations match OTP.~n").

line_table(Path,Module) ->
    {ok,{Module,[{"Line",Table}]}} = beam_lib:chunks(Path,["Line"]),
    <<0:32,0:32,Count:32,_/binary>> = Table,
    {beam_file,Module,_,_,_,Fs} = beam_disasm:file(Path),
    Count = length([L || {function,_,_,_,Code} <- Fs, {line,L} <- Code]),
    {Count,Table}.

call_locations(Path,Module) ->
    {beam_file,Module,_,_,_,Fs} = beam_disasm:file(Path),
    {function,run,2,_,Code} = lists:keyfind(run,2,Fs),
    calls(Code,none,[]).

calls([{line,Loc}|T], _, A) -> calls(T,Loc,A);
calls([{call,_,_}=Call|T], Loc, A) -> calls(T,Loc,[{Call,Loc}|A]);
calls([_|T], Loc, A) -> calls(T,Loc,A);
calls([], _, A) -> lists:reverse(A).
