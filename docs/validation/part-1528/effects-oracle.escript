#!/usr/bin/env escript
-mode(compile).

main([Reference, Candidate]) ->
    Inputs = [<<>>, <<"payload">>, binary:copy(<<0, 1, 2, 3>>, 50001)],
    Expected = observe(Reference, Inputs),
    Expected = observe(Candidate, Inputs),
    Inputs = [Data || {ok, Data} <- Expected],
    io:format("~B transfers produced the original bytes after decompression.~n", [length(Inputs)]).

observe(Path, Inputs) ->
    Module = beam_tuple_case_effects,
    {ok, Image} = file:read_file(Path),
    {module, Module} = code:load_binary(Module, Path, Image),
    try
        [transfer(Module, Data) || Data <- Inputs]
    after
        code:delete(Module),
        code:purge(Module)
    end.

transfer(Module, Data) ->
    {ok, Input} = file:open(Data, [ram, read, binary]),
    {ok, Output} = file:open(<<>>, [ram, read, write, binary]),
    Z = zlib:open(),
    try
        ok = zlib:deflateInit(Z, default),
        Result = Module:transfer(Z, Input, Output),
        ok = zlib:deflateEnd(Z),
        {ok, 0} = file:position(Output, bof),
        {ok, Encoded} = file:read(Output, 1000000),
        {Result, zlib:uncompress(Encoded)}
    after
        zlib:close(Z),
        file:close(Output),
        file:close(Input)
    end.
