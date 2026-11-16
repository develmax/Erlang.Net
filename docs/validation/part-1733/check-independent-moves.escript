-mode(compile).
main([OtpPath, BeforePath, AfterPath]) ->
    {Reference, Expected} = read(OtpPath),
    {Previous, Old} = read(BeforePath),
    {Current, New} = read(AfterPath),
    true = Reference =:= Current,
    true = Reference =/= Previous,
    true = Expected =:= New,
    true = Expected =/= Old,
    Module = beam_independent_call_moves,
    {module, Module} = code:load_binary(Module, AfterPath, Current),
    {"abc.parse", "abc"} = Module:run({"abc"}),
    {".parse", []} = Module:run({[]}),
    io:format("Pinned OTP equals current module byte for byte. Previous image differs.~n"),
    io:format("Previous run/1: ~0p~nCurrent run/1: ~0p~n",[Old,New]),
    io:format("OTP loader and both calls passed.~n").

read(Path) ->
    {ok, Binary} = file:read_file(Path),
    {beam_file,_,_,_,_,Functions} = beam_disasm:file(Path),
    {Binary, lists:keyfind(run, 2, Functions)}.
