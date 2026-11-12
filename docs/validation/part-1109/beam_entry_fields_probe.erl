-module(beam_entry_fields_probe).
-export([read/1, reversed/1, remote/1]).
read(Input) ->
    case source(Input) of
        {tag,X,Y} -> out(X,Y);
        _ -> missing
    end.
reversed(Input) ->
    case source(Input) of
        {tag,X,Y} -> out(Y,X);
        _ -> missing
    end.
remote(Input) ->
    case source(Input) of
        {tag,X,Y} -> maps:get(X,Y);
        _ -> missing
    end.
source(Value) -> Value.
out(X,Y) -> {X,Y}.
