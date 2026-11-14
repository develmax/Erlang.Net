-module(beam_receive_hibernate).
-export([sleep/1, wake/1]).
sleep(Receiver) ->
    receive
    after 5 ->
        erlang:hibernate(?MODULE, wake, [Receiver])
    end.
wake(Value) ->
    Value.
