-module(beam_tuple_key_filter_probe).
-export([filter/1]).

filter(Options) ->
    [Option || {Key, _} = Option <- Options,
               Key =:= active orelse Key =:= deliver orelse Key =:= packet].
