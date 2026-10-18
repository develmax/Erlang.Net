-module(scheduler_migration_previous_binding).
-export([run/0]).
run() ->
    Count=erlang:system_info(schedulers), true=Count>=2,
    0=process_flag(scheduler,1), 1=erlang:system_info(scheduler_id),
    1=process_flag(scheduler,Count), Count=erlang:system_info(scheduler_id),
    Count=process_flag(scheduler,0), 0=process_flag(scheduler,0), done.
