-module(checked_root_order_fixture).
-export([unary/1, binary/1]).
-record(payload, {entry, next, retained}).

unary(Payload) ->
    Result = checked_root_order_provider:first(Payload#payload.entry),
    checked_root_order_provider:unary(Payload#payload.next),
    case Result of
        ok -> {ok, Payload#payload.retained};
        {error, Reason} -> {error, Reason}
    end.

binary(Payload) ->
    Result = checked_root_order_provider:first(Payload#payload.entry),
    Next = checked_root_order_provider:binary(marker, Payload#payload.next),
    Last = checked_root_order_provider:unary(Next),
    {Result, Last, Payload#payload.retained}.
