-module(beam_socket_accept_probe).
-export([accept_open/2]).

accept_open(Address, Listener) ->
    case beam_accept_transport:accept(Listener) of
        {ok, Socket} ->
            maybe
                {ok, #{addr := Ip}} ?=
                    beam_accept_transport:sockname(Socket),
                {ok, #{addr := PeerIp, port := PeerPort}} ?=
                    beam_accept_transport:peername(Socket),
                beam_accept_transport:check_ip(Ip, PeerIp),
                handshake(Address, Socket, PeerIp, PeerPort)
            else
                {error, Reason} ->
                    _ = beam_accept_transport:close(Socket),
                    exit({accept_open, Reason})
            end;
        {error, Reason} ->
            exit({accept_open, Reason})
    end.

handshake(Address, Socket, PeerIp, PeerPort) ->
    {Address, Socket, PeerIp, PeerPort}.
