-mode(compile).
main([Reference, Candidate]) ->
    Expected = helpers(Reference),
    Actual = helpers(Candidate),
    case Expected =:= Actual of
        true -> io:format("~p helper definitions in OTP order~n", [length(Expected)]);
        false -> io:format("Expected ~w~nActual ~w~n", [Expected,Actual]), halt(1)
    end.

helpers(Path) ->
    {beam_file,_,_,_,_,Functions} = beam_disasm:file(Path),
    [{Name,Arity} || {function,Name,Arity,_,_} <- Functions, is_helper(Name)].

is_helper(Name) ->
    Text = atom_to_list(Name),
    string:find(Text, "-lc$^") =/= nomatch andalso not lists:prefix("-binary/", Text).
