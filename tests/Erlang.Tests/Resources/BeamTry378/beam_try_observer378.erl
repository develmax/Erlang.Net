-module(beam_try_observer378).
-export([run/1, observe/1]).

run(Output) ->
    Results = [{Case, observe(Case)} || Case <- beam_try_fixture378:cases()],
    ok = file:write_file(Output, term_to_binary(Results)),
    ok.

observe(Case) ->
    Parent = self(),
    {Child, Monitor} = spawn_monitor(beam_try_fixture378, capture, [Parent, Case]),
    receive
        {Case, Outcome} ->
            receive
                {'DOWN', Monitor, process, Child, normal} -> Outcome;
                {'DOWN', Monitor, process, Child, Reason} ->
                    erlang:error({reported_but_abnormal_child, Case, Reason})
            after 5000 ->
                exit(Child, kill),
                erlang:error({missing_child_down, Case})
            end;
        {'DOWN', Monitor, process, Child, Reason} ->
            erlang:error({child_down_without_report, Case, Reason})
    after 5000 ->
        exit(Child, kill),
        receive
            {'DOWN', Monitor, process, Child, _} -> ok
        after 5000 ->
            erlang:error({unsettled_child, Case})
        end,
        erlang:error({missing_child_report, Case})
    end.
