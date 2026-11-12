-module(beam_probe).
-export([answer/0, ready/0, negative/0]).
answer() -> 42.
ready() -> ready.
negative() -> -32769.
