-module(beam_wx_option_colour_probe).
-export([paint/3, paint/4, paint_pair/4, paint_pair/5, copy/5, copy/6]).
-record(wx_ref, {ref, type, state}).
-record(wx_env, {ref, sv, debug}).
-define(COLOUR(V), ((tuple_size(V) =:= 3 orelse tuple_size(V) =:= 4)
                    andalso is_integer(element(1, V)))).
-define(CLASS(Type, Class), (Type =:= Class) orelse Type:parent_class(Class)).
-define(ENV, case erlang:get(wx_env) of
    #wx_env{ref=Ref} -> Ref;
    _ -> erlang:error({wx, unknown_env})
end).

paint(This, {X,Y}=Point, Colour)
  when is_record(This, wx_ref), is_integer(X), is_integer(Y), ?COLOUR(Colour) ->
    paint(This, Point, Colour, []).
paint(#wx_ref{type=Type}=This, {X,Y}=Point, Colour, Options)
  when is_integer(X), is_integer(Y), ?COLOUR(Colour), is_list(Options) ->
    ?CLASS(Type, wxDC),
    Mapper = fun({style, _}=Arg) -> Arg;
                (Bad) -> erlang:error({badoption, Bad}) end,
    Mapped = lists:map(Mapper, Options),
    wxe_util:queue_cmd(This, Point, wxe_util:color(Colour), Mapped, ?ENV, 658),
    wxe_util:rec(658).

paint_pair(This, {X,Y}=Point, A, B)
  when is_record(This, wx_ref), is_integer(X), is_integer(Y), ?COLOUR(A), ?COLOUR(B) ->
    paint_pair(This, Point, A, B, []).
paint_pair(#wx_ref{type=Type}=This, {X,Y}=Point, A, B, Options)
  when is_integer(X), is_integer(Y), ?COLOUR(A), ?COLOUR(B), is_list(Options) ->
    ?CLASS(Type, wxDC),
    Mapper = fun({style, _}=Arg) -> Arg;
                (Bad) -> erlang:error({badoption, Bad}) end,
    Mapped = lists:map(Mapper, Options),
    wxe_util:queue_cmd(This, Point, wxe_util:color(A), wxe_util:color(B), Mapped, ?ENV, 686).

copy(This, {DestX,DestY}=Dest, {Width,Height}=Size, Source, {SrcX,SrcY}=Src)
  when is_record(This, wx_ref), is_integer(DestX), is_integer(DestY),
       is_integer(Width), is_integer(Height), is_record(Source, wx_ref),
       is_integer(SrcX), is_integer(SrcY) ->
    copy(This, Dest, Size, Source, Src, []).

copy(This, Dest, Size, Source, Src, Options) ->
    {This,Dest,Size,Source,Src,Options}.
