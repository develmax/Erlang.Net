-module(beam_conditional_probe).
-define(ANSWER, 41).
-ifdef(ANSWER).
-undef(ANSWER).
-else.
-define(ANSWER, ?UNDEFINED).
-endif.
-ifndef(ANSWER).
-define(ANSWER, 42).
-ifdef(MISSING).
-if(?UNDEFINED).
-define(ANSWER, invalid).
-endif.
-else.
-export([answer/0, line/0]).
-endif.
-endif.
answer() -> ?ANSWER.
line() -> ?LINE.
