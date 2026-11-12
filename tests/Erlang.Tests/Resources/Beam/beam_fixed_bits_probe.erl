-module(beam_fixed_bits_probe).
-export([read/1]).
read(<<X:16/little-signed>>) -> X;
read(_) -> no.
