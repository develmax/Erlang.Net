-module(beam_finite_atom_probe).
-export([pick_first/0, pick_second/0, same_first/0, same_second/0, choose_third/0, pair_first/0, pair_second/0, route_first/1, route_second/1, open/1, tag_unused/1, wide_alpha/0, wide_beta/0, wide_gamma/0, wide_delta/0, wide_epsilon/0, wide_zeta/0]).

pick_first() -> pick(alpha).
pick_second() -> pick(beta).
same_first() -> same(alpha).
same_second() -> same(beta).
choose_third() -> pick(gamma).

pick(alpha) -> 11;
pick(beta) -> 22;
pick(gamma) -> 33;
pick(unused) -> 44.

same(alpha) -> stable;
same(beta) -> stable;
same(unused) -> other.

pair_first() -> forward(alpha).
pair_second() -> forward(beta).
forward(Key) -> pair(Key).
pair(alpha) -> first;
pair(beta) -> second;
pair(unused) -> other.

route_first(Value) -> route(alpha, Value).
route_second(Value) -> route(beta, Value).
route(alpha, Value) -> tag_first(Value);
route(beta, Value) -> tag_second(Value);
route(unused, Value) -> tag_unused(Value).
tag_first(Value) -> {first, Value}.
tag_second(Value) -> {second, Value}.
tag_unused(Value) -> {unused, Value}.

open(Key) -> open_pick(Key).
open_pick(alpha) -> first;
open_pick(beta) -> second;
open_pick(unused) -> other.

wide_alpha() -> wide(alpha).
wide_beta() -> wide(beta).
wide_gamma() -> wide(gamma).
wide_delta() -> wide(delta).
wide_epsilon() -> wide(epsilon).
wide_zeta() -> wide(zeta).

wide(alpha) -> 1;
wide(beta) -> 2;
wide(gamma) -> 3;
wide(delta) -> 4;
wide(epsilon) -> 5;
wide(zeta) -> 6.
