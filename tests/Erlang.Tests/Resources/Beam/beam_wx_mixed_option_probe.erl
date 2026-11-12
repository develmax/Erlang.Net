-module(beam_wx_mixed_option_probe).
-export([option/1, colour_option/1]).
option({value, Value}) -> Binary = unicode:characters_to_binary(Value), {value, Binary};
option({caption, Caption}) -> Binary = unicode:characters_to_binary(Caption), {caption, Binary};
option({pos, {_, _}} = Arg) -> Arg;
option({style, _} = Arg) -> Arg;
option(Bad) -> erlang:error({badoption, Bad}).

colour_option({flags, _} = Arg) -> Arg;
colour_option({col, Colour}) -> {col, wxe_util:color(Colour)};
colour_option(Bad) -> erlang:error({badoption, Bad}).
