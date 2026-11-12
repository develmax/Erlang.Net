-module(beam_guarded_call_probe).
-export([callbacks/2]).
callbacks(Socket, Controller) ->
    {fun(Ctrl, Packet) when Ctrl =:= Controller ->
         beam_callback_transport:send(Socket, Packet)
     end,
     fun(Ctrl, Node, Handle) when Ctrl =:= Controller ->
         beam_callback_transport:complete(Controller, Node, Handle)
     end,
     fun(Ctrl) when Ctrl =:= Controller ->
         Controller ! tick
     end}.
