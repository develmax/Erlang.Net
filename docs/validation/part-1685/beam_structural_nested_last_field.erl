-module(beam_structural_nested_last_field).
-export([find/1]).
find([{attribute, _, module, {Value, _}} | _]) -> Value;
find([_ | Rest]) -> find(Rest).
