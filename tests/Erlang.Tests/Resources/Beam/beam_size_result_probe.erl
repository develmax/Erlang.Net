-module(beam_size_result_probe).
-export([l/1,b/1,bs/1]).
l(V) -> length(V) + 1.
b(V) -> byte_size(V) - 1.
bs(V) -> bit_size(V) + 1.
