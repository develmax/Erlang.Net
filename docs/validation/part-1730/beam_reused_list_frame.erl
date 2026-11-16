-module(beam_reused_list_frame).
-export([run/3]).
run(A,B,C) -> "tag " ++ atom_to_list(B) ++ atom_to_list(C) ++ atom_to_list(A).
