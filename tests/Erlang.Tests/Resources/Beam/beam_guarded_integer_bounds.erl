-module(beam_guarded_integer_bounds).
-export([lt/2, gt/2]).
lt(X, Default) ->
    case other:read(X) of
        V when is_integer(V) andalso V < 3 -> V;
        _ -> Default
    end.
gt(X, Default) ->
    case other:read(X) of
        V when is_integer(V) andalso V > -3 -> V;
        _ -> Default
    end.
