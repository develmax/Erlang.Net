-module(beam_structural_unused_fields).
-export([find/1]).
find([{attribute, Unused, module, Value} | _]) -> Value;
find([_ | Rest]) -> find(Rest).
