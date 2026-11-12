-module(wx_text_command_probe).
-export([one/2, two/3, set/2, overload/2]).

one(Text, Options) when (is_list(Text) orelse is_binary(Text)), is_list(Options) ->
    Binary = unicode:characters_to_binary(Text),
    Mapper = fun({size, _} = Value) -> Value;
                (Bad) -> erlang:error({badoption, Bad})
             end,
    Mapped = lists:map(Mapper, Options),
    wxe_util:queue_cmd(Binary, Mapped, case erlang:get(wx_env) of
        {wx_env, Ref, _, _} -> Ref;
        _ -> erlang:error({wx, unknown_env})
    end, 900),
    wxe_util:rec(900).

two(Title, Text, Options)
  when (is_list(Title) orelse is_binary(Title)),
       (is_list(Text) orelse is_binary(Text)), is_list(Options) ->
    First = unicode:characters_to_binary(Title),
    Second = unicode:characters_to_binary(Text),
    Mapper = fun({size, _} = Value) -> Value;
                (Bad) -> erlang:error({badoption, Bad})
             end,
    Mapped = lists:map(Mapper, Options),
    wxe_util:queue_cmd(First, Second, Mapped, case erlang:get(wx_env) of
        {wx_env, Ref, _, _} -> Ref;
        _ -> erlang:error({wx, unknown_env})
    end, 901),
    wxe_util:rec(901).

set(Name, Value) when (is_list(Name) orelse is_binary(Name)), is_integer(Value) ->
    First = unicode:characters_to_binary(Name),
    wxe_util:queue_cmd(First, Value, case erlang:get(wx_env) of
        {wx_env, Ref, _, _} -> Ref;
        _ -> erlang:error({wx, unknown_env})
    end, 902);
set(Name, Value)
  when (is_list(Name) orelse is_binary(Name)), (is_list(Value) orelse is_binary(Value)) ->
    First = unicode:characters_to_binary(Name),
    Second = unicode:characters_to_binary(Value),
    wxe_util:queue_cmd(First, Second, case erlang:get(wx_env) of
        {wx_env, Ref, _, _} -> Ref;
        _ -> erlang:error({wx, unknown_env})
    end, 903).

overload({wx_ref, _, ParentType, _} = Parent, Options) when is_list(Options) ->
    ParentType =:= wxWindow orelse ParentType:parent_class(wxWindow),
    Mapper = fun({data, {wx_ref, _, Type, _}} = Value) ->
                    Type =:= wxPrintDialogData orelse Type:parent_class(wxPrintDialogData),
                    Value;
                (Bad) -> erlang:error({badoption, Bad})
             end,
    Mapped = lists:map(Mapper, Options),
    wxe_util:queue_cmd(Parent, Mapped, case erlang:get(wx_env) of
        {wx_env, Ref, _, _} -> Ref;
        _ -> erlang:error({wx, unknown_env})
    end, 904),
    wxe_util:rec(904);
overload({wx_ref, _, ParentType, _} = Parent, {wx_ref, _, DataType, _} = Data) ->
    ParentType =:= wxWindow orelse ParentType:parent_class(wxWindow),
    DataType =:= wxPrintData orelse DataType:parent_class(wxPrintData),
    wxe_util:queue_cmd(Parent, Data, case erlang:get(wx_env) of
        {wx_env, Ref, _, _} -> Ref;
        _ -> erlang:error({wx, unknown_env})
    end, 905),
    wxe_util:rec(905).
