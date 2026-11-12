-module(beam_guarded_filter_probe).
-export([callback/2]).
callback(Socket, Controller) ->
    fun(Ctrl, Opts) when Ctrl =:= Controller ->
        case filter(Opts) of
            [] -> beam_filter_transport:setopts(Socket, Opts);
            Bad -> {error, {badopts, Bad}}
        end
    end.

filter([]) -> [];
filter(Opts) -> [Opts].
