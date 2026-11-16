-module(beam_delayed_result_spill).
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
    Third = slider(Panel, Start, Min, Max, [{style, style(third) bor 16}]),
    remember(Panel, First, first_value),
    remember(Panel, Second, second_value),
    remember(Panel, Third, third_value),
    {Panel, #state{panel=Panel,config=Config}}.

repeat(Input) ->
    Target = fixed,
    Target = Input,
    put(matched, Target).

remember(Panel, Value, Key) -> put({Panel,Key}, Value).
keep(Value) -> {panel, Value}.
slider(Panel, Start, Min, Max, Options) -> {Panel, Start, Min, Max, Options}.
style(Name) ->
    put(Name, true),
    1.
