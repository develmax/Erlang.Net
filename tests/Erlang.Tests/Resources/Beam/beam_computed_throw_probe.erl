-module(beam_computed_throw_probe).
-export([caught/1, observed/1, tuple_switch/1, nested/1]).

caught(Value) ->
    try choose(false, Value) catch
        throw:{error,{token,Text}} -> Text
    end.

observed(Value) ->
    try keep_stack(Value) catch
        throw:{error,{token,Text}}:Stack -> {Text,Stack =/= []}
    end.

choose(Flag, Value) ->
    case Flag of
        true -> keep;
        false -> throw({error,{token,integer_to_list(Value)}})
    end.

keep_stack(Value) ->
    throw({error,{token,integer_to_list(Value)}}).

tuple_switch(Value) ->
    try switch_scan(Value) catch
        throw:{a,A} -> {first,A};
        throw:{b,B,C} -> {second,B,C}
    end.

switch_scan(Value) ->
    throw({a,integer_to_list(Value)}).

nested(Value) ->
    try
        try nested_scan(Value) catch
            error:badarg -> invalid
        end
    catch
        throw:{result,Text} -> Text
    end.

nested_scan(Value) ->
    throw({result,integer_to_list(Value)}).
