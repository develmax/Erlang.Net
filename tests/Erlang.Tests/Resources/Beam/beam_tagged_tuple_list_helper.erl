-module(beam_tagged_tuple_list_helper).
-export([aliases/1, records/1, fields/1, record_fields/1, caught/2, repeated/1, guarded/1]).
-record(entry,{value,other}).

aliases(Values) ->
    [T || {entry, _, _} = T <- Values].

records(Values) ->
    [T || T = #entry{} <- Values].

fields(Values) ->
    [Value || {entry, Value, _} <- Values].

record_fields(Values) ->
    [Value || #entry{value = Value} <- Values].

repeated(Values) ->
    [Value || {entry, Value, Value} <- Values].

guarded(Values) ->
    [T || T = {entry, Value, _} <- Values, is_integer(Value)].

strict(Values) ->
    [T || T = {entry, _, _} <:- Values].

caught(aliases, Values) ->
    try aliases(Values)
    catch error:Reason -> Reason
    end;
caught(strict, Values) ->
    try strict(Values)
    catch error:Reason -> Reason
    end.
