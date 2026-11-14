-module(beam_record_branch_frame).
-export([run/2]).
-record(state, {items}).
run({request, Id}, #state{items=Items}=State) ->
    New = beam_probe:change(Id, Items),
    {ok,State#state{items=New}};
run(_,State) -> {ok,State}.
