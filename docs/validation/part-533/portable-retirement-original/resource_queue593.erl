-module(resource_queue593).
-export([run/0, receiver/2, sender/3]).

%% Optional NEW observer over the unchanged real family527 DSO. No C API,
%% existing observer/default job, source golden or timeout is modified.
run() ->
    [{consume, observe_case(consume)}, {abandon, observe_case(abandon)}].

observe_case(Mode) ->
    {Version, Before, _} = native_resource_family527:count527(),
    Ref = make_ref(),
    {Receiver, ReceiverMonitor} = spawn_monitor(?MODULE, receiver, [self(), Ref]),
    {Sender, SenderMonitor} = spawn_monitor(?MODULE, sender, [self(), Ref, Receiver]),
    Result = try
        receive {Ref, ready593} -> ok after 10000 -> error(no_receiver_ready593) end,
        receive
            {Ref, sent593} -> ok;
            {'DOWN', SenderMonitor, process, Sender, SenderFailure} ->
                error({sender_failed593, SenderFailure})
        after 10000 -> error(no_sender_ack593)
        end,
        receive {'DOWN', SenderMonitor, process, Sender, normal} -> ok
          after 10000 -> error(no_sender_normal_down593) end,
        %% Only the queued entry owns resource headers now. No resource term
        %% crosses this controller or an external host callback boundary.
        {Version, Before, _} = Queued = native_resource_family527:count527(),
        Inspection = case Mode of
            consume ->
                Receiver ! {Ref, consume593},
                Value = receive {Ref, inspected593, Checked} -> Checked
                    after 10000 -> error(no_inspection593) end,
                receive {'DOWN', ReceiverMonitor, process, Receiver, normal} -> ok
                    after 10000 -> error(no_receiver_normal_down593) end,
                Value;
            abandon ->
                exit(Receiver, kill),
                receive {'DOWN', ReceiverMonitor, process, Receiver, killed} -> ok
                    after 10000 -> error(no_receiver_killed_down593) end,
                abandoned593
        end,
        After = await_count593(Version, Before + 1, 2000),
        {returned593, {before_count593, Before}, {queued593, Queued}, Inspection, {after593, After}}
    catch Class:Reason:Stack -> {raised593, Class, Reason, Stack}
    end,
    %% Both cleanup actions are independent. Full primary failures are kept in
    %% Result even when a later bounded cleanup also fails.
    SenderCleanup = cleanup593(Sender, SenderMonitor),
    ReceiverCleanup = cleanup593(Receiver, ReceiverMonitor),
    {Result, [{sender_cleanup593, SenderCleanup}, {receiver_cleanup593, ReceiverCleanup}]}.

sender(Parent, Ref, Receiver) ->
    make_and_send593(Receiver, Ref),
    Parent ! {Ref, sent593},
    ok.

make_and_send593(Receiver, Ref) ->
    {Resource, Binary, _} = native_resource_family527:make527(),
    Receiver ! {Ref, payload593, Resource, Binary},
    ok.

receiver(Parent, Ref) ->
    Parent ! {Ref, ready593},
    %% Selective receive must leave the earlier resource-bearing payload queued
    %% until the controller observed actual sender death/count with no lease.
    receive {Ref, consume593} ->
        receive {Ref, payload593, Resource, Binary} ->
            {0, _, 4096} = Inspected = native_resource_family527:inspect527(Resource),
            4096 = byte_size(Binary),
            Parent ! {Ref, inspected593,
                {Inspected, byte_size(Binary), binary:referenced_byte_size(Binary), binary_to_list(Binary)}},
            ok
        after 10000 -> error(no_payload593)
        end
    after 10000 -> error(no_consume593)
    end.

await_count593(Version, Expected, 0) ->
    error({destructor_timeout593, Version, Expected, native_resource_family527:count527()});
await_count593(Version, Expected, Left) ->
    case native_resource_family527:count527() of
        {Version, Expected, _} = Value -> Value;
        {Version, Count, _} when Count < Expected ->
            timer:sleep(5), await_count593(Version, Expected, Left - 1);
        Other -> error({unexpected_destructor593, Other})
    end.

cleanup593(Pid, Monitor) ->
    try
        exit(Pid, kill),
        case is_process_alive(Pid) of
            false -> ok;
            true -> receive {'DOWN', Monitor, process, Pid, _} -> ok
                after 10000 -> error(cleanup_down_timeout593) end
        end,
        demonitor(Monitor, [flush]),
        ok
    catch Class:Reason:Stack -> {raised_cleanup593, Class, Reason, Stack}
    end.
