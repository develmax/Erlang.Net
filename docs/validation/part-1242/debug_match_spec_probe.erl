-module(debug_match_spec_probe).
-export([select/1]).
-compile({parse_transform, ms_transform}).
select(Min) ->
    ets:fun2ms(fun({tag, Value} = Row) when is_integer(Value), Value > Min -> Row;
                  ({fallback, Value}) -> {result, Value}
               end).
