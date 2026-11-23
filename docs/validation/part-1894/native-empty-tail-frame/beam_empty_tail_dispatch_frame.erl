-module(beam_empty_tail_dispatch_frame).
-export([member/1, trap/1]).
-record(state, {options}).
member(S) when is_record(S, state) ->
    member(S#state.options);
member(Options) ->
    lists:member(chosen, Options).
trap(S) when is_record(S, state) ->
    trap(S#state.options);
trap(Pid) ->
    erlang:link(Pid).