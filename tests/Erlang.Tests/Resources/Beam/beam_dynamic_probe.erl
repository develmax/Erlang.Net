-module(beam_dynamic_probe).
-export([dynamic/3, ordinary/3, module/2, function/2]).
dynamic(M,F,X) -> M:F(X).
ordinary(M,F,X) ->
    M:F(X),
    X.
module(M,X) -> M:reverse(X).
function(F,X) -> lists:F(X).
