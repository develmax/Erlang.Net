-module(contract_resources506).
-export([run/0]).

capture(Fun) ->
    try Fun() of Value -> {returned, Value}
    catch Class:Reason:Stack -> {raised, Class, Reason, Stack}
    end.

info(Pid) ->
    Keys = [heap_size, total_heap_size, stack_size, memory, garbage_collection,
            garbage_collection_info, min_heap_size, min_bin_vheap_size,
            max_heap_size, fullsweep_after, message_queue_data, async_dist,
            message_queue_len, dictionary],
    {capture(fun() -> erlang:process_info(Pid) end),
     [{Key, capture(fun() -> erlang:process_info(Pid, Key) end)} || Key <- Keys],
     capture(fun() -> erlang:process_info(Pid, [heap_size, heap_size, stack_size]) end),
     capture(fun() -> erlang:process_info(Pid, [heap_size, total_heap_size, stack_size,
                                               garbage_collection]) end)}.

request(Pid, Message) ->
    Ref = make_ref(),
    Pid ! erlang:insert_element(3, Message, Ref),
    receive {reply, Ref, Pid, Value} -> Value
    after 5000 -> error({request_timeout, Message})
    end.

run() ->
    Parent = self(),
    Token = make_ref(),
    {Pid, Monitor} = spawn_monitor(fun() -> contract_subject506:held(Parent, Token, []) end),
    receive {held, Token, Pid} -> ok after 5000 -> error(held_timeout) end,
    try
        Before = info(Pid),
        GcBefore = erlang:statistics(garbage_collection),
        Payload = {lists:seq(1, 4096), binary:copy(<<17>>, 8192)},
        put(contract_parent_payload506, Payload),
        replaced = request(Pid, {replace, Parent, Payload}),
        Held = info(Pid),
        true = request(Pid, {gc, Parent, major}),
        CollectedWhileHeld = info(Pid),
        dropped = request(Pid, {drop, Parent}),
        true = request(Pid, {gc, Parent, major}),
        Released = info(Pid),
        GcAfter = erlang:statistics(garbage_collection),
        {#{caller => Parent, target => Pid, monitor => Monitor, token => Token,
           caller_group_leader => group_leader(), node => node()},
         Before, Held, CollectedWhileHeld, Released, GcBefore, GcAfter}
    after
        erase(contract_parent_payload506),
        unlink(Pid),
        exit(Pid, kill),
        receive {'DOWN', Monitor, process, Pid, _} -> ok
        after 5000 -> error(cleanup_timeout)
        end
    end.
