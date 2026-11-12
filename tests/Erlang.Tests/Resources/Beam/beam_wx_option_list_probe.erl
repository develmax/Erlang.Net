-module(beam_wx_option_list_probe).
-export([run/2]).
-record(wx_env, {ref, sv, debug}).
-define(ENV, case erlang:get(wx_env) of
    #wx_env{ref=Ref} -> Ref;
    _ -> erlang:error({wx, unknown_env})
end).
run(Text, Options) when (is_list(Text) orelse is_binary(Text)), is_list(Options) ->
    Binary = unicode:characters_to_binary(Text),
    Mapper = fun({client, Client}) -> Converted = unicode:characters_to_binary([Client, $_, $C]), {client, Converted};
                ({choices, Values}) ->
                    Converted = [unicode:characters_to_binary(Item) || Item <- Values],
                    {choices, Converted};
                ({size, {_, _}} = Arg) -> Arg;
                (Bad) -> erlang:error({badoption, Bad}) end,
    Mapped = lists:map(Mapper, Options),
    wxe_util:queue_cmd(Binary, Mapped, ?ENV, 3374),
    wxe_util:rec(3374).
