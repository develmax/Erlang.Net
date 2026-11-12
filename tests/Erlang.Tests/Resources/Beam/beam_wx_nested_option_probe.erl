-module(beam_wx_nested_option_probe).
-export([option/1, window_option/1]).
option({pos, {_X, _Y}} = Arg) -> Arg;
option({size, {_, _}} = Arg) -> Arg;
option({style, _} = Arg) -> Arg;
option(Bad) -> erlang:error({badoption, Bad}).

window_option({id, _} = Arg) -> Arg;
window_option({pos, {_, _}} = Arg) -> Arg;
window_option({size, {_, _}} = Arg) -> Arg;
window_option({style, _} = Arg) -> Arg;
window_option(Bad) -> erlang:error({badoption, Bad}).
