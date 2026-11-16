-module(beam_call_result_slot_reuse).
-export([run/1,repeat/1]).
-record(state,{panel,config}).

run(Config) ->
    Parent = put(parent, Config),
    Panel = keep(Parent),
    Min = 0,
    Max = 100,
    Start = 25,
    First = slider(Panel, Start, Min, Max, [{style, style(first) bor 4}]),
    Second = slider(Panel, Start, Min, Max, [{style, style(second) bor 8}]),
    remember(first_value, First),
    remember(second_value, Second),
    {Panel, #state{panel=Panel,config=Config}}.

repeat(Input) ->
    Target = fixed,
    Target = Input,
    put(matched, Target).

remember(Key, Value) -> put(Key, Value).
keep(Value) -> {panel, Value}.
slider(Panel, Start, Min, Max, Options) -> {Panel, Start, Min, Max, Options}.
style(Name) ->
    put(Name, true),
    1.
