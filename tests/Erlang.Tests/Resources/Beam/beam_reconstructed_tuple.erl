-module(beam_reconstructed_tuple).
-export([run/1, constructed/2]).
run(Arg) ->
    case dep:result(Arg) of
        {ok, Val} -> Val;
        {error, Reason} -> {error, Reason}
    end.

constructed(Target, Data) ->
    case dep:result(Target,
                    {sample, {sample, start, [Data]}, temporary, 10000, worker, [sample]}) of
        {ok, Val} -> Val;
        {error, Reason} -> {error, Reason}
    end.
