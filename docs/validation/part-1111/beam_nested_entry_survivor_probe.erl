-module(beam_nested_entry_survivor_probe).
-export([one/3, two/3]).
one(A,B,C) -> finish(C,read(A,B)).
two(A,B,C) -> finish(B,wrap(C,read(A,B))).
read(A,B) -> {A,B}.
wrap(A,B) -> {A,B}.
finish(A,B) -> {A,B}.
