-module(beam_xor_direct).
-export([bits/2,neg/1,bool/2]).
bits(A,B) -> A bxor B.
neg(A) -> bnot A.
bool(A,B) -> A xor B.
