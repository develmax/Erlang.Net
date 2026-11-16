-module(beam_numeric_compare_probe).
-export([equal/2, unequal/2, guard/2, atom_guard/1, zero/1, guard_ne/2, atom_value/1]).
equal(A,B) -> A == B.
unequal(A,B) -> A /= B.
guard(A,B) when A == B -> yes;
guard(_,_) -> no.
atom_guard(A) when A /= probe -> yes;
atom_guard(_) -> no.
zero(A) -> A == 0.
guard_ne(A,B) when A /= B -> yes;
guard_ne(_,_) -> no.
atom_value(A) -> A == probe.
