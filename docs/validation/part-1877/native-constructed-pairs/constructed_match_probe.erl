-module(constructed_match_probe).
-export([dynamic/2, unknown/2, widen/2, fail/3]).

dynamic(N,V) ->
    <<W:N>> = <<V:N>>,
    W.

unknown(N,B) ->
    <<W:N>> = B,
    W.

widen(N,V) ->
    Size = N+8,
    <<W:Size>> = <<V,0:N>>,
    W.

fail(N,V,M) ->
    <<W:M>> = <<V:N>>,
    W.