-module(beam_tuple_result_type).
-export([run/1]).
-import(lists,[reverse/1]).
run(Input) ->
    case select(Input) of
        missing -> {missing,Input};
        Values -> {values,Values}
    end.
select(Input) ->
    case read(Input, []) of
        {ok, Values} -> Values;
        _ -> missing
    end.
read([], Acc) -> {ok, reverse(Acc)};
read([Head|Tail], Acc) -> read(Tail, [Head|Acc]);
read(Other, _) -> {error, Other}.
