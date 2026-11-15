-module(beam_entry_frame_probe).
-export([shared_literal/2, shared_guard/2, single_tag/2, nested_common/2]).
shared_literal({message,one,X}, Param) ->
    V = first(X),
    {V,Param};
shared_literal({message,two,X}, Param) ->
    V = second(Param),
    {V,X};
shared_literal(_Else, Param) ->
    {default,Param}.
shared_guard({message,K,V}, Expected) when K =:= Expected ->
    X = first(V),
    {X,Expected};
shared_guard({message,K,V}, Expected) ->
    Y = second(V),
    {Y,K,Expected};
shared_guard(_Else, Expected) ->
    {default,Expected}.
single_tag({message,X}, Param) ->
    V = first(X),
    Y = second(V),
    {Y,Param};
single_tag(_Else, Param) ->
    {default,Param}.
nested_common({message,{one,X}}, Param) ->
    V = first(X),
    {V,Param};
nested_common({message,{two,X}}, Param) ->
    V = second(Param),
    {V,X};
nested_common(_Else, Param) ->
    {default,Param}.
first(X) -> put(first,X).
second(X) -> put(second,X).
