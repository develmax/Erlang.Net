-module(beam_list_record_merge_probe).
-export([merge/2]).
-record(item, {id, flags, mode, format}).

merge([First = #item{mode=extend}, Second | Rest], Acc) ->
    merge([First#item{mode=Second#item.mode, format=Second#item.format} | Rest], Acc);
merge([First = #item{mode={default,extend}}, Second | Rest], Acc) ->
    merge([First#item{mode=Second#item.mode, format=Second#item.format} | Rest], Acc);
merge([First = #item{mode={default,auto}}, Second | Rest], Acc) ->
    merge([First#item{mode=Second#item.mode, format=Second#item.format} | Rest], Acc);
merge([Head | Tail], Acc) ->
    merge(Tail, [Head | Acc]);
merge([], Acc) ->
    lists:reverse(Acc).
