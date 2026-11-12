-module(dynamic_apply_register_probe).
-export([f/3, zero/0, m/0]).
target() -> lists.
target_self() -> ?MODULE.
f(X, Y, Z) -> (target()):zip3(X, Y, Z).
zero() -> (target_self()):m().
m() -> ok.

