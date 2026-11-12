-module(beam_binary_digit_list_probe).
-export([format/1]).

format(Bin) -> digits(Bin, 25).

digits(<<>>, _) ->
    [];
digits(<<High:4, Low:4>>, _) ->
    [integer_to_list(High, 16), integer_to_list(Low, 16)];
digits(<<High:4, Low:4, Rest/binary>>, 0) ->
    [integer_to_list(High, 16), integer_to_list(Low, 16), $,, $\n, $\s, $\s
     | digits(Rest, 25)];
digits(<<High:4, Low:4, Rest/binary>>, N) ->
    [integer_to_list(High, 16), integer_to_list(Low, 16), $,
     | digits(Rest, N - 1)].
