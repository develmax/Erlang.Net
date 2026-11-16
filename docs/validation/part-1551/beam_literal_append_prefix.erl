-module(beam_literal_append_prefix).
-export([run/2]).
run(empty, Tail) -> wrap([] ++ Tail);
run(four, Tail) -> wrap([a,b,c,d] ++ Tail);
run(long, Tail) -> wrap([a,b,c,d,e] ++ Tail);
run(number, Tail) -> list_to_integer("-" ++ Tail);
run(_, Tail) -> wrap(Tail).
wrap(Value) -> {value,Value}.
