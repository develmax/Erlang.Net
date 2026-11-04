%% A valid handler deliberately omitting all optional callbacks.
-module(gen_event_no_optional).
-export([init/1,handle_event/2,handle_call/2]).
init(State) -> {ok,State}.
handle_event(N,State) -> {ok,State+N}.
handle_call(_,State) -> {ok,State,State}.
