-module(beam_node_size_guards).
-export([port/1, floating/1, remote/1, bytes/1, local/1]).
port(X) when is_port(X) -> yes;
port(_) -> no.
floating(X) when is_float(X) -> yes;
floating(_) -> no.
remote(X) when node(X) =:= example -> yes;
remote(_) -> no.
bytes(X) when byte_size(X) =:= 2 -> yes;
bytes(_) -> no.
local(X) when node() =:= X -> yes;
local(_) -> no.
