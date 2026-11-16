-module(beam_retained_tuple_field).
-export([inline/1, bound/1]).
-record(state, {name, data}).
inline(State = #state{name = Name}) ->
    finish(State, filename:rootname(Name)).
bound(#state{name = Name} = State) ->
    Root = filename:rootname(Name),
    finish(State, Root).
finish(State, Name) ->
    {State, Name}.
