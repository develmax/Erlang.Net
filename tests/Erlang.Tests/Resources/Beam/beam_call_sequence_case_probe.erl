-module(beam_call_sequence_case_probe).
-export([options/2]).

options(File, Options) ->
    Initial = add(base, Options),
    Prepared = add(filename:dirname(File), Initial),
    case proplists:get_value(outdir, Options) of
        undefined -> Prepared;
        Dir -> add(Dir, Prepared)
    end.

add(Dir, Options) ->
    [Dir|Options].
