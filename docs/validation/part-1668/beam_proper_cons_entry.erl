-module(beam_proper_cons_entry).
-export([run/1, unknown/1]).
run(Items) -> checked([Item || Item <- Items]).
checked([{tag,V}|_]) -> V;
checked([]) -> empty.
unknown([{tag,V}|_]) -> V;
unknown([]) -> empty.
