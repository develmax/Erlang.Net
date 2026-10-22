-module(binary369_closure_contract).
-export([cases/0,run/0,run_stacks/0,probe/1,relay/1]).
cases() -> [live_stack_three_matched_worker,live_stack_four_matched_worker,hex_before_decode_7,hex_before_decode_8,hex_before_decode_9,hex_before_decode_64,hex_before_decode_65,hex_before_decode_257,external_size_writable,term_to_binary_writable,self_send_default,cross_process_send,join_writable_input,encode_writable_input].
run() -> binary_all369_support:run(binary369_closure_contract,cases()).
run_stacks() -> binary_all369_support:profiles(binary369_closure_contract,cases()).
probe(live_stack_three_matched_worker) -> binary369_callback_supplement:probe(live_stack_three);
probe(live_stack_four_matched_worker) -> binary369_callback_supplement:probe(live_stack_four);
probe(hex_before_decode_7) -> B = binary:copy(<<7>>,7), E = binary:encode_hex(B), EB = observe(E), D = binary:decode_hex(E), {observe(B),EB,observe(E),observe(D),D =:= B};
probe(hex_before_decode_8) -> B = binary:copy(<<7>>,8), E = binary:encode_hex(B), EB = observe(E), D = binary:decode_hex(E), {observe(B),EB,observe(E),observe(D),D =:= B};
probe(hex_before_decode_9) -> B = binary:copy(<<7>>,9), E = binary:encode_hex(B), EB = observe(E), D = binary:decode_hex(E), {observe(B),EB,observe(E),observe(D),D =:= B};
probe(hex_before_decode_64) -> B = binary:copy(<<7>>,64), E = binary:encode_hex(B), EB = observe(E), D = binary:decode_hex(E), {observe(B),EB,observe(E),observe(D),D =:= B};
probe(hex_before_decode_65) -> B = binary:copy(<<7>>,65), E = binary:encode_hex(B), EB = observe(E), D = binary:decode_hex(E), {observe(B),EB,observe(E),observe(D),D =:= B};
probe(hex_before_decode_257) -> B = binary:copy(<<7>>,257), E = binary:encode_hex(B), EB = observe(E), D = binary:decode_hex(E), {observe(B),EB,observe(E),observe(D),D =:= B};
probe(external_size_writable) -> B = append_loop(257,<<>>), Before = observe(B), Size = erlang:external_size(B), {Before,Size,observe(B)};
probe(term_to_binary_writable) -> B = append_loop(257,<<>>), Before = observe(B), Wire = erlang:term_to_binary(B), After = observe(B), Restored = erlang:binary_to_term(Wire), {Before,After,observe(Restored),Restored =:= B};
probe(self_send_default) -> B = append_loop(257,<<>>), Before = observe(B), self()!{closure_binary,B}, receive {closure_binary,Received} -> {Before,observe(B),observe(Received),Received =:= B} after 5000 -> erlang:error(self_send_timeout) end;
probe(cross_process_send) -> B = append_loop(257,<<>>), Before = observe(B), {ParentAfter,ChildView} = cross_send(B), {Before,ParentAfter,ChildView};
probe(join_writable_input) -> B = append_loop(257,<<>>), Before = observe(B), Joined = binary:join([B,<<>>],<<>>), {Before,observe(B),observe(Joined),Joined =:= B};
probe(encode_writable_input) -> B = append_loop(257,<<>>), Before = observe(B), Encoded = binary:encode_hex(B), {Before,observe(B),observe(Encoded)}.
observe(Binary) -> {byte_size(Binary),binary:referenced_byte_size(Binary)}.
append_loop(0,Binary) -> Binary;
append_loop(Count,Binary) -> append_loop(Count-1,<<Binary/binary,7>>).
cross_send(Binary) ->
    {Pid,Ref} = spawn_monitor(binary369_closure_contract,relay,[self()]),
    Pid!{closure_binary,Binary},
    receive
        {Pid,Observed} -> relay_down(Pid,Ref,Binary,Observed);
        {'DOWN',Ref,process,Pid,Reason} -> erlang:error({relay_failed,Reason})
    after 5000 -> relay_abort(Pid,Ref) end.
relay_down(Pid,Ref,Binary,Observed) ->
    receive {'DOWN',Ref,process,Pid,normal} -> {observe(Binary),Observed};
            {'DOWN',Ref,process,Pid,Reason} -> erlang:error({relay_failed,Reason})
    after 5000 -> relay_abort(Pid,Ref) end.
relay_abort(Pid,Ref) ->
    exit(Pid,kill),
    receive {'DOWN',Ref,process,Pid,_} -> erlang:error(relay_timeout)
    after 5000 -> erlang:error(relay_cleanup_timeout) end.
relay(Parent) ->
    receive {closure_binary,Binary} -> Parent!{self(),observe(Binary)},normal
    after 5000 -> erlang:error(relay_input_timeout) end.
