-module(beam_trapping_tail_frames).
-export([disconnect/2,opt_unlink/1]).

disconnect(Process, Reason) -> erlang:exit(Process, Reason).

opt_unlink(Pid) ->
    if
        Pid =:= undefined -> ok;
        true -> unlink(Pid)
    end.
