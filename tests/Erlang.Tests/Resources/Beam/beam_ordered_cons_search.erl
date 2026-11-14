-module(beam_ordered_cons_search).
-export([contains/2]).
contains(Key, [Head | Tail]) when Key > Head -> contains(Key, Tail);
contains(Key, [Head | _]) when Key < Head -> false;
contains(_, [_ | _]) -> true;
contains(_, []) -> false.
