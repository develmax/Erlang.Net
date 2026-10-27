-module(contract_control509).
-export([capture/1, request/3, waiting/2, settle/2]).

capture(Fun) ->
    try Fun() of Value -> {returned, Value}
    catch Class:Reason:Stack -> {raised, Class, Reason, Stack}
    end.

request(Pid, Command, Value) ->
    Ref = make_ref(),
    Pid ! {control509, self(), Ref, Command, Value},
    receive {reply509, Ref, Pid, Result} -> {Ref, Result}
    after 5000 -> error({request_timeout509, Pid, Ref, Command})
    end.

waiting(Pid, Kind) ->
    waiting(Pid, Kind, erlang:monotonic_time(millisecond) + 5000, []).

waiting(Pid, Kind, Deadline, Seen) ->
    Value = erlang:process_info(Pid, [status, current_function]),
    case {Kind, Value} of
        {held, [{status, waiting}, _]} -> {waiting509, Value};
        {hibernate, [{status, waiting}, {current_function, {erlang, hibernate, 3}}]} ->
            {hibernated509, Value};
        _ ->
            case erlang:monotonic_time(millisecond) < Deadline of
                true -> erlang:yield(), waiting(Pid, Kind, Deadline, [Value|lists:sublist(Seen, 7)]);
                false -> error({waiting_timeout509, Pid, Kind, Value, Seen})
            end
    end.

settle(Pid, PreviousMonitor) ->
    % A fresh monitor always produces an unconsumed terminal witness, including
    % when the scenario already received its original DOWN.
    Monitor = erlang:monitor(process, Pid),
    erlang:exit(Pid, kill),
    Down = receive {'DOWN', Monitor, process, Pid, Reason} -> {down509, Monitor, Pid, Reason}
           after 5000 -> error({cleanup_timeout509, Pid, Monitor}) end,
    erlang:demonitor(PreviousMonitor, [flush]),
    Down.
