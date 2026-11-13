-module(beam_deferred_record_call).
-export([run/3]).
-record(state, {items}).
run({request,Key,Input}, _Unused, #state{items=Items}=State) ->
    First = erlang:monitor(process,Input),
    Next = beam_probe:record(Key,1,Items,{Key,First}),
    {reply,ok,State#state{items=Next}}.
