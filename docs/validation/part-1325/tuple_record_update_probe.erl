-module(tuple_record_update_probe).
-export([run/2, run_bound/2, store/2]).
-record(state,{value=none, other=kept}).
run(tag, {_,V}) ->
    State = case read() of
        R when is_record(R,state) -> R;
        _ -> #state{}
    end,
    store(key, State#state{value=V});
run(tag, _) -> no.
run_bound(tag, {_,V}) ->
    State = case read() of
        R when is_record(R,state) -> R;
        _ -> #state{}
    end,
    Updated = State#state{value=V},
    store(key, Updated);
run_bound(tag, _) -> no.
read() -> get(tuple_record_update_probe).
store(Key, State) -> {Key,State}.
