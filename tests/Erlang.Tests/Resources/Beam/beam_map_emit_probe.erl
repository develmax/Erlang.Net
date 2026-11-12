-module(beam_map_emit_probe).
-export([make/2, insert/2, exact/2, empty/1]).
make(A,B) -> #{z => A, a => B}.
insert(M,V) -> M#{key => V}.
exact(M,V) -> M#{key := V}.
empty(M) -> M#{}.
