-module(beam_shared_receive_tail).
-export([drain/1, guarded/1, payload/1, repeated/1]).

drain(Key) ->
    receive
        {packet, Key, _Ignored} -> drain(Key);
        {stopped, Key} -> drain(Key);
        {failed, Key} -> drain(Key)
    after 0 ->
        drained
    end.

guarded(Key) ->
    receive
        {packet, Key, Data} when Data =:= wanted -> guarded(Key);
        {stopped, Key} -> guarded(Key);
        {failed, Key} -> guarded(Key)
    after 0 ->
        guarded_timeout
    end.

payload(Key) ->
    receive
        {packet, Key, Data} -> Data;
        {stopped, Key} -> payload(Key);
        {failed, Key} -> payload(Key)
    after 0 ->
        payload_timeout
    end.

repeated(Key) ->
    receive
        {packet, Key, _Same, _Same} -> repeated(Key);
        {stopped, Key} -> repeated(Key);
        {failed, Key} -> repeated(Key)
    after 0 ->
        repeated_timeout
    end.
