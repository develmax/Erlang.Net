-module(beam_forwarded_tail_probe).
-export([forward/3]).

forward(Map, Left, Right) ->
    Built = identity(Map),
    finish(maps:values(Built), Left, Right).

finish(Values, Left, Right) ->
    {Values,Left,Right}.

identity(Value) ->
    Value.
