-module(beam_plain_socket_connect_probe).
-export([connect/3]).
-record(address, {peer, family}).

connect(#address{peer = {Ip, Port}, family = Family} = Address, _Timer, Options) ->
    maybe
        {ok, Socket} ?= beam_plain_transport:open(Family, stream),
        ok ?= setopts(Socket, Options),
        Peer = #{family => Family, addr => Ip, port => Port},
        ok ?= beam_plain_transport:connect(Socket, Peer),
        start(Address, Socket)
    else
        {error, _} = Error -> Error
    end.

setopts(Socket, Options) ->
    beam_plain_transport:setopts(Socket, Options).

start(Address, Socket) ->
    {Address, Socket}.
