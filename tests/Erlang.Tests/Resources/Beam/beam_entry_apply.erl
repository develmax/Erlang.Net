-module(beam_entry_apply).
-export([list/2, map/2]).
list([], Module) -> Module:f();
list(_, _) -> error.
map(#{}, Module) when is_atom(Module) -> Module:f();
map(_, _) -> none.
