-module(beam_terminal_operator_call).
-export([filename/1, append/2, subtract/2, difference/2, quotient/2, nested/2, opaque/1]).

filename(Input) ->
    opaque(Input) ++ ".app".

append(Input, Prefix) ->
    Prefix ++ ?MODULE:opaque(Input).

subtract(Input, Removed) ->
    ?MODULE:opaque(Input) -- Removed.

difference(Input, Start) ->
    Start - ?MODULE:opaque(Input).

quotient(Input, Denominator) ->
    ?MODULE:opaque(Input) / Denominator.

nested(Input, Offset) ->
    Offset + ?MODULE:opaque(?MODULE:opaque(Input)).

opaque(Value) ->
    Value.
