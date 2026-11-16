-module(beam_wrapped_record_update).
-export([run/3, single/2]).
-record(state, {items, count}).
run(#state{}=S, Value, Flag) ->
    case Flag of
        true -> {ok,S#state{items=Value}};
        false -> {error,S#state{items=Value,count=0}}
    end.
single(#state{}=S, X) ->
    {ok,S#state{items=beam_probe:pass(X)}}.
