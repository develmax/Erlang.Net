-module(beam_maybe_socket_connect_probe).
-export([connect/3]).

-record(address, {peer, host, family}).

connect(#address{peer = {Ip, Port}, family = Family, host = Host} = NetAddress, _Timer, Options) ->
    maybe
        {ok, Socket} ?=
            beam_connect_transport:open(Family, stream),
        ok ?=
            setopts(
              Socket,
              beam_connect_transport:merge(
                Options, [{nodelay, true}], [])),
        BindAddress =
            #{family => Family, addr => any,
              port => proplists:get_value(port, Options, 0)},
        ok ?=
            beam_connect_transport:bind(Socket, BindAddress),
        ConnectAddress =
            #{family => Family, addr => Ip, port => Port},
        ok ?=
            beam_connect_transport:connect(Socket, ConnectAddress),
        SslOptions = beam_connect_transport:defaults(),
        {ok, SslSocket} ?=
            beam_connect_transport:secure(
              Socket,
              beam_connect_transport:merge(
                SslOptions,
                [Family, binary, {active, false}, {packet, 4}, {server_name_indication, Host}],
                []),
              beam_connect_transport:timeout()),
        start(NetAddress, SslSocket)
    else
        {error, _} = Error ->
            Error
    end.

setopts(Socket, Options) ->
    beam_connect_transport:setopts(Socket, Options).

start(Address, Socket) ->
    {Address, Socket}.
