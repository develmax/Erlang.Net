-module(beam_catch_probe).
-export([direct/1, local/1, constant/0, nested/1]).
direct(X) -> catch erlang:throw(X).
local(X) -> catch helper(X).
constant() -> catch ok.
nested(X) -> {catch helper(X), X}.
helper(X) -> X.
