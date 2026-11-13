-module(beam_tuple_case_effects).
-export([transfer/3]).

transfer(Transform, Input, Output) ->
    case file:read(Input, 100000) of
        {ok, Data} ->
            Encoded = zlib:deflate(Transform, Data),
            _ = file:write(Output, Encoded),
            transfer(Transform, Input, Output);
        eof ->
            Encoded = zlib:deflate(Transform, <<>>, finish),
            _ = file:write(Output, Encoded),
            ok
    end.
