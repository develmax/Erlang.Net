-module(beam_wx_text_overload_probe).
-export([run/2, open/1, open/2, from_options/1, shape/2, new/1, draw/2, colour/2]).
-record(wx_ref, {ref, type, state}).
-record(wx_env, {ref, sv, debug}).
-define(ENV, case erlang:get(wx_env) of
    #wx_env{ref=Ref} -> Ref;
    _ -> erlang:error({wx, unknown_env})
end).
run(#wx_ref{type=Type}=This, Index) when is_integer(Index) ->
    (Type =:= wxListBox) orelse Type:parent_class(wxListBox),
    wxe_util:queue_cmd(This, Index, ?ENV, 1652);
run(#wx_ref{type=Type}=This, Text) when is_list(Text) orelse is_binary(Text) ->
    (Type =:= wxListBox) orelse Type:parent_class(wxListBox),
    Binary = unicode:characters_to_binary(Text),
    wxe_util:queue_cmd(This, Binary, ?ENV, 1653).

open(Text) when is_list(Text) orelse is_binary(Text) ->
    open(Text, []);
open(#wx_ref{type=Type}=Icon) ->
    (Type =:= wxIcon) orelse Type:parent_class(wxIcon),
    wxe_util:queue_cmd(Icon, ?ENV, 1055),
    wxe_util:rec(1055).

open(Text, Options) -> {Text, Options}.

from_options(Options) when is_list(Options) ->
    Mapper = fun({bitmap, #wx_ref{type=Type}} = Arg) ->
                     (Type =:= wxBitmap) orelse Type:parent_class(wxBitmap),
                     Arg;
                (Bad) -> erlang:error({badoption, Bad}) end,
    Mapped = lists:map(Mapper, Options),
    wxe_util:queue_cmd(Mapped, ?ENV, 3398),
    wxe_util:rec(3398);
from_options(#wx_ref{type=Type}=Bitmap) ->
    (Type =:= wxBitmap) orelse Type:parent_class(wxBitmap),
    wxe_util:queue_cmd(Bitmap, ?ENV, 3397),
    wxe_util:rec(3397).

shape(#wx_ref{type=ThisType}=This, #wx_ref{type=ShapeType}=Shape) ->
    (ThisType =:= wxTopLevelWindow) orelse ThisType:parent_class(wxTopLevelWindow),
    IsRegion = try (ShapeType =:= wxRegion) orelse ShapeType:parent_class(wxRegion)
               catch _:_ -> false end,
    IsPath = try (ShapeType =:= wxGraphicsPath) orelse ShapeType:parent_class(wxGraphicsPath)
             catch _:_ -> false end,
    Type = if IsRegion -> wxRegion;
              IsPath -> wxGraphicsPath;
              true -> error({badarg, ShapeType})
           end,
    wxe_util:queue_cmd(This, wx:typeCast(Shape, Type), ?ENV, 307),
    wxe_util:rec(307).

new(#wx_ref{type=DeviceType}=Device) ->
    IsWindow = try (DeviceType =:= wxWindowDC) orelse DeviceType:parent_class(wxWindowDC)
               catch _:_ -> false end,
    IsMemory = try (DeviceType =:= wxMemoryDC) orelse DeviceType:parent_class(wxMemoryDC)
               catch _:_ -> false end,
    IsGraphics = try (DeviceType =:= wxGraphicsContext) orelse DeviceType:parent_class(wxGraphicsContext)
                 catch _:_ -> false end,
    Type = if IsWindow -> wxWindowDC;
              IsMemory -> wxMemoryDC;
              IsGraphics -> wxGraphicsContext;
              true -> error({badarg, DeviceType})
           end,
    wxe_util:queue_cmd(wx:typeCast(Device, Type), ?ENV, 3563),
    wxe_util:rec(3563).

draw(#wx_ref{type=ThisType}=This, #wx_ref{type=DeviceType}=Device) ->
    (ThisType =:= wxGraphicsRenderer) orelse ThisType:parent_class(wxGraphicsRenderer),
    IsWindow = try (DeviceType =:= wxWindowDC) orelse DeviceType:parent_class(wxWindowDC)
               catch _:_ -> false end,
    IsMemory = try (DeviceType =:= wxMemoryDC) orelse DeviceType:parent_class(wxMemoryDC)
               catch _:_ -> false end,
    IsGraphics = try (DeviceType =:= wxGraphicsContext) orelse DeviceType:parent_class(wxGraphicsContext)
                 catch _:_ -> false end,
    Type = if IsWindow -> wxWindowDC;
              IsMemory -> wxMemoryDC;
              IsGraphics -> wxGraphicsContext;
              true -> error({badarg, DeviceType})
           end,
    wxe_util:queue_cmd(This, wx:typeCast(Device, Type), ?ENV, 834).

colour(#wx_ref{type=Type}=This, Name) when is_list(Name) orelse is_binary(Name) ->
    (Type =:= wxColourPickerCtrl) orelse Type:parent_class(wxColourPickerCtrl),
    Binary = unicode:characters_to_binary(Name),
    wxe_util:queue_cmd(This, Binary, ?ENV, 2241);
colour(#wx_ref{type=Type}=This, Colour)
  when (tuple_size(Colour) =:= 3 orelse tuple_size(Colour) =:= 4)
       andalso is_integer(element(1, Colour)) ->
    (Type =:= wxColourPickerCtrl) orelse Type:parent_class(wxColourPickerCtrl),
    wxe_util:queue_cmd(This, wxe_util:color(Colour), ?ENV, 2240).
