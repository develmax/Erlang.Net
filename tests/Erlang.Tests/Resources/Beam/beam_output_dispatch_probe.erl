-module(beam_output_dispatch_probe).
-export([dispatch/5]).
-record(state, {socket, handle, watermark}).

dispatch(State, Buffer, Size, Pending, Selection) ->
    if
        Pending, State#state.watermark > Size ->
            data(State, Buffer, Size, Selection);
        Selection =:= undefined, Size > 0 ->
            send(State, Buffer, Size, Pending);
        true ->
            wait(State, Buffer, Size, Pending, Selection, infinity)
    end.

data(State, Buffer, Size, Selection) -> {data, State, Buffer, Size, Selection}.
send(State, Buffer, Size, Pending) -> {send, State, Buffer, Size, Pending}.
wait(State, Buffer, Size, Pending, Selection, _Timeout) -> {wait, State, Buffer, Size, Pending, Selection}.
