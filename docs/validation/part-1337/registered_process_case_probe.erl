-module(registered_process_case_probe).
-export([dispatch/1, remote_dispatch/1, self/0, found/1, missing/1, render/1]).
dispatch(Value) ->
    Current = erlang:self(),
    case whereis(registered_process_case_probe) of
        Current ->
            Text = "registered process received [" ++
                atom_to_list(Value) ++ "]",
            ?MODULE:render(Text);
        Pid when is_pid(Pid) ->
            ?MODULE:found(Value);
        _ ->
            ?MODULE:missing(Value)
    end.
remote_dispatch(Value) ->
    Current = ?MODULE:self(),
    case whereis(registered_process_case_probe) of
        Current ->
            Text = "registered process received [" ++
                atom_to_list(Value) ++ "]",
            ?MODULE:render(Text);
        Pid when is_pid(Pid) ->
            ?MODULE:found(Value);
        _ ->
            ?MODULE:missing(Value)
    end.
self() -> undefined.
found(Value) -> {found,Value}.
missing(Value) -> {missing,Value}.
render(Value) -> {render,Value}.
