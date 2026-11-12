-module(part1502_numeric_controls).
-export([one/2, chain/3, guarded/2]).
one(A, B) -> A + B.
chain(A, B, C) -> A + B + C.
guarded(A, B) when is_number(A), is_number(B) -> A + B.
