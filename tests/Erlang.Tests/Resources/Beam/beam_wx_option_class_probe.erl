-module(beam_wx_option_class_probe).
-export([option/1]).
-record(wx_ref, {ref, type, state}).
option({initial, #wx_ref{type=Type}} = Arg) ->
    (Type =:= wxFont) orelse Type:parent_class(wxFont),
    Arg;
option({pos, {_, _}} = Arg) -> Arg;
option({style, _} = Arg) -> Arg;
option({validator, #wx_ref{type=Type}} = Arg) ->
    (Type =:= wx) orelse Type:parent_class(wx),
    Arg;
option(Bad) -> erlang:error({badoption, Bad}).

