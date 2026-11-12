-module(beam_list_caller_facts).
-export([run/1, unknown/1]).
run(Count) ->
    Bits = bits(Count),
    relay(Bits, 0).
unknown(Bits) -> generic(Bits, 2, 0).
relay([_,_|Rest], Acc) -> relay(Rest, Acc);
relay(Bits, Acc) -> fold(Bits, 2, Acc).
bits(0) -> [];
bits(1) -> [0];
bits(N) -> [1|bits(N-1)].
fold([], Trail, Acc) -> {Trail+1, Acc};
fold([Bit|Rest], Trail, Acc) -> fold(Rest, Trail-1, (Bit bsl Trail) bor Acc).
generic([], Trail, Acc) -> {Trail+1, Acc};
generic([Bit|Rest], Trail, Acc) -> generic(Rest, Trail-1, (Bit bsl Trail) bor Acc).

