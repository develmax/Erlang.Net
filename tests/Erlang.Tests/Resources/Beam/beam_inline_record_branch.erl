-module(beam_inline_record_branch).
-export([run/2]).
-record(state, {items}).
run({request, Id}, #state{items=Items}=State) ->
    {ok,State#state{items=beam_probe:change(Id, Items)}};
run(_,State) -> {ok,State}.
