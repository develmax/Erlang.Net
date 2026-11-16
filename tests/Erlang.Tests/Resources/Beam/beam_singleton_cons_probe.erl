-module(beam_singleton_cons_probe).
-export([convert/1]).

convert([]) ->
    [];
convert([Value]) ->
    [{tag,Value,Value}];
convert([Head|Tail]) ->
    Rest = convert(Tail),
    merge([{tag,Head,Head}], Rest).

merge(Left, Right) ->
    lists:append(Left, Right).
