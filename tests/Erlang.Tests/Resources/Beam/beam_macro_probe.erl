-module(beam_macro_probe).
-define(BASE, 42).
-define(ANSWER, ?BASE).
-export([answer/0, name/0, line/0]).
answer() -> ?ANSWER.
name() -> ?MODULE.
line() -> ?LINE.
