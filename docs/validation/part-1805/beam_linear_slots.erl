-module(beam_linear_slots).
-export([run/2, heading/2, log/3, finish/1]).
-record(state, {unused, target_name}).

run(Pid, State) ->
    log(heading(run, State#state.target_name), "closing", [Pid]),
    finish(Pid).

heading(Tag, Name) -> {Tag, Name}.
log(Heading, Format, Args) -> put(log, {Heading, Format, Args}).
finish(Pid) -> {done, Pid}.
