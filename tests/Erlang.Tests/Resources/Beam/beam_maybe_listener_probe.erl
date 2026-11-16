-module(beam_maybe_listener_probe).
-export([listen_open/2]).

-record(address, {family}).

listen_open(#address{family = Family}, Options) ->
    maybe
        Key = backlog,
        Default = 128,
        Backlog = proplists:get_value(Key, Options, Default),
        {ok, Listener} ?=
            beam_maybe_transport:open(Family, stream),
        ok ?=
            setopts(
              Listener,
              beam_maybe_transport:merge(
                Options, [{nodelay, true}], [])),
        {ok, {Listener, Backlog}}
    else
        {error, _} = Error ->
            Error
    end.

setopts(Listener, Options) ->
    beam_maybe_transport:setopts(Listener, Options).
