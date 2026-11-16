-module(beam_linked_record_closures_probe).
-export([start/2]).

-record(socket, {unused, controller, channel}).
-record(handshake, {controller, send, address, ready, mode = normal}).

start(Address, #socket{controller = Controller} = Socket) ->
    link(Controller),
    #handshake{
        controller = Controller,
        ready = fun(Ctrl) when Ctrl =:= Controller -> ok end,
        address = fun(Ctrl) when Ctrl =:= Controller -> {address, Address} end,
        send = fun(Ctrl) when Ctrl =:= Controller -> {socket, Socket} end
    }.
