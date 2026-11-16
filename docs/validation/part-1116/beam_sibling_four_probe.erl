-module(beam_sibling_four_probe).
-export([run/3]).
run(A,B,C) -> finish(make(first,A), make(second,B), make(third,C), make(fourth,B), C).
make(Tag,Value) -> {Tag,Value}.
finish(A,B,C,D,E) -> {A,B,C,D,E}.
