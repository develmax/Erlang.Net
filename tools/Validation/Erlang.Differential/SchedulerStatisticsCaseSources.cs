namespace Erlang.Differential;

internal static class SchedulerStatisticsCaseSources
{
    internal const string Shape = """
        -module(scheduler_statistics).
        -export([run/0]).
        run() ->
            N=erlang:system_info(schedulers),
            lists:foreach(fun(Key)->
                V=erlang:statistics(Key),true=is_integer(V),true=(V>=0)
            end,[run_queue,total_run_queue_lengths,total_run_queue_lengths_all,total_active_tasks,total_active_tasks_all]),
            lists:foreach(fun({Key,Extra})->
                L=erlang:statistics(Key),true=(length(L)=:=N+Extra),
                true=lists:all(fun(V)->is_integer(V) andalso V>=0 end,L)
            end,[{run_queue_lengths,1},{run_queue_lengths_all,2},{active_tasks,1},{active_tasks_all,2}]),
            A=erlang:statistics(total_active_tasks),true=(A>=1),done.
        """;
}
