-module(constant_remote_probe).
-export([check/2, check_and_finish/2]).
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
