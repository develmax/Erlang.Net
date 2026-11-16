-module(beam_entry_spills).
-export([emit/2, across/2]).

emit(Term, Flag) ->
    case Flag of
        yes -> put(captured, Term);
        no -> get(Term)
    end.

across(X, Y) ->
    Old = put(key, Y),
    {Old, X}.
