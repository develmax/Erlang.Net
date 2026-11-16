-module(beam_discarded_match).
-export([run/2, value/1]).
run(X, Flag) ->
    _ = case Flag of
        yes -> beam_probe:pass(X);
        no -> ok
    end,
    {ok,X}.
value(X) -> _ = beam_probe:pass(X).
