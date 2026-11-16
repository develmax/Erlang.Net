-module(beam_shared_entry_frame).
-export([run/3]).
run(raw, short, _) -> [];
run(raw, full, X) -> wrap(left(right(X)));
run(_, _, X) -> wrap(right(left(X))).
left(X) -> {left, X}.
right(X) -> {right, X}.
wrap(X) -> {wrapped, X}.
