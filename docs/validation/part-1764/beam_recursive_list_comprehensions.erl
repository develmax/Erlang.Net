-module(beam_recursive_list_comprehensions).
-export([map/2, captured/3, embedded/2, choose/2]).

map(List, Prefix) ->
    [item(Prefix, Element) || Element <- List].

captured(List, Z, A) ->
    [record_item(Z, Element, A) || Element <- List].

embedded(List, Prefix) ->
    {tag, lists:reverse([item(Prefix, Element) || Element <- List])}.

choose(false, _) ->
    skipped;
choose({prefix, Prefix}, List) ->
    [item(Prefix, Element) || Element <- List].

item(Prefix, Element) -> {Prefix, Element}.
record_item(First, Element, Last) ->
    put(seen, [Element | get(seen)]),
    {First, Element, Last}.