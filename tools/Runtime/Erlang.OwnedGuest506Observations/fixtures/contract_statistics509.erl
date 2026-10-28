-module(contract_statistics509).
-export([run/0]).

capture(Fun) -> contract_control509:capture(Fun).
snapshot() -> [{Key, capture(fun() -> erlang:statistics(Key) end)} || Key <- contract_catalog509:statistics()].

run() ->
    Parent = self(), Token = make_ref(),
    {Pid, Monitor} = spawn_monitor(fun() -> contract_process_subject509:start(Parent, Token) end),
    try
        Ready = receive {ready509, Token, Pid, Setup} -> Setup after 5000 -> error(stat_ready_timeout509) end,
        Waiting = contract_control509:waiting(Pid, held),
        Before = snapshot(),
        Retain = contract_control509:request(Pid, retain, none),
        Retained = snapshot(),
        Minor = contract_control509:request(Pid, gc, minor),
        AfterMinor = snapshot(),
        Drop = contract_control509:request(Pid, drop, none),
        Major = contract_control509:request(Pid, gc, major),
        AfterMajor = snapshot(),
        Features = [feature(scheduler_wall_time), feature(microstate_accounting)],
        Invalid = [{Value, capture(fun() -> statistics(Value) end)} || Value <-
                   [unknown_stat509, 42, [], {run_queue}, <<"runtime">>]],
        Observed = #{caller => self(), node => node(), target => Pid, token => Token, monitor => Monitor,
          ready => Ready, waiting => Waiting, catalog_count => length(contract_catalog509:statistics()),
          scheduler_witnesses => [capture(fun() -> erlang:system_info(schedulers) end),
                                  capture(fun() -> erlang:system_info(schedulers_online) end),
                                  capture(fun() -> erlang:system_info(dirty_cpu_schedulers) end),
                                  capture(fun() -> erlang:system_info(dirty_io_schedulers) end)],
          before => Before, retain => Retain, retained => Retained, minor => Minor,
          after_minor => AfterMinor, drop => Drop, major => Major, after_major => AfterMajor,
          features => Features, invalid => Invalid},
        Cleanup = contract_control509:settle(Pid, Monitor),
        Observed#{cleanup => Cleanup}
    after contract_control509:settle(Pid, Monitor)
    end.

feature(Key) ->
    Setter = capture(fun() -> erlang:system_flag(Key, true) end),
    case Setter of
        {returned, Previous} ->
            try {Key, Setter, snapshot()}
            after erlang:system_flag(Key, Previous) end;
        _ -> {Key, Setter, unsupported_in_this_original_configuration509}
    end.
