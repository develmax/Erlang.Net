-module(beam_live_list_branch).
-export([run/2]).
run([$=|Rest], Acc) -> {lists:reverse(Acc), Rest};
run([Char|Rest], Acc) -> run(Rest, [Char|Acc]);
run([], Acc) -> lists:reverse(Acc).
