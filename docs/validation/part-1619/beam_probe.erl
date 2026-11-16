-module(beam_probe).
-export([pass/2,record/4]).
pass(prefix, Value) -> put(trace,[pass]), Value.
record(Key,1,Items,{Key,Value}) ->
    put(trace,[record]),
    erlang:demonitor(Value,[flush]),
    [Key|Items].
