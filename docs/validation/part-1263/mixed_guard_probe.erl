-module(mixed_guard_probe).
-export([check/2, check_and_finish/2, guarded/3, tuple_tail/1, prefixed_tail/1]).
-compile({inline, [target/0]}).

target() -> maps.

check(Key, Map) when is_atom(Key), is_map(Map) ->
    Module = target(),
    Module:get(Key, Map),
    ok.

check_and_finish(Key, Map) when is_atom(Key), is_map(Map) ->
    Module = target(),
    Module:get(Key, Map),
    finish(42).

finish(Value) -> {done, Value}.
guarded(Flag, Tuple, Map) when (0 =:= Flag orelse 1 =:= Flag), tuple_size(Tuple) =:= 2, is_map(Map) ->
    Module = target(),
    Module:get(Flag, Map),
    ok.

tuple_tail({A, B, C}) -> pack(A, B, C).
pack(A, B, C) -> {A, B, C}.

prefixed_tail({A, B, C}) -> pack_prefixed(prefix, A, B, C).
pack_prefixed(Prefix, A, B, C) -> {Prefix, A, B, C}.
