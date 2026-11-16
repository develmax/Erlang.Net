-module(beam_nested_trim_probe).
-export([all_dead/3, retained/3]).
all_dead(A,B,C) -> finish(tail,wrap(C,read(A,B))).
retained(A,B,C) -> finish(C,wrap(C,read(A,B))).
read(A,B) -> {A,B}.
wrap(A,B) -> {A,B}.
finish(A,B) -> {A,B}.
