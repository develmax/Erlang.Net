-module(beam_wx_middle_text_probe).
-export([run/4, tuple_text/3, tuple_options/4, text_pair/3, text_pair/4]).
-record(wx_ref, {ref, type, state}).
-record(wx_env, {ref, sv, debug}).
-define(ENV, case erlang:get(wx_env) of
    #wx_env{ref=Ref} -> Ref;
    _ -> erlang:error({wx, unknown_env})
end).
run(#wx_ref{type=Type}=This, Text, Pos, Data)
  when (is_list(Text) orelse is_binary(Text)), is_integer(Pos) ->
    (Type =:= wxControlWithItems) orelse Type:parent_class(wxControlWithItems),
    Binary = unicode:characters_to_binary(Text),
    wxe_util:queue_cmd(This, Binary, Pos, Data, ?ENV, 898),
    wxe_util:rec(898).

tuple_text(#wx_ref{type=Type}=This, Text, {X,Y}=Pos)
  when (is_list(Text) orelse is_binary(Text)), is_integer(X), is_integer(Y) ->
    (Type =:= wxDC) orelse Type:parent_class(wxDC),
    Binary = unicode:characters_to_binary(Text),
    wxe_util:queue_cmd(This, Binary, Pos, ?ENV, 655).

tuple_options(#wx_ref{type=Type}=This, Text, {X,Y}=Pos, Options)
  when (is_list(Text) orelse is_binary(Text)), is_integer(X), is_integer(Y), is_list(Options) ->
    (Type =:= wxDC) orelse Type:parent_class(wxDC),
    Binary = unicode:characters_to_binary(Text),
    Mapper = fun({style, _} = Arg) -> Arg;
                (Bad) -> erlang:error({badoption, Bad}) end,
    Mapped = lists:map(Mapper, Options),
    wxe_util:queue_cmd(This, Binary, Pos, Mapped, ?ENV, 641).

text_pair(#wx_ref{type=Type}=This, Domain, Language, Charset)
  when (is_list(Domain) orelse is_binary(Domain)), is_integer(Language),
       (is_list(Charset) orelse is_binary(Charset)) ->
    (Type =:= wxLocale) orelse Type:parent_class(wxLocale),
    DomainBinary = unicode:characters_to_binary(Domain),
    CharsetBinary = unicode:characters_to_binary(Charset),
    wxe_util:queue_cmd(This, DomainBinary, Language, CharsetBinary, ?ENV, 3513),
    wxe_util:rec(3513).

text_pair(#wx_ref{type=Type}=This, Menu, Item)
  when (is_list(Menu) orelse is_binary(Menu)), (is_list(Item) orelse is_binary(Item)) ->
    (Type =:= wxMenuBar) orelse Type:parent_class(wxMenuBar),
    MenuBinary = unicode:characters_to_binary(Menu),
    ItemBinary = unicode:characters_to_binary(Item),
    wxe_util:queue_cmd(This, MenuBinary, ItemBinary, ?ENV, 861),
    wxe_util:rec(861).
