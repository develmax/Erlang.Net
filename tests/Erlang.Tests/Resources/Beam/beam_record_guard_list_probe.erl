-module(beam_record_guard_list_probe).
-export([scan/3, observed/1]).
-record(entry, {mode, data}).

scan(Key, [Item|Rest], Acc) when is_record(Item, entry) ->
    case Item#entry.mode of
        emit ->
            save(Key, lists:reverse([Item|Acc])),
            finish(Key, Rest);
        _ ->
            scan(Key, Rest, [Item|Acc])
    end;
scan(Key, [Item|Rest], Acc) ->
    scan(Key, Rest, [Item|Acc]);
scan(Key, [], Acc) ->
    save(Key, lists:reverse(Acc)).

save(Key, Items) ->
    persistent_term:put(Key, Items).

finish(Key, Rest) ->
    {Key, Rest}.

observed(Key) ->
    persistent_term:get(Key).
