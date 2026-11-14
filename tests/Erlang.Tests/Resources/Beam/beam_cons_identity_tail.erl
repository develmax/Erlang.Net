-module(beam_cons_identity_tail).
-export([run/1, finish/1]).

run([32 | Tail]) -> finish(Tail);
run([13 | Tail]) -> finish(Tail);
run([10 | Tail]) -> finish(Tail);
run([9 | Tail]) -> finish(Tail);
run(Value) -> Value.

finish(Value) -> {selected, Value}.
