-module(beam_remote_peer387).
-export([hold/2]).

hold(Owner, Case) ->
    Owner ! {peer_ready, self(), Case},
    receive
        {release, Case} -> released(Case)
    end.

released(normal) -> {remote_returned, [first, second]};
released(error) -> erlang:error({remote_error, #{detail => preserved}});
released(throw) -> erlang:throw({remote_throw, [first, second]});
released(exit) -> erlang:exit({remote_exit, {nested, [first, second]}}).
