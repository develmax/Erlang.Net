-module(beam_case_saved_value).
-export([run/2]).
run(X,Y) ->
    A = beam_probe:pass(X),
    B = beam_probe:select(Y),
    case B of
        yes -> {A,B};
        no -> {B,A}
    end.
