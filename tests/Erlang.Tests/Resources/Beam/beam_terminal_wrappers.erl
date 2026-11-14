-module(beam_terminal_wrappers).
-export([fail/1, stop/1, raise_throw/1, fail_args/2]).
fail(Reason) -> error(Reason).
stop(Reason) -> exit(Reason).
raise_throw(Reason) -> throw(Reason).
fail_args(Reason, Args) -> erlang:error(Reason, Args).
