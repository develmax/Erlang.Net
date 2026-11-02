-module(source_literalpool_probe820).
-export([observe/0]).

observe() ->
    {parser_constant820, erlang:system_info(wordsize),
     [{tuple, shape(source_literalpool_subject820:tuple_literal(), source_literalpool_subject820:tuple_literal())},
      {list, shape(source_literalpool_subject820:list_literal(), source_literalpool_subject820:list_literal())},
      {improper, shape(source_literalpool_subject820:improper_literal(), source_literalpool_subject820:improper_literal())},
      {bits7, shape(source_literalpool_subject820:small_bits_literal(), source_literalpool_subject820:small_bits_literal())},
      {binary130, shape(source_literalpool_subject820:large_binary_literal(), source_literalpool_subject820:large_binary_literal())},
      {native_bits, shape(source_literalpool_subject820:native_bits(), source_literalpool_subject820:native_bits())},
      {sparse_binary, shape(source_literalpool_subject820:sparse_binary(), source_literalpool_subject820:sparse_binary())}],
     lifetime(literal), lifetime(dynamic)}.

shape(Value, Again) ->
    Copy = binary_to_term(term_to_binary(Value)),
    {Value, erts_debug:size_shared(Value), erts_debug:flat_size(Value),
     erts_debug:size(Value), erts_debug:same(Value, Again),
     {Copy, erts_debug:size_shared(Copy), erts_debug:flat_size(Copy),
      erts_debug:same(Value, Copy)}}.

lifetime(Mode) ->
    Parent = self(),
    {Receiver, ReceiverMonitor} = spawn_monitor(fun() -> receiver(Parent) end),
    {Sender, SenderMonitor} = spawn_monitor(fun() ->
        Bin = case Mode of
            literal -> source_literalpool_subject820:large_binary_literal();
            dynamic -> binary:copy(source_literalpool_subject820:large_binary_literal())
        end,
        Receiver ! {payload, Bin},
        receive release_sender -> ok after 10000 -> erlang:error(sender_timeout) end
    end),
    Before = receive {held, Snapshot0} -> Snapshot0 after 10000 -> erlang:error(held_timeout) end,
    Sender ! release_sender,
    receive {'DOWN', SenderMonitor, process, Sender, normal} -> ok
    after 10000 -> erlang:error(sender_down_timeout) end,
    Receiver ! inspect,
    After = receive {live, Snapshot1} -> Snapshot1 after 10000 -> erlang:error(live_timeout) end,
    Receiver ! drop,
    Dropped = receive {released, Footprint} -> Footprint after 10000 -> erlang:error(drop_timeout) end,
    Receiver ! finish,
    receive {'DOWN', ReceiverMonitor, process, Receiver, normal} -> ok
    after 10000 -> erlang:error(receiver_down_timeout) end,
    {Mode, Before, After, Dropped, sender_down, receiver_down}.

receiver(Parent) ->
    receive {payload, Bin} ->
        Parent ! {held, snapshot_binary(Bin)},
        receive inspect ->
            true = erlang:garbage_collect(),
            Parent ! {live, snapshot_binary(Bin)},
            receive drop -> released(Parent)
            after 10000 -> erlang:error(receiver_drop_timeout) end
        after 10000 -> erlang:error(receiver_inspect_timeout) end
    after 10000 -> erlang:error(payload_timeout) end.

released(Parent) ->
    true = erlang:garbage_collect(),
    Parent ! {released, binary_footprint()},
    receive finish -> ok after 10000 -> erlang:error(finish_timeout) end.

snapshot_binary(Bin) ->
    {Bin, bit_size(Bin), binary:referenced_byte_size(Bin),
     erts_debug:size_shared(Bin), erts_debug:flat_size(Bin), binary_footprint()}.

binary_footprint() ->
    {binary, Entries} = erlang:process_info(self(), binary),
    binary_footprint(Entries, 0, 0).

binary_footprint([{_Identifier, Size, _References} | Rest], Count, Bytes) ->
    binary_footprint(Rest, Count + 1, Bytes + Size);
binary_footprint([], Count, Bytes) -> {Count, Bytes}.
