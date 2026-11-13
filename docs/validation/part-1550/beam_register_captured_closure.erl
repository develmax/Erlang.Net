-module(beam_register_captured_closure).
-export([make/3,empty/1]).
make(Z, _, A) ->
    fun
        (flip) -> {A,Z};
        (Value) -> {Z,Value,A}
    end.
empty(_) ->
    fun
        (yes) -> 1;
        (_) -> 0
    end.
