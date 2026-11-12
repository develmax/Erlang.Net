-module(beam_include_probe).
-include("beam_probe_outer.hrl").
-if(?TEXT({16#2a, 'a b'}) =:= "{ 42 , 'a b' }" andalso
    ?TEXT({$\s, "a\n"}) =:= "{ $\\s , \"a\\n\" }").
-export([answer/0, line/0]).
-endif.
answer() -> ?ANSWER.
line() -> ?HEADER_LINE.
