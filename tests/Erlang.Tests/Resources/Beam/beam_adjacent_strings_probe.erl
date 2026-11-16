-module(beam_adjacent_strings_probe).
-export([text/0, binary_text/0]).
-define(PREFIX, "prefix").

text() -> ?PREFIX ""
          % Comments can separate quoted strings.
          " " "слово".

binary_text() -> <<"one" "two">>.
