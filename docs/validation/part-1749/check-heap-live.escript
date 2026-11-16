-mode(compile).

main([Official,Generated]) ->
    Otp = check(Official),
    GeneratedResults = check(Generated),
    true = Otp =:= GeneratedResults,
    io:format("OTP and generated BEAM produced the same separator, ordinary and empty results: ~0p~n",[Otp]).

check(Directory) ->
    Module = beam_path_split,
    File = filename:join(Directory, "beam_path_split.beam"),
    {ok,Bytes} = file:read_file(File),
    {module,Module} = code:load_binary(Module, File, Bytes),
    Results = [Module:split("ab/c", [], []),
               Module:split("abc", [], []),
               Module:split([], "a", ["bc"])],
    ["c","ba"] = hd(Results),
    true = code:delete(Module),
    false = code:purge(Module),
    Results.
