-module(beam_guarded_atom_tail).
-export([run/4, flipped/4]).

run(Keep, alpha, Pid, Value) when is_pid(Pid) ->
    pack(Keep, left, Pid, Value);
run(Keep, gamma, Process, Value) when erlang:is_pid(Process) ->
    pack(Keep, right, Process, Value).

flipped(gamma, Keep, Value, Process) when is_pid(Process) ->
    pack(right, Keep, Value, Process);
flipped(alpha, Keep, Value, Pid) when is_pid(Pid) ->
    pack(left, Keep, Value, Pid).

pack(Keep, Side, Pid, Value) -> {Keep, Side, Pid, Value}.
