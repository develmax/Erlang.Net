-module(beam_record_transport_probe).
-export([connect/4]).

-record(config, {ssl, emulated, inet_ssl, transport_info}).

connect(Host, Port, #config{transport_info = Callback, ssl = SslOptions,
                            emulated = Emulated, inet_ssl = SocketOptions}, Timeout) ->
    Transport = element(1, Callback),
    case Transport:open(0, SocketOptions ++ default_options()) of
        {ok, Socket} ->
            beam_transport_fsm:start_fsm(client, Host, Port, {{Host, Port}, Socket},
                                         {SslOptions, Emulated, undefined}, self(), Callback, Timeout);
        {error, _} = Error ->
            Error
    end.

default_options() ->
    [binary].
