-module(beam_throw_stack_guard_probe).
-export([safe/1, observed/1, bound/2, mismatch/0, escape/0, escaped/0, cleanup/0, choices/0]).

safe(Input) ->
    try hidden(Input, []) catch
        throw:{error,missing} -> handled
    end.

observed(Input) ->
    try seen(Input, []) catch
        throw:{error,missing}:Stack -> Stack =/= []
    end.

bound(Input, Reason) ->
    try checked(Input, []) catch
        throw:Reason -> handled
    end.

mismatch() ->
    try bound([], wrong) catch
        throw:{error,missing}:Stack -> Stack =/= []
    end.

escape() ->
    fun escaped_scan/2.

escaped() ->
    F = escape(),
    try F([], []) catch
        throw:{error,missing}:Stack -> Stack =/= []
    end.

hidden([stop|Rest], Acc) ->
    {{token,lists:reverse(Acc)},Rest};
hidden([Head|Rest], Acc) ->
    hidden(Rest, [Head|Acc]);
hidden([], _) ->
    throw({error,missing}).

seen([stop|Rest], Acc) ->
    {{token,lists:reverse(Acc)},Rest};
seen([Head|Rest], Acc) ->
    seen(Rest, [Head|Acc]);
seen([], _) ->
    throw({error,missing}).

checked([stop|Rest], Acc) ->
    {{token,lists:reverse(Acc)},Rest};
checked([Head|Rest], Acc) ->
    checked(Rest, [Head|Acc]);
checked([], _) ->
    throw({error,missing}).

escaped_scan([stop|Rest], Acc) ->
    {{token,lists:reverse(Acc)},Rest};
escaped_scan([Head|Rest], Acc) ->
    escaped_scan(Rest, [Head|Acc]);
escaped_scan([], _) ->
    throw({error,missing}).

cleanup() ->
    catch protect().

protect() ->
    try cleanup_scan([], []) after put(cleaned, true) end.

choices() ->
    try choice_scan([], []) catch
        throw:first -> first;
        throw:second -> second;
        throw:third -> third
    end.

cleanup_scan([stop|Rest], Acc) ->
    {{token,lists:reverse(Acc)},Rest};
cleanup_scan([Head|Rest], Acc) ->
    cleanup_scan(Rest, [Head|Acc]);
cleanup_scan([], _) ->
    throw({error,missing}).

choice_scan([stop|Rest], Acc) ->
    {{token,lists:reverse(Acc)},Rest};
choice_scan([Head|Rest], Acc) ->
    choice_scan(Rest, [Head|Acc]);
choice_scan([], _) ->
    throw(first).
