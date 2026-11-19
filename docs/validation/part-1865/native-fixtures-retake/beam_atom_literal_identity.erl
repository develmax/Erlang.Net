-module(beam_atom_literal_identity).
-export([translate/1, guarded/1, duplicate/1, numbers/1]).
translate(any) -> absent;
translate(loopback) -> {local, <<>>};
translate(IP) -> IP.
guarded(any) -> absent;
guarded(loopback) -> {local, <<>>};
guarded(IP) when is_atom(IP) -> IP.
duplicate(any) -> absent;
duplicate(any) -> {local, <<>>};
duplicate(IP) -> IP.
numbers(1) -> absent;
numbers(2) -> {local, <<>>};
numbers(Number) -> Number.
