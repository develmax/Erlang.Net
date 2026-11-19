-module(beam_guard_band).
-export([match/1]).

match(Options) ->
    case proplists:get_value(socket, Options) of
        {checked, Port} when (Port band -65536) =:= 0 ->
            accepted;
        Other ->
            {rejected, Other}
    end.
