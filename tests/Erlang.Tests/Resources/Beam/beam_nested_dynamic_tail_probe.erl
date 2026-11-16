-module(beam_nested_dynamic_tail_probe).
-export([send/3, accept/3]).

-record(config, {options, transport}).

send(Transport, {{IP, Port}, Socket}, Data) ->
    Transport:send(Socket, IP, Port, Data).

accept({Listener, _}, #config{}, _Timeout) ->
    case beam_fixture_transport:accept(Listener, self()) of
        {ok, Pid} ->
            receive
                {Pid, user_socket, UserSocket} -> {ok, UserSocket}
            end;
        {error, Reason} ->
            {error, Reason}
    end.
