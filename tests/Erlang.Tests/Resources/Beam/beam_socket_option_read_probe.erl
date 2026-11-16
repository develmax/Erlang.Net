-module(beam_socket_option_read_probe).
-export([getopts/3, getstat/3]).

-record(socket, {handle, config}).
-record(config, {options}).

getopts(_, #socket{handle = {Listener, _}, config = #config{}}, Options) ->
    Split = beam_option_transport:split_options(gen_udp, Options),
    beam_option_transport:get_sock_opts(Listener, Split);
getopts(gen_udp, {_, {{_, _}, Socket}}, Options) ->
    beam_option_transport:getopts(Socket, Options);
getopts(gen_udp, {_, Socket}, Options) ->
    beam_option_transport:getopts(Socket, Options);
getopts(Transport, Socket, Options) ->
    Transport:getopts(Socket, Options).

getstat(gen_udp, Pid, Options) when is_pid(Pid) ->
    beam_option_transport:listener_stats(Pid, Options);
getstat(gen_udp, {_, {_, Socket}}, Options) ->
    beam_option_transport:socket_stats(Socket, Options);
getstat(gen_udp, {_, Socket}, Options) ->
    beam_option_transport:socket_stats(Socket, Options);
getstat(gen_udp, Socket, Options) ->
    beam_option_transport:socket_stats(Socket, Options);
getstat(Transport, Socket, Options) ->
    Transport:getstat(Socket, Options).
