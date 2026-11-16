-module(beam_octet_ranges).
-export([signed/1, unsigned/1, unguarded/1]).

signed(Value) ->
    if
        Value >= 0 -> nonnegative;
        true -> negative(Value, [])
    end.

negative(-1, [Head | _Tail] = Acc) when Head > 127 -> Acc;
negative(Value, Acc) -> negative(Value bsr 8, [Value band 255 | Acc]).

unsigned(Value) ->
    measured(Value + 0).

measured(Value) when Value =< 127 -> [Value];
measured(Value) -> octets(Value, []).

octets(0, Acc) -> Acc;
octets(Value, Acc) -> octets(Value bsr 8, [Value band 255 | Acc]).

unguarded(Value) ->
    any_octets(Value, []).

any_octets(0, Acc) -> Acc;
any_octets(Value, Acc) -> any_octets(Value bsr 8, [Value band 255 | Acc]).
