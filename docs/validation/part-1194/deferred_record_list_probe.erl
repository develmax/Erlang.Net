-module(deferred_record_list_probe).
-export([event/2]).
-record(event, {name,node,data}).
event(#event{name=Name,node=Node,data=Data}, State) ->
    note(header, []),
    note(event, [Name,Node,Data]),
    {ok,State}.
note(Key, Value) -> erlang:put(Key, Value).
