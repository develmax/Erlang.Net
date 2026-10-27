-module(contract_proclib509).
-export([run/0]).

capture(Fun) -> contract_control509:capture(Fun).

run() ->
    Token = make_ref(),
    Logger = capture(fun() -> logger:add_handler(contract_report509, contract_logger509,
        #{level => all, config => #{owner => self(), token => Token}}) end),
    try
        {returned, ok} = Logger,
        Init = [init_case(Kind, Ancestors) || Kind <- [fun0, fun1, mfa, missing_mfa],
                                          Ancestors <- [[], [ancestor509], improper_ancestor509]],
        InitLogs = barrier(Token),
        Crashes = [crash_case(Class, Reason, Token) || {Class, Reason} <-
            [{exit, normal}, {exit, shutdown}, {exit, {shutdown, detail509}},
             {exit, failure509}, {error, failure509}, {throw, failure509}]],
        Hibernate = [hibernate_case(Mode, Token) || Mode <- [return509, {error, wake_failure509}]],
        Invalid = invalid_entries(),
        Formats = format_cases(),
        Barrier = barrier(Token),
        #{caller => self(), node => node(), logger_token => Token, logger => Logger,
          init => Init, init_logs => InitLogs, crashes => Crashes, hibernate => Hibernate,
          invalid => Invalid, formats => Formats, final_logger_barrier => Barrier}
    after logger:remove_handler(contract_report509)
    end.

init_case(Kind, Ancestors) ->
    Parent = self(), Token = make_ref(),
    Body = case Kind of
        fun0 -> fun() -> proc_lib:init_p(Parent, Ancestors,
                    fun() -> contract_proc_subject509:held(Parent, Token) end) end;
        fun1 -> fun() -> proc_lib:init_p(Parent, Ancestors, fun(Value) -> Value end) end;
        mfa -> fun() -> proc_lib:init_p(Parent, Ancestors, contract_proc_subject509, held, [Parent, Token]) end;
        missing_mfa -> fun() -> proc_lib:init_p(Parent, Ancestors, missing_module509, missing_function509, []) end
    end,
    {Pid, Monitor} = spawn_monitor(Body),
    try
        First = receive
            {proc_ready509, Token, Pid, A, I} -> {ready509, A, I, contract_control509:waiting(Pid, held)};
            {'DOWN', Monitor, process, Pid, Reason} -> {down509, Reason}
        after 5000 -> error({init_case_timeout509, Kind, Pid}) end,
        Info = capture(fun() -> process_info(Pid, [initial_call, dictionary, current_function, parent]) end),
        Cleanup = contract_control509:settle(Pid, Monitor),
        {Kind, Ancestors, Token, Pid, Monitor, First, Info, Cleanup}
    after contract_control509:settle(Pid, Monitor)
    end.

crash_case(Class, Reason, LogToken) ->
    Parent = self(),
    % A nontrapping live neighbour provides the actual linked_info domain;
    % its possible subsequent exit is retained by the real monitor.
    {Neighbour, NMon} = spawn_monitor(fun() ->
        Parent ! {neighbour_ready509, self()},
        receive stop_neighbour509 -> ok end
    end),
    receive {neighbour_ready509, Neighbour} -> ok after 5000 -> error(neighbour_timeout509) end,
    {Pid, Monitor} = spawn_monitor(fun() ->
        link(Neighbour),
        proc_lib:init_p(Parent, [], contract_proc_subject509, fail, [Class, Reason])
    end),
    try
        Down = receive {'DOWN', Monitor, process, Pid, Result} -> Result
               after 5000 -> error({crash_timeout509, Pid}) end,
        {Logs, Marker} = barrier(LogToken),
        Formats = [{Emitter, capture(fun() -> proc_lib:report_cb(Report, #{}) end)} ||
                   {Emitter, #{msg := {report, Report = #{label := {proc_lib, crash}}}}} <- Logs],
        Cleanup = contract_control509:settle(Pid, Monitor),
        NeighbourCleanup = contract_control509:settle(Neighbour, NMon),
        {Class, Reason, Pid, Monitor, Neighbour, NMon, Down, Logs, Marker, Formats, Cleanup, NeighbourCleanup}
    after
        contract_control509:settle(Pid, Monitor),
        contract_control509:settle(Neighbour, NMon)
    end.

hibernate_case(Mode, LogToken) ->
    Parent = self(), Token = make_ref(),
    {Pid, Monitor} = spawn_monitor(fun() -> proc_lib:init_p(Parent, [],
        contract_proc_subject509, hibernate, [Parent, Token, Mode]) end),
    try
        Before = receive {pre_hibernate509, Token, Pid, A, I} -> {A, I}
                 after 5000 -> error({pre_hibernate_timeout509, Pid}) end,
        % This atomic status/current-function witness is the sleep boundary.
        Slept = contract_control509:waiting(Pid, hibernate),
        Asleep = capture(fun() -> process_info(Pid, [heap_size, stack_size, total_heap_size,
                            memory, dictionary, initial_call, current_function]) end),
        Pid ! {wake509, Token},
        Woke = receive {woke509, Token, Pid, Kept, A2, I2} -> {Kept, A2, I2}
               after 5000 -> error({wake_timeout509, Pid}) end,
        Terminal = case Mode of
            return509 -> receive {proc_ready509, Token, Pid, _, _} ->
                            contract_control509:waiting(Pid, held),
                            capture(fun() -> process_info(Pid, [status, dictionary, current_stacktrace]) end)
                         after 5000 -> error({wake_ready_timeout509, Pid}) end;
            _ -> receive {'DOWN', Monitor, process, Pid, Reason} -> {down509, Reason}
                 after 5000 -> error({wake_down_timeout509, Pid}) end
        end,
        Logs = barrier(LogToken),
        Cleanup = contract_control509:settle(Pid, Monitor),
        {Mode, Token, Pid, Monitor, Before, Slept, Asleep, Woke, Terminal, Logs, Cleanup}
    after contract_control509:settle(Pid, Monitor)
    end.

barrier(Token) ->
    Ref = make_ref(),
    logger:error(#{label => {contract_barrier509, Ref}}),
    drain(Token, Ref, []).

drain(Token, Ref, Acc) ->
    receive
        {log509, Token, Emitter, #{msg := {report, #{label := {contract_barrier509, Ref}}}} = Event} ->
            {lists:reverse(Acc), {logger_marker509, Ref, Emitter, Event}};
        {log509, Token, Emitter, Event} -> drain(Token, Ref, [{Emitter, Event}|Acc])
    after 5000 -> error({logger_barrier_timeout509, Token, Ref}) end.

invalid_entries() ->
    [isolated_invalid(Id, Fun) || {Id, Fun} <-
        [{init3_fun, fun() -> proc_lib:init_p(self(), [], 42) end},
         {init5_module, fun() -> proc_lib:init_p(self(), [], 42, entry, []) end},
         {init5_args, fun() -> proc_lib:init_p(self(), [], contract_proc_subject509, held, [a|b]) end},
         {wake_module, fun() -> proc_lib:wake_up(42, entry, []) end},
         {wake_args, fun() -> proc_lib:wake_up(contract_proc_subject509, held, [a|b]) end},
         {hibernate_module, fun() -> proc_lib:hibernate(42, entry, []) end},
         {hibernate_args, fun() -> proc_lib:hibernate(contract_proc_subject509, held, [a|b]) end}]].

% proc_lib:hibernate accepts an improper cons in its wrapper guard and packs
% it into the proper outer wake_up argument list. Hibernating the controller
% would erase its capture/eval stack. Every candidate therefore gets its own
% actual process, and a tagged pending message guarantees a wake if it sleeps.
isolated_invalid(Id, Fun) ->
    Parent = self(), Token = make_ref(),
    {Pid, Monitor} = spawn_monitor(fun() ->
        Parent ! {invalid_armed509, Token, self()},
        receive {invalid_invoke509, Token} ->
            Value = capture(Fun),
            Parent ! {invalid_returned509, Token, self(), Value}
        end
    end),
    try
        Ready = receive {invalid_armed509, Token, Pid} -> armed509
                after 5000 -> error({invalid_armed_timeout509, Id, Pid, Token}) end,
        Pid ! {invalid_invoke509, Token},
        Pid ! {invalid_wake509, Token},
        Outcome = receive
            {invalid_returned509, Token, Pid, Returned} ->
                Down = receive {'DOWN', Monitor, process, Pid, ReturnedReason} -> ReturnedReason
                       after 5000 -> error({invalid_down_timeout509, Id, Pid, Monitor}) end,
                {returned_to_capture509, Returned, {down509, Monitor, Pid, Down}};
            {'DOWN', Monitor, process, Pid, DestructiveReason} ->
                % A valid destructive hibernate never returns through capture.
                % Its actual wake_up exit reason/stack and logger report remain
                % the observed protocol; do not invent a wrapper guard error.
                {did_not_return509, {down509, Monitor, Pid, DestructiveReason}}
        after 5000 -> error({invalid_case_timeout509, Id, Pid, Token}) end,
        Cleanup = contract_control509:settle(Pid, Monitor),
        {Id, #{pid => Pid, monitor => Monitor, token => Token, ready => Ready,
               wake_message => {invalid_wake509, Token}, outcome => Outcome, cleanup => Cleanup}}
    after contract_control509:settle(Pid, Monitor)
    end.

format_cases() ->
    [{Report, Options, capture(fun() -> proc_lib:report_cb(Report, Options) end)} ||
        Report <- [#{label => {proc_lib, crash}, report => [[], []]},
                   #{label => wrong509, report => []}, #{}, not_report509],
        Options <- [#{}, #{single_line => true, chars_limit => 80, depth => 5, encoding => utf8},
                    #{encoding => latin1}, not_options509]].
