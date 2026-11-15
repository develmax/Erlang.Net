-module(beam_straight_constants).
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
    put(values, {First, Second}),
    {Panel, #state{panel=Panel,config=Config}}.

repeat(Input) ->
    Target = fixed,
    Target = Input,
    put(matched, Target).

keep(Value) -> {panel, Value}.
slider(Panel, Start, Min, Max, Options) -> {Panel, Start, Min, Max, Options}.
style(Name) ->
    put(Name, true),
    1.
