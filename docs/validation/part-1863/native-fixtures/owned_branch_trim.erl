-module(owned_branch_trim).
-export([run/2]).

run(Key, Channel) ->
    case lookup(Key) of
        {ok, OptionKey} ->
            case lookup(OptionKey) of
                ok -> {ok, Channel};
                Error1 -> close(Channel), Error1
            end;
        Error2 ->
            close(Channel), Error2
    end.

lookup(Key) ->
    erlang:put(last_lookup, Key),
    erlang:get(Key).

close(failing_channel) ->
    erlang:error(close_failed);
close(Channel) ->
    erlang:put(last_closed, Channel),
    ok.
