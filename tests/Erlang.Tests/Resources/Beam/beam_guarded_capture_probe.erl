-module(beam_guarded_capture_probe).
-export([callbacks/1, callback/1]).
callbacks(Controller) ->
    {fun(Ctrl) when Ctrl =:= Controller -> ok end,
     fun(Ctrl) when Ctrl =:= Controller -> {ok, Controller} end,
     fun(Ctrl) when Ctrl =:= Controller -> Controller end}.

callback(Controller) ->
    fun(Value, Ctrl) when Ctrl =:= Controller -> {Value, Controller} end.
