-module(beam_fixed_literal_prefix).
-export([decode/1]).
decode(<<16#12345678:32,-2:8/signed,Value:8,Tail/binary>>) ->
    {Value,Tail};
decode(_) ->
    mismatch.
