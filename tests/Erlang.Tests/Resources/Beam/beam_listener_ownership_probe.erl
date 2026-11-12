-module(beam_listener_ownership_probe).
-export([start/5, create_socket/3, default_ip/1]).

-record(config, {ssl, emulated, transport}).
-record(socket, {handle, table, config}).

start(#config{transport = {Transport, _, _, _, _} = Info, emulated = Options, ssl = Ssl} = Config,
      IP, Port, Socket, Owner) ->
    Emulated = prepare(Options, []),
    case beam_listener_stub:start_child([Owner, Port, Info, Emulated, Ssl, Socket]) of
        {ok, Listener} ->
            ok = Transport:controlling_process(Socket, Listener),
            beam_listener_stub:register_listener({self(), Listener}, IP, Port),
            Result = create_socket(Config, Listener, Port),
            {ok, Result};
        Error ->
            Error
    end.

create_socket(#config{emulated = Options} = Config, Listener, Port) ->
    Socket = #socket{handle = {Listener, Port}, table = fixture_table, config = Config},
    check(Options, Socket),
    Socket.

prepare(Options, Defaults) -> beam_listener_stub:prepare(Options, Defaults).
check(Options, Socket) -> beam_listener_stub:check(Options, Socket).

default_ip(Options) ->
    case proplists:get_value(inet6, Options, false) of
        false -> {0,0,0,0};
        true -> {0,0,0,0,0,0,0,0}
    end.
