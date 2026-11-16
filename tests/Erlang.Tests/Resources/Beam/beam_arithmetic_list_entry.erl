-module(beam_arithmetic_list_entry).
-export([run/3]).
run([], Trail, Acc) -> {Trail + 1, Acc};
run([Bit|Rest], Trail, Acc) ->
    run(Rest, Trail - 1, (Bit bsl Trail) bor Acc).
