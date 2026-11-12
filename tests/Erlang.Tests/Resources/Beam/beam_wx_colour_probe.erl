-module(beam_wx_colour_probe).
-export([run/2, run/3, new/1, new/2]).
-record(wx_ref, {ref, type, state}).
-record(wx_env, {ref, sv, debug}).
-define(ENV, case erlang:get(wx_env) of
    #wx_env{ref=Ref} -> Ref;
    _ -> erlang:error({wx, unknown_env})
end).
run(#wx_ref{type=Type}=This, Colour)
  when (tuple_size(Colour) =:= 3 orelse tuple_size(Colour) =:= 4)
       andalso is_integer(element(1, Colour)) ->
    (Type =:= wxAuiTabArt) orelse Type:parent_class(wxAuiTabArt),
    wxe_util:queue_cmd(This, wxe_util:color(Colour), ?ENV, 2642).

run(#wx_ref{type=Type}=This, Colour, Position)
  when ((tuple_size(Colour) =:= 3 orelse tuple_size(Colour) =:= 4)
        andalso is_integer(element(1, Colour))), is_number(Position) ->
    (Type =:= wxGraphicsGradientStops) orelse Type:parent_class(wxGraphicsGradientStops),
    wxe_util:queue_cmd(This, wxe_util:color(Colour), Position, ?ENV, 850).

new(Colour) when (tuple_size(Colour) =:= 3 orelse tuple_size(Colour) =:= 4)
                 andalso is_integer(element(1, Colour)) ->
    new(Colour, []);
new(#wx_ref{type=Type}=Object) ->
    IsBrush = try (Type =:= wxBrush) orelse Type:parent_class(wxBrush)
              catch _:_ -> false end,
    IsBitmap = try (Type =:= wxBitmap) orelse Type:parent_class(wxBitmap)
               catch _:_ -> false end,
    Class = if IsBrush -> wxBrush;
               IsBitmap -> wxBitmap;
               true -> error({badarg, Type})
            end,
    wxe_util:queue_cmd(wx:typeCast(Object, Class), ?ENV, 1164),
    wxe_util:rec(1164).

new(Colour, Options)
  when ((tuple_size(Colour) =:= 3 orelse tuple_size(Colour) =:= 4)
        andalso is_integer(element(1, Colour))), is_list(Options) ->
    Mapper = fun({style, _} = Arg) -> Arg;
                (Bad) -> erlang:error({badoption, Bad}) end,
    Mapped = lists:map(Mapper, Options),
    wxe_util:queue_cmd(wxe_util:color(Colour), Mapped, ?ENV, 1165),
    wxe_util:rec(1165).
