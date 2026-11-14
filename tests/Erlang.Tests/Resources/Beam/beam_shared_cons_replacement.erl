-module(beam_shared_cons_replacement).
-export([run/2]).
run([$a | Tail], Acc) -> run(Tail, [$x | Acc]);
run([$b | Tail], Acc) -> run(Tail, [$x | Acc]);
run([$c | Tail], Acc) -> run(Tail, [$x | Acc]);
run([Head | Tail], Acc) -> run(Tail, [Head | Acc]);
run([], Acc) -> lists:reverse(Acc).
