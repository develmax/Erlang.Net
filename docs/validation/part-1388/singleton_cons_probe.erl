-module(singleton_cons_probe).
-export([convert/1, via/1]).

convert([]) ->
    [];
convert([Value]) ->
    [{tag,Value,Value}];
convert([Head|Tail]) ->
    Rest = convert(Tail),
    merge([{tag,Head,Head}], Rest).

via(Values) ->
    Values1 = [Value || Value <- Values],
    wrapper(Values1).

wrapper(Values) ->
    {ok, collect(Values)}.

collect([]) ->
    [];
collect([Value]) ->
    [{tag,Value,Value}];
collect([Head|Tail]) ->
    Rest = collect(Tail),
    merge([{tag,Head,Head}], Rest).

merge(Left, Right) ->
    lists:append(Left, Right).
