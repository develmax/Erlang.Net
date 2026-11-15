-module(beam_filtered_list_generator).
-export([guarded/1, boolean/1, comparison/2, identity/1]).

guarded(List) -> [{ok, V} || {tag, V} <- List, hd(V) > 0].
boolean(List) -> [{selected, V} || V <- List, choose(V)].
comparison(List, Key) -> [{Key, V} || {K, V} <- List, K =:= Key].
identity(List) -> [V || V <- List, is_atom(V)].

choose(crash) -> erlang:error(filter_crash);
choose(Value) -> put(last_filter, Value), Value.