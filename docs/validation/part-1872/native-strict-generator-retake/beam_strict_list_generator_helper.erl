-module(beam_strict_list_generator_helper).
-export([strict/1, relaxed/1, variable/1, nested/1, observe/1, split_source/1]).
strict(Input) ->
    [V || {tag,V} <:- Input].
relaxed(Input) ->
    [V || {tag,V} <- Input].
variable(Input) ->
    [V || V <:- Input].
nested(Input) ->
    [[V || {tag,V} <:- Row] || Row <- Input].
observe(Input) ->
    erase(events),
    try strict_effect(Input) of
        Value -> {Value, get(events)}
    catch error:Reason ->
        {Reason, get(events)}
    end.
strict_effect(Input) ->
    [remember(V) || {tag,V} <:- Input].
remember(V) ->
    Before = case get(events) of undefined -> []; Other -> Other end,
    put(events, [V | Before]),
    V.
split_source(Input) ->
    [V || {tag,V} <:-
          select_input(Input)].
select_input(Input) -> Input.
