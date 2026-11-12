-module(beam_automatic_inline).
-compile(inline).
-export([run/1]).
run(X) -> {helper(X), literal()}.
helper(X) -> X.
literal() -> ok.
