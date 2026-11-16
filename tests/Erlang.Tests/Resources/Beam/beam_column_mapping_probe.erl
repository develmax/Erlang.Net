-module(beam_column_mapping_probe).
-export([alloc_columns/1, get_area_info/0]).

alloc_columns(Columns) ->
    [{"", 0, 240} |
     [{Column, 1, 140} || Column <- Columns]].

get_area_info() ->
    {ok, Rows, Warning} = area_view:read(),
    Values = [tuple_to_list(Row) || Row <- Rows],
    {area_columns(), Values, Warning}.

area_columns() ->
    [{"Name", 0, 150}, {"Size", 1, 150}].
