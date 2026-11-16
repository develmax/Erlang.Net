-module(beam_try_after_one_line).
-export([run/0]).
run() -> try probe:body() after probe:cleanup() end.
