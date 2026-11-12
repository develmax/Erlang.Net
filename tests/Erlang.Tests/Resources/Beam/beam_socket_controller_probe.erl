-module(beam_socket_controller_probe).
-export([accept_controller/3]).

accept_controller(_Address, Controller, Socket) ->
    maybe
        ok ?= beam_controller_transport:controlling_process(Socket, Controller),
        Socket
    else
        {error, Reason} ->
            exit({accept_controller, Reason})
    end.
