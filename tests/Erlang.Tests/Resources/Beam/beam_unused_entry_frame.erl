-module(beam_unused_entry_frame).
-export([run/2]).
-record(hash_state, {n, p, l, hash}).
run(_Tab, State) when State == undefined ->
    #hash_state{n=1, p=1, l=0, hash=phash2}.
