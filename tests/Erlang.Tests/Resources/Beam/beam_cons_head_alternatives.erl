-module(beam_cons_head_alternatives).
-export([run/1]).
run([Head | Tail]) when Head == 32; Head == 13; Head == 10; Head == 9 ->
    run(Tail);
run([_ | _]) ->
    false;
run([]) ->
    true.
