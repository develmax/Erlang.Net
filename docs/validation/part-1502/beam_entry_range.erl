-module(beam_entry_range).
-export([closed/2, exposed/2, escaped/0, referenced_seed/2]).

closed(0, V) -> push(0, [], V);
closed(2, V) -> push(2, [], V);
closed(4, V) -> push(4, [], V).

push(State, Stack, V) -> pop([State | Stack], V).
pop([State | _], V) -> route(State, V).

route(0, V) -> zero(V);
route(2, V) -> two(V);
route(4, V) -> four(V).

exposed(0, V) -> zero(V);
exposed(2, V) -> two(V);
exposed(4, V) -> four(V).

escaped() -> fun referenced/2.

referenced_seed(0, V) -> referenced(0, V);
referenced_seed(2, V) -> referenced(2, V);
referenced_seed(4, V) -> referenced(4, V).

referenced(0, V) -> zero(V);
referenced(2, V) -> two(V);
referenced(4, V) -> four(V).

zero(V) -> {zero, V}.
two(V) -> {two, V}.
four(V) -> {four, V}.
