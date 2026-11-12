-module(bound_tuple_probe).
-export([reply/2]).
reply(_, State) ->
    Reply = ok,
    Status = Reply,
    {ok, Status, State}.
