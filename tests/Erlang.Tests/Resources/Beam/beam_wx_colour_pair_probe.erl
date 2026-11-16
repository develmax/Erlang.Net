-module(beam_wx_colour_pair_probe).
-export([leading/3, trailing/3, middle/4]).
-record(wx_ref, {ref, type, state}).
-record(wx_env, {ref, sv, debug}).
-define(COLOUR(Value), ((tuple_size(Value) =:= 3 orelse tuple_size(Value) =:= 4)
                        andalso is_integer(element(1, Value)))).
-define(CLASS(Type, Class), (Type =:= Class) orelse Type:parent_class(Class)).
-define(ENV, case erlang:get(wx_env) of
    #wx_env{ref=Ref} -> Ref;
    _ -> erlang:error({wx, unknown_env})
end).

leading(Text, Background, #wx_ref{type=Type}=Font)
  when ?COLOUR(Text), ?COLOUR(Background) ->
    ?CLASS(Type, wxFont),
    wxe_util:queue_cmd(wxe_util:color(Text), wxe_util:color(Background), Font, ?ENV, 1756),
    wxe_util:rec(1756).

trailing(#wx_ref{type=Type}=This, Foreground, Background)
  when ?COLOUR(Foreground), ?COLOUR(Background) ->
    ?CLASS(Type, wxCalendarCtrl),
    wxe_util:queue_cmd(This, wxe_util:color(Foreground), wxe_util:color(Background), ?ENV, 773).

middle(#wx_ref{type=Type}=This, First, Second, {X,Y}=Position)
  when ?COLOUR(First), ?COLOUR(Second), is_integer(X), is_integer(Y) ->
    ?CLASS(Type, wxDC),
    wxe_util:queue_cmd(This, wxe_util:color(First), wxe_util:color(Second), Position, ?ENV, 705).
