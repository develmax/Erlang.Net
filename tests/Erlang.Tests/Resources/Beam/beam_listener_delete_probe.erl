-module(beam_listener_delete_probe).
-export([listen_open/2, accept_controller/3]).
-record(address, {family}).

listen_open(#address{family = Family}, Options) ->
    maybe
        Key = backlog,
        Default = 128,
        Backlog = proplists:get_value(Key, Options, Default),
        {ok, Socket} ?= beam_listener_transport:open(Family, stream),
        ok ?= setopts(Socket, [beam_listener_transport:nodelay() |
                              proplists:delete(Key, proplists:delete(nodelay, Options))]),
        {ok, {Socket, Backlog}}
    else
        {error, _} = Error -> Error
    end.

setopts(Socket, Options) ->
    beam_listener_transport:setopts(Socket, Options).

accept_controller(_Address, Controller, Socket) ->
    maybe
        ok ?= beam_listener_transport:setopt(Socket, {otp, controlling_process}, Controller),
        Socket
    else
        {error, Reason} -> exit({accept_controller, Reason})
    end.
