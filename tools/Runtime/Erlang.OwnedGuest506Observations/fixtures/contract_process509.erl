-module(contract_process509).
-export([run/0]).

capture(Fun) -> contract_control509:capture(Fun).

run() ->
    Parent = self(), Token = make_ref(),
    {Pid, Monitor} = spawn_monitor(fun() -> contract_process_subject509:start(Parent, Token) end),
    try
        Setup = receive {ready509, Token, Pid, Value} -> Value
                after 5000 -> error({ready_timeout509, Pid, Token}) end,
        Barrier = contract_control509:waiting(Pid, held),
        Items = contract_catalog509:items(),
        Default = capture(fun() -> process_info(Pid) end),
        DefaultOrder = case Default of {returned, Entries} -> [Key || {Key, _} <- Entries]; _ -> Default end,
        Scalar = [{Item, capture(fun() -> process_info(Pid, Item) end)} || Item <- Items],
        List = capture(fun() -> process_info(Pid, Items) end),
        Duplicates = [[registered_name, registered_name, status, status],
            [dictionary, {dictionary, {tuple_key509,1}}, dictionary, {dictionary, absent509},
             {dictionary, {tuple_key509,1}}],
            [heap_size, heap_size, stack_size, total_heap_size, garbage_collection]],
        ListCases = [[], [status], Items|Duplicates] ++
            [[unknown509, status], [status, unknown509], [status|improper509],
             [{dictionary}], [{dictionary, atom_key509, extra509}], [42]],
        Lists = [{Query, capture(fun() -> process_info(Pid, Query) end)} || Query <- ListCases],
        Self = [{Item, capture(fun() -> process_info(self(), Item) end)} || Item <- Items],
        Owner = contract_control509:request(Pid, snapshot, Items),
        Registration = capture(fun() -> register(contract_registered509, Pid) end),
        Registered = {capture(fun() -> process_info(Pid) end),
                      capture(fun() -> process_info(Pid, [registered_name, registered_name]) end)},
        Unregister = capture(fun() -> unregister(contract_registered509) end),
        Pid ! {retained_message509, Token, 1},
        Pid ! {retained_message509, Token, 2},
        % Query and prior messages share this controller sender: process_info's
        % force-signal handling sees their preceding signal order.
        Inbox = capture(fun() -> process_info(Pid, [messages, message_queue_len, messages]) end),
        Resource = resources(Pid),
        Stop = contract_control509:request(Pid, stop, none),
        Down = receive {'DOWN', Monitor, process, Pid, Reason} -> {Monitor, Pid, Reason}
               after 5000 -> error({down_timeout509, Pid, Monitor}) end,
        Dead = {capture(fun() -> process_info(Pid) end),
                [{Item, capture(fun() -> process_info(Pid, Item) end)} || Item <- Items],
                capture(fun() -> process_info(Pid, Items) end),
                capture(fun() -> process_info(Pid, unknown509) end)},
        InvalidPid = [{Bad, capture(fun() -> process_info(Bad, status) end)} || Bad <- [not_pid509, 42, [], make_ref()]],
        Observed = #{caller => self(), node => node(), target => Pid, monitor => Monitor, token => Token,
          setup => Setup, waiting => Barrier, catalog_count => length(Items),
          default => Default, default_order => DefaultOrder, scalars => Scalar, bulk => List,
          query_lists => Lists, self => Self, subject_self => Owner, registration => Registration,
          registered => Registered, unregister => Unregister, inbox => Inbox,
          resources => Resource, stop => Stop, down => Down, dead => Dead, invalid_pid => InvalidPid},
        Cleanup = contract_control509:settle(Pid, Monitor),
        Observed#{cleanup => Cleanup}
    after
        contract_control509:settle(Pid, Monitor)
    end.

resources(Pid) ->
    Fields = [heap_size, total_heap_size, stack_size, memory, binary, garbage_collection,
              garbage_collection_info, min_heap_size, min_bin_vheap_size, max_heap_size],
    Before = capture(fun() -> process_info(Pid, Fields) end),
    Retain = contract_control509:request(Pid, retain, none),
    Held = contract_control509:waiting(Pid, held),
    During = capture(fun() -> process_info(Pid, Fields) end),
    Minor = contract_control509:request(Pid, gc, minor),
    AfterMinor = capture(fun() -> process_info(Pid, Fields) end),
    Drop = contract_control509:request(Pid, drop, none),
    Major = contract_control509:request(Pid, gc, major),
    AfterMajor = capture(fun() -> process_info(Pid, Fields) end),
    {Before, Retain, Held, During, Minor, AfterMinor, Drop, Major, AfterMajor}.
