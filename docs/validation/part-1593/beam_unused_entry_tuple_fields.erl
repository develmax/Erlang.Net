-module(beam_unused_entry_tuple_fields).
-export([run/2, same/1]).
run({missing, _Unused}, _) -> missing;
run({item, Value, Gate, _Unused}, Other) when Gate =:= ready ->
    Prepared = prepare(Value),
    {Prepared,Other};
run(_, _) -> fallback.
same({same, Value, Value}) -> same;
same(_) -> fallback.
prepare(Value) -> {prepared,Value}.
