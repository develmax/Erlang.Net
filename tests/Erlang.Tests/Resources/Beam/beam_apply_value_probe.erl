-module(beam_apply_value_probe).
-export([zero/1, swap/3, identity/2, const/1, repeated/2]).
zero(F) -> F().
swap(F, A, B) -> F(B, A).
identity(A, F) -> F(A).
const(F) -> F(ok).
repeated(F, A) -> F(A, A).
