-module(beam_dominating_helper_constants).
-export([lookup/1, events/0, invoke_closure/2, invoke_shadow/2, invoke_repeated/3, checked/2, shadow_generator/0]).
-record(row, {name, count}).
lookup(Rows) ->
    Fields = record_info(fields, row),
    remember(lookup),
    [decorate(Fields, Whole) || Whole = #row{} <- Rows].
events() -> get(helper_events).
remember(Value) ->
    Before = case get(helper_events) of undefined -> []; Found -> Found end,
    put(helper_events, [Value | Before]),
    ok.
decorate(Fields, Row) -> {Fields, Row}.
closure(Outer) ->
    Tag = {fixed,2},
    remember(closure),
    fun(Item) -> {Tag,Outer,Item} end.
invoke_closure(Outer, Item) ->
    F = closure(Outer),
    F(Item).
shadow(Outer) ->
    Tag = fixed,
    fun
        (Tag) when is_integer(Tag) -> {Tag,Outer};
        (Item) -> {Tag,Outer,Item}
    end.
invoke_shadow(Outer, Item) ->
    F = shadow(Outer),
    F(Item).
repeated(Outer) ->
    Tag = fixed,
    fun
        (Tag, Tag) -> {Tag,Outer};
        (Left, Right) -> {Tag,Outer,Left,Right}
    end.
invoke_repeated(Outer, Left, Right) ->
    F = repeated(Outer),
    F(Left, Right).
checked(Expected, Rows) ->
    Expected = fixed,
    [decorate(Expected, Item) || Item <- Rows].
shadow_generator() ->
    Source = [one,two],
    [Source || Source <- Source, is_atom(Source)].
