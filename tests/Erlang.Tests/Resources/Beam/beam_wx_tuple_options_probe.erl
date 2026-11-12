-module(beam_wx_tuple_options_probe).
-export([run/4, run/5, run/6, run/7, enable/2, enable/3, set_status/2, set_status/3, show/2, show/3, init/2, init/3, font/3, font/4]).
-record(wx_ref, {ref, type, state}).
-record(wx_env, {ref, sv, debug}).
-define(ENV, case erlang:get(wx_env) of
    #wx_env{ref=Ref} -> Ref;
    _ -> erlang:error({wx, unknown_env})
end).
run(This, Index, {X,Y} = Pos, Choices)
  when is_record(This, wx_ref), is_integer(Index), is_integer(X), is_integer(Y), is_list(Choices) ->
    run(This, Index, Pos, Choices, []).
run(#wx_ref{type=Type}=This, Index, {X,Y} = Pos, Choices, Options)
  when is_integer(Index), is_integer(X), is_integer(Y), is_list(Choices), is_list(Options) ->
    (Type =:= wxChoice) orelse Type:parent_class(wxChoice),
    Converted = [unicode:characters_to_binary(Item) || Item <- Choices],
    Mapper = fun({style, _} = Arg) -> Arg;
                (Bad) -> erlang:error({badoption, Bad}) end,
    Mapped = lists:map(Mapper, Options),
    wxe_util:queue_cmd(This, Index, Pos, Converted, Mapped, ?ENV, 1567),
    wxe_util:rec(1567).

run(#wx_ref{type=Type}=This, Index, Label, {X,Y} = Pos, Choices, Options)
  when is_integer(Index), (is_list(Label) orelse is_binary(Label)),
       is_integer(X), is_integer(Y), is_list(Choices), is_list(Options) ->
    (Type =:= wxChoice) orelse Type:parent_class(wxChoice),
    Binary = unicode:characters_to_binary(Label),
    Converted = [unicode:characters_to_binary(Item) || Item <- Choices],
    Mapper = fun({style, _} = Arg) -> Arg;
                (Bad) -> erlang:error({badoption, Bad}) end,
    Mapped = lists:map(Mapper, Options),
    wxe_util:queue_cmd(This, Index, Binary, Pos, Converted, Mapped, ?ENV, 1567),
    wxe_util:rec(1567).

enable(This, Index) when is_record(This, wx_ref), is_integer(Index) ->
    enable(This, Index, []);
enable(#wx_ref{type=Type}=This, Options) when is_list(Options) ->
    (Type =:= wxRadioBox) orelse Type:parent_class(wxRadioBox),
    Mapper = fun({enable, _} = Arg) -> Arg;
                (Bad) -> erlang:error({badoption, Bad}) end,
    Mapped = lists:map(Mapper, Options),
    wxe_util:queue_cmd(This, Mapped, ?ENV, 2118),
    wxe_util:rec(2118).

enable(This, Index, Options) -> {This, Index, Options}.

run(#wx_ref{type=Type}=This, Index, Label, Caption, {X,Y} = Pos, Choices, Options)
  when is_integer(Index), (is_list(Label) orelse is_binary(Label)),
       (is_list(Caption) orelse is_binary(Caption)),
       is_integer(X), is_integer(Y), is_list(Choices), is_list(Options) ->
    (Type =:= wxChoice) orelse Type:parent_class(wxChoice),
    Binary = unicode:characters_to_binary(Label),
    CaptionBinary = unicode:characters_to_binary(Caption),
    Converted = [unicode:characters_to_binary(Item) || Item <- Choices],
    Mapper = fun({style, _} = Arg) -> Arg;
                (Bad) -> erlang:error({badoption, Bad}) end,
    Mapped = lists:map(Mapper, Options),
    wxe_util:queue_cmd(This, Index, Binary, CaptionBinary, Pos, Converted, Mapped, ?ENV, 1567),
    wxe_util:rec(1567).

set_status(This, Status) when is_record(This, wx_ref), is_record(Status, wx_ref) ->
    set_status(This, Status, []);
set_status(#wx_ref{type=Type}=This, Index) when is_integer(Index) ->
    (Type =:= wxHtmlWindow) orelse Type:parent_class(wxHtmlWindow),
    wxe_util:queue_cmd(This, Index, ?ENV, 3465).

set_status(This, Status, Options) -> {This, Status, Options}.

show(This, Show) when is_record(This, wx_ref), is_boolean(Show) ->
    show(This, Show, []).

show(This, Show, Options) -> {This, Show, Options}.

init(This, {Width, Height} = Area)
  when is_record(This, wx_ref), is_integer(Width), is_integer(Height) ->
    init(This, Area, []);
init(#wx_ref{type=Type}=This, Options) when is_list(Options) ->
    (Type =:= wxDC) orelse Type:parent_class(wxDC),
    Mapper = fun({style, _} = Arg) -> Arg;
                (Bad) -> erlang:error({badoption, Bad}) end,
    Mapped = lists:map(Mapper, Options),
    wxe_util:queue_cmd(This, Mapped, ?ENV, 737),
    wxe_util:rec(737).

init(This, Area, Options) -> {This, Area, Options}.

font(This, Size, Face) when is_record(This, wx_ref), is_number(Size),
                           (is_list(Face) orelse is_binary(Face)) ->
    font(This, Size, Face, []);
font(#wx_ref{type=ThisType}=This, #wx_ref{type=FontType}=Font, Options)
  when is_list(Options) ->
    (ThisType =:= wxGraphicsRenderer) orelse ThisType:parent_class(wxGraphicsRenderer),
    (FontType =:= wxFont) orelse FontType:parent_class(wxFont),
    Mapper = fun({col, Colour}) -> {col, wxe_util:color(Colour)};
                (Bad) -> erlang:error({badoption, Bad}) end,
    Mapped = lists:map(Mapper, Options),
    wxe_util:queue_cmd(This, Font, Mapped, ?ENV, 837),
    wxe_util:rec(837).

font(This, Size, Face, Options) -> {This, Size, Face, Options}.
