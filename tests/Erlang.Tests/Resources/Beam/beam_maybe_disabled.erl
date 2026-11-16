-module(beam_maybe_disabled).
-feature(maybe_expr, disable).
-export([maybe/0]).
maybe() -> else.
