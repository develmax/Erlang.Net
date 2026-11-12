-module(beam_atom_selector_probe).
-export([converted/1, guarded/1, unknown/1]).

converted(Name) -> select(list_to_atom(Name)).
guarded(Name) when is_atom(Name) -> select(Name);
guarded(_) -> invalid.
unknown(Name) -> select_unknown(Name).

select(alpha) -> first;
select(beta) -> second;
select(_) -> other.

select_unknown(alpha) -> first;
select_unknown(beta) -> second;
select_unknown(_) -> other.
