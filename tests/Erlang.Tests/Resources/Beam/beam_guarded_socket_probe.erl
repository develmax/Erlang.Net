-module(beam_guarded_socket_probe).
-export([socket/5, setopts/3]).

-record(sslsocket, {socket_handle, connection_handler, payload_sender,
                    transport_cb, connection_cb, tab, listener_config}).
-record(config, {options}).

socket([Pid], gen_udp = Transport,
       PeerAndSock = {{_Host, _Port}, _Socket}, ConnectionCb, Tab) when Tab =/= undefined ->
    #sslsocket{socket_handle = PeerAndSock, connection_handler = Pid,
               payload_sender = Pid, transport_cb = Transport,
               connection_cb = ConnectionCb, tab = Tab};
socket([Pid], Transport, Socket, ConnectionCb, Tab) when Tab =/= undefined ->
    #sslsocket{socket_handle = Socket, connection_handler = Pid,
               payload_sender = Pid, transport_cb = Transport,
               connection_cb = ConnectionCb, tab = Tab}.

setopts(_, Socket = #sslsocket{socket_handle = {ListenPid, _}, listener_config = #config{}}, Options) ->
    SplitOpts = {_, EmOpts} = beam_option_transport:split_options(gen_udp, Options),
    check_active(EmOpts, Socket),
    beam_option_transport:set_sock_opts(ListenPid, SplitOpts);
setopts(gen_udp, Socket, Options) ->
    beam_option_transport:setopts(Socket, Options);
setopts(Transport, Socket, Options) ->
    Transport:setopts(Socket, Options).

check_active(EmOpts, Socket) ->
    beam_option_transport:check_active(EmOpts, Socket).
