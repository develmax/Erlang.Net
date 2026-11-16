-module(beam_entry_atom_suffix).
-export([run/3]).
run(_, {missing, _}, _) -> missing;
run(raw, {Value, _}, _) -> {ok, Value};
run(convert, {Value, Options}, Extra) ->
    Prepared = prepare(Options, Extra),
    {Value, Prepared};
run(_, _, _) -> fallback.
prepare(Options, Extra) -> {Options, Extra}.
