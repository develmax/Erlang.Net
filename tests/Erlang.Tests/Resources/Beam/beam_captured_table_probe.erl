-module(beam_captured_table_probe).
-export([get_info/0]).

get_info() ->
    table_view:progress({ok, "Loading"}),
    Memory = memory(),
    table_view:progress({ok, 33}),
    {Rows, Warning} = allocate(),
    table_view:progress({ok, 66}),
    Area = area(),
    table_view:progress({ok, 100}),
    [{"Memory", table_view, Memory}
     | [{Title, table_view, {Columns, Data, Warning}}
        || {Title, Columns, Data} <- Rows]] ++
    [{"Area", table_view, Area}].

memory() -> table_view:memory().
allocate() ->
    {ok, Input, Warning} = table_view:select(),
    {rows(Input), Warning}.
area() -> table_view:area().

rows(empty) -> [];
rows(_) -> [{"Allocator", "Columns", "Data"}].
