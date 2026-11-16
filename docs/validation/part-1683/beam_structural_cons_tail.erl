-module(beam_structural_cons_tail).
-export([strip/1, scan/2, nested/2]).
strip([{skip, _} | Rest]) -> strip(Rest);
strip([Head | Rest]) -> [Head | strip(Rest)];
strip([]) -> [].
scan([{skip, _} | Rest], Flag) -> scan(Rest, Flag);
scan([_ | Rest], Flag) when is_atom(Flag) -> scan(Rest, Flag);
scan([], _) -> done.
nested([{keep, [Value]} | Rest], Flag) -> nested(Rest, {Flag, Value});
nested([Head | Rest], Flag) -> nested(Rest, {Flag, Head});
nested([], Flag) -> Flag.
