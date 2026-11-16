-module(beam_wx_tuple_overload_probe).
-export([rect_or_object/2, tuple_width/2, static_tuple/2]).
-record(wx_ref, {ref, type, state}).
-record(wx_env, {ref, sv, debug}).
-define(CLASS(Type, Class), ((Type) =:= Class) orelse (Type):parent_class(Class)).
-define(ENV, case erlang:get(wx_env) of
    #wx_env{ref=Ref} -> Ref;
    _ -> erlang:error({wx, unknown_env})
end).

rect_or_object(#wx_ref{type=Type}=This,{X,Y,W,H}=Rect)
  when is_integer(X),is_integer(Y),is_integer(W),is_integer(H) ->
    ?CLASS(Type, wxRegion),
    wxe_util:queue_cmd(This,Rect,?ENV,111),
    wxe_util:rec(111);
rect_or_object(#wx_ref{type=Type}=This,#wx_ref{type=OtherType}=Other) ->
    ?CLASS(Type, wxRegion),
    ?CLASS(OtherType, wxRegion),
    wxe_util:queue_cmd(This,Other,?ENV,112),
    wxe_util:rec(112).

tuple_width(#wx_ref{type=Type}=This,{X,Y}=Point)
  when is_integer(X),is_integer(Y) ->
    ?CLASS(Type, wxRegion),
    wxe_util:queue_cmd(This,Point,?ENV,113),
    wxe_util:rec(113);
tuple_width(#wx_ref{type=Type}=This,{X,Y,W,H}=Rect)
  when is_integer(X),is_integer(Y),is_integer(W),is_integer(H) ->
    ?CLASS(Type, wxRegion),
    wxe_util:queue_cmd(This,Rect,?ENV,114),
    wxe_util:rec(114).

static_tuple({X,Y}=First,{W,H}=Second)
  when is_integer(X),is_integer(Y),is_integer(W),is_integer(H) ->
    wxe_util:queue_cmd(First,Second,?ENV,115),
    wxe_util:rec(115).
