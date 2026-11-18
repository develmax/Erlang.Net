-module(beam_implicit_line_file).
-export([first/0, second/0, third/0]).
-file("beam_implicit_line_file.erl", 42).
first() -> first.
-file("other/beam_implicit_line_file.erl", 42).
second() -> second.
-file("beam_implicit_line_file.erl", 42).
third() -> third.
