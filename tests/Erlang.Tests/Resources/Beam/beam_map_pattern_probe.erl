-module(beam_map_pattern_probe).
-export([read/1]).
read(#{key:=V}) -> V;
read(_) -> missing.
