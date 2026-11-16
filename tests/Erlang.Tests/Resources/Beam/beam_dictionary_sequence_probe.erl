-module(beam_dictionary_sequence_probe).
-export([resolve/1, pair/2]).

resolve(Type) ->
    Current = get(current),
    Data = get({Current, imports}),
    finish(Type, Current, Data).

pair(Key, Input) ->
    Value = erlang:get(Key),
    Extra = erlang:get({Value, extra}),
    finish(Input, Value, Extra).

finish(Type, Current, Data) ->
    {Type,Current,Data}.
