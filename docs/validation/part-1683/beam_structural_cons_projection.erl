-module(beam_structural_cons_projection).
-export([find/1]).
find([{attribute, _, module, Value} | _]) -> Value;
find([_ | Rest]) -> find(Rest).
