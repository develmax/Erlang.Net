-module(beam_general_catch_probe).
-export([run/1, select/4]).
run({M,F,X}) -> catch M:F(X).
select(M,F,X,Saved) ->
    case catch M:F(X) of
        4 -> Saved;
        _ -> failed
    end.

