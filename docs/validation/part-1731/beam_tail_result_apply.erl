-module(beam_tail_result_apply).
-export([run/1, result/1, effect/0]).
run(Arg) ->
    note(Arg),
    Mod = select(Arg),
    Mod:result(Arg).
select({Mod, _}) -> Mod.
result(Arg) -> {result, Arg}.
note(Arg) -> erlang:put(note, Arg).
effect() -> erlang:erase(note).
