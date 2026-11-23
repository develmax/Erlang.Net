-module(beam_integer_guard_type).
-export([nonnegative/1, nonpositive/1, independent/2]).

nonnegative(Value) when is_integer(Value) andalso Value >= 0 -> valid.
nonpositive(Value) when erlang:is_integer(Value) andalso Value =< 0 -> valid.
independent(Value, Other) when is_integer(Value) andalso Other >= 0 -> valid.
