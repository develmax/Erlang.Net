-module(beam_entry_tag_dispatch_probe).
-export([same_arity/1, mixed_arity/1]).
same_arity(Input) ->
    case source(Input) of
        {left,X,Y} -> out(X,Y);
        {right,X,Y} -> out(Y,X);
        _ -> missing
    end.
mixed_arity(Input) ->
    case source(Input) of
        {left,X,Y} -> out(X,Y);
        {right,X} -> out(right,X);
        _ -> missing
    end.
source(Value) -> Value.
out(X,Y) -> {X,Y}.
