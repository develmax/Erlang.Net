-module(beam_wx_unicode_list_probe).
-export([run/4, mapper/0, set/3]).
-record(wx_ref, {ref, type, state}).
-record(wx_env, {ref, sv, debug}).
-define(ENV, case erlang:get(wx_env) of
    #wx_env{ref=Ref} -> Ref;
    _ -> erlang:error({wx, unknown_env})
end).
run(#wx_ref{type=Type}=This, Text, Pos, Data)
  when is_list(Text), is_integer(Pos) ->
    (Type =:= wxControlWithItems) orelse Type:parent_class(wxControlWithItems),
    Binary = [unicode:characters_to_binary(Item) ||
              Item <- Text],
    wxe_util:queue_cmd(This, Binary, Pos, Data, ?ENV, 898),
    wxe_util:rec(898).
mapper() -> fun(Value) -> Value end.
set(#wx_ref{type=Type}=This, Index, Text)
  when is_integer(Index), (is_list(Text) orelse is_binary(Text)) ->
    (Type =:= wxControlWithItems) orelse Type:parent_class(wxControlWithItems),
    Binary = unicode:characters_to_binary(Text),
    wxe_util:queue_cmd(This, Index, Binary, ?ENV, 900).