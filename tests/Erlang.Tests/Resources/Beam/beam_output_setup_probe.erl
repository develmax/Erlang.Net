-module(beam_output_setup_probe).
-export([start/2]).
-record(state, {socket, handle, watermark}).

start(Socket, Handle) ->
    try
        beam_output_transport:notify(Handle),
        {ok, Size} = beam_output_transport:getopt(Socket, {socket, sndbuf}),
        State = #state{socket = Socket, handle = Handle, watermark = Size bsr 1},
        Buffer = [],
        Count = 0,
        Pending = false,
        Selection = undefined,
        run(State, Buffer, Count, Pending, Selection)
    catch
        Class:Reason:Stack when Class =:= error ->
            beam_output_transport:report(
                [output_exception, {class, Class}, {reason, Reason}, {stacktrace, Stack}]),
            erlang:raise(Class, Reason, Stack)
    end.

run(State, _Buffer, _Count, _Pending, _Selection) ->
    erlang:error({stopped, State}).
