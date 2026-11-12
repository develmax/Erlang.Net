-module(beam_listener_start_probe).
-export([start/4]).

-record(config, {options, transport}).

start(IP, Requested, Owner, #config{transport = {Transport, _, _, _, _}, options = Options} = Config) ->
    InetOptions = Options ++ defaults(),
    case Transport:open(Requested, InetOptions) of
        {ok, Socket} ->
            Port = case Requested of
                       0 ->
                           {ok, Actual} = beam_listener_transport:port(Socket),
                           Actual;
                       _ ->
                           Requested
                   end,
            finish(Config, IP, Port, Socket, Owner);
        {error, eaddrinuse} ->
            {error, already_listening};
        Error ->
            Error
    end.

defaults() ->
    beam_listener_transport:defaults().

finish(Config, IP, Port, Socket, Owner) ->
    {Config, IP, Port, Socket, Owner}.
