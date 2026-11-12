-module(beam_wx_guarded_forward_probe).
-export([new/2, new/3, text_options/5, cursor/1, cursor/2]).
-record(wx_ref, {ref, type, state}).
-record(wx_env, {ref, sv, debug}).
-define(CLASS(Type, Class), (Type =:= Class) orelse Type:parent_class(Class)).
-define(ENV, case erlang:get(wx_env) of
    #wx_env{ref=Ref} -> Ref;
    _ -> erlang:error({wx, unknown_env})
end).

new(Orient, Parent) when is_integer(Orient), is_record(Parent, wx_ref) ->
    new(Orient, Parent, []);
new(#wx_ref{type=Type}=Box, Orient) when is_integer(Orient) ->
    ?CLASS(Type, wxStaticBox),
    wxe_util:queue_cmd(Box, Orient, ?ENV, 1380),
    wxe_util:rec(1380).

new(Orient, Parent, Options) ->
    {Orient, Parent, Options}.

text_options(#wx_ref{type=Type}=This, Text, Size, Bold, Options)
  when (is_list(Text) orelse is_binary(Text)), is_integer(Size), is_boolean(Bold), is_list(Options) ->
    ?CLASS(Type, wxStyledTextCtrl),
    Binary = unicode:characters_to_binary(Text),
    Mapper = fun({encoding, _} = Arg) -> Arg;
                (Bad) -> erlang:error({badoption, Bad}) end,
    Mapped = lists:map(Mapper, Options),
    wxe_util:queue_cmd(This, Binary, Size, Bold, Mapped, ?ENV, 3347).

cursor(Name) when is_list(Name) orelse is_binary(Name) ->
    cursor(Name, []);
cursor(#wx_ref{type=Type}=Image) ->
    IsImage = try ?CLASS(Type, example_image) catch _:_ -> false end,
    IsCursor = try ?CLASS(Type, example_cursor) catch _:_ -> false end,
    Target = if
        IsImage -> example_image;
        IsCursor -> example_cursor;
        true -> erlang:error({badarg, Type})
    end,
    wxe_util:queue_cmd(wx:typeCast(Image, Target), ?ENV, 1073),
    wxe_util:rec(1073);
cursor(Id) when is_integer(Id) ->
    wxe_util:queue_cmd(Id, ?ENV, 1072),
    wxe_util:rec(1072).

cursor(Name, Options) ->
    {Name, Options}.
