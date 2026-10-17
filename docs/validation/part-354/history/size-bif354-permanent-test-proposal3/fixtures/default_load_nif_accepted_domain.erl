-module(default_load_nif_accepted_domain).
-export([run/0,check/1]).

run() ->
    [done = default_load_nif_accepted_domain:check(Value) ||
        Value <- [[],[[],[97,[16#1f642]]],<<0>>,<<255,0,254>>]],
    done.

check(Value) ->
    case erlang:load_nif(Value,[]) of
        {error,{_,_}} -> done;
        Other -> {unexpected_library_result,Other}
    end.
