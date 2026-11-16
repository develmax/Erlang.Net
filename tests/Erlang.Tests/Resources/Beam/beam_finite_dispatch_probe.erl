-module(beam_finite_dispatch_probe).
-export([absent/1, classify/1, single/1, forward/1]).
absent(optional) -> asn1_NOVALUE;
absent(default_false) -> false.
classify(4) -> {result, large};
classify(1) -> {result, small};
classify(2) -> {result, small}.
single(only) -> 1.
forward(Value) -> classify(Value).
