-module(beam_collectors415).
-export([run/0, run_stacks/0, child/3]).

cases() -> [release, copy_nil, copy_wrong_how, clean_nil, clean_atom, check].

run() -> [capture(Name, default) || Name <- cases()].

run_stacks() ->
    [{Name, [{Depth, capture(Name, Depth)} || Depth <- [default, 0, 1, 2, 8]]}
     || Name <- cases()].

capture(Name, default) -> capture_child(Name);
capture(Name, Depth) ->
    Previous = erlang:system_flag(backtrace_depth, Depth),
    try capture_child(Name)
    after erlang:system_flag(backtrace_depth, Previous)
    end.

capture_child(Name) ->
    {Pid, Ref} = spawn_monitor(?MODULE, child, [self(), Name, undefined]),
    receive
        {Pid, Outcome} ->
            receive {'DOWN', Ref, process, Pid, normal} -> {Name, Outcome} end;
        {'DOWN', Ref, process, Pid, Reason} -> erlang:error({collector_child_failed, Name, Reason})
    after 5000 -> erlang:error({collector_child_timeout, Name})
    end.

child(Parent, Name, _) ->
    Outcome = try probe(Name) of
                  Value -> {value, Value}
              catch Class:Reason:Trace -> {exception, Class, Reason, Trace}
              end,
    Parent ! {self(), Outcome}.

probe(release) -> erts_literal_area_collector:release_area_switch();
probe(copy_nil) -> erts_literal_area_collector:send_copy_request([], {nested, [7]}, init);
probe(copy_wrong_how) -> erts_literal_area_collector:send_copy_request([], {nested, [7]}, wrong);
probe(clean_nil) -> erts_trace_cleaner:send_trace_clean_signal([]);
probe(clean_atom) -> erts_trace_cleaner:send_trace_clean_signal(wrong);
probe(check) -> erts_trace_cleaner:check().
