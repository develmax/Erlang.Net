%% SPDX-License-Identifier: Apache-2.0
%% Adapted from OTP 29.1.1 dtls_socket.erl.
%% Copyright Ericsson AB 2013-2026. All Rights Reserved.
-module(beam_socket_address_probe).
-export([peername/2, sockname/2, port/2]).

peername(_, undefined) ->
    {error, enotconn};
peername(gen_udp, {_, {Client, _Socket}}) ->
    {ok, Client};
peername(gen_udp, {Client, _Socket}) ->
    {ok, Client};
peername(Transport, Socket) ->
    Transport:peername(Socket).
sockname(gen_udp, {Listener, {_Client, Socket}}) when is_pid(Listener) ->
    beam_address_transport:sockname(Socket);
sockname(gen_udp, {_Client, Socket}) ->
    beam_address_transport:sockname(Socket);
sockname(gen_udp, Socket) ->
    beam_address_transport:sockname(Socket);
sockname(Transport, Socket) ->
    Transport:sockname(Socket).

port(gen_udp, {_,Socket}) ->
    beam_address_transport:port(Socket);
port(Transport, Socket) ->
    Transport:port(Socket).
