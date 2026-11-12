-module(tuple_tail_probe).
-export([forward/2, pack/3]).
forward({left, Value}, Extra) -> pack(Extra, Value, Value);
forward({right, marker, Value}, Extra) -> ?MODULE:pack(Value, Extra, external).
pack(A, B, C) -> {A, B, C}.
