-module(beam_local_atom_moved).
-export([start/2]).
start(Env, Data) -> pick(Env, Data, marker).
pick(Env, Data, Key) ->
    case dep:lookup(Data, Key) of
        [] -> missing;
        [Hit | _] -> dep:keep(Env, Key, Hit)
    end.
