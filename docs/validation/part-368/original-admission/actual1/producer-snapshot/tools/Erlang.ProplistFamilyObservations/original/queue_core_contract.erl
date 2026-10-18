-module(queue_core_contract).
-export([cases/0,run/0,run_stacks/0]).
cases() -> [new,
            is_queue_bad_atom,
            is_queue_bad_tuple0,
            is_queue_bad_tuple1,
            is_queue_bad_tuple3,
            is_queue_bad_rear,
            is_queue_bad_front,
            is_queue_empty,
            is_queue_rear_one,
            is_queue_rear_two,
            is_queue_front_one,
            is_queue_front_two,
            is_queue_split_two,
            is_empty_bad_atom,
            is_empty_bad_tuple0,
            is_empty_bad_tuple1,
            is_empty_bad_tuple3,
            is_empty_bad_rear,
            is_empty_bad_front,
            is_empty_empty,
            is_empty_rear_one,
            is_empty_rear_two,
            is_empty_front_one,
            is_empty_front_two,
            is_empty_split_two,
            len_bad_atom,
            len_bad_tuple0,
            len_bad_tuple1,
            len_bad_tuple3,
            len_bad_rear,
            len_bad_front,
            len_empty,
            len_rear_one,
            len_rear_two,
            len_front_one,
            len_front_two,
            len_split_two,
            in_bad_atom,
            in_bad_tuple0,
            in_bad_tuple1,
            in_bad_tuple3,
            in_bad_rear,
            in_bad_front,
            in_empty,
            in_rear_one,
            in_rear_two,
            in_front_one,
            in_front_two,
            in_split_two,
            out_bad_atom,
            out_bad_tuple0,
            out_bad_tuple1,
            out_bad_tuple3,
            out_bad_rear,
            out_bad_front,
            out_empty,
            out_rear_one,
            out_rear_two,
            out_front_one,
            out_front_two,
            out_split_two,
            to_list_bad_atom,
            to_list_bad_tuple0,
            to_list_bad_tuple1,
            to_list_bad_tuple3,
            to_list_bad_rear,
            to_list_bad_front,
            to_list_empty,
            to_list_rear_one,
            to_list_rear_two,
            to_list_front_one,
            to_list_front_two,
            to_list_split_two,
            is_queue_nil,
            is_queue_integer,
            is_queue_float,
            is_queue_binary,
            is_queue_map,
            is_queue_list,
            is_queue_improper_value,
            is_queue_improper_rear_one,
            is_queue_improper_front_one,
            is_queue_improper_both,
            is_empty_improper_rear_one,
            is_empty_improper_front_one,
            is_empty_improper_both,
            len_improper_rear_one,
            len_improper_rear_three,
            len_improper_front_one,
            len_improper_front_three,
            len_improper_both,
            in_improper_rear_one,
            in_improper_front_one,
            in_improper_both,
            out_improper_rear_one,
            out_improper_rear_three,
            out_improper_front_one,
            out_improper_front_three,
            out_improper_both,
            to_list_improper_rear_one,
            to_list_improper_rear_three,
            to_list_improper_front_one,
            to_list_improper_front_three,
            to_list_improper_both,
            out_rear_3,
            out_rear_4,
            out_rear_5,
            out_rear_32,
            out_rear_33,
            out_rear_257,
            out_r2f_0,
            out_r2f_1,
            out_r2f_2,
            out_r2f_3,
            out_r2f_4,
            out_r2f_5,
            out_r2f_32,
            out_r2f_33,
            out_r2f_improper_one,
            out_r2f_improper_three,
            from_list_0,
            from_list_1,
            from_list_2,
            from_list_3,
            from_list_4,
            from_list_5,
            from_list_32,
            from_list_33,
            from_list_256,
            from_list_257,
            from_list_improper_1,
            from_list_improper_2,
            from_list_improper_3,
            from_list_improper_257,
            from_list_bad_atom,
            from_list_bad_tuple,
            from_list_bad_map,
            from_list_bad_integer,
            len_long_improper_rear,
            to_list_long_improper_rear,
            len_long_improper_front,
            to_list_long_improper_front,
            len_long_improper_both,
            to_list_long_improper_both,
            legacy_empty,
            legacy_representation,
            legacy_ordering,
            legacy_persistence,
            legacy_heterogeneous,
            legacy_errors,
            legacy_improper,
            legacy_raw_shapes,
            legacy_raw_observer,
            sequence_257,
            roundtrip_257,
            caller_explicit,
            caller_length,
            caller_reverse,
            opaque_raw,
            opaque_relationships].
run() -> [{Name,value(Name)} || Name <- cases()].
value(Name) ->
    try probe(Name) of Value -> {ok,Value}
    catch Class:Reason -> {error,Class,Reason} end.
run_stacks() ->
    Names = cases(),
    Default = {default,[{Name,capture(Name)} || Name <- Names]},
    Old = erlang:system_flag(backtrace_depth,8),
    try [Default | [at_depth(Depth,Names) || Depth <- [0,1,2,8]]]
    after erlang:system_flag(backtrace_depth,Old) end.
at_depth(Depth,Names) ->
    erlang:system_flag(backtrace_depth,Depth),
    {Depth,[{Name,capture(Name)} || Name <- Names]}.
capture(Name) ->
    try probe(Name) of Value -> {ok,Value}
    catch Class:Reason:Stack -> {error,Class,Reason,Stack} end.
probe(new) -> queue:new();
probe(is_queue_bad_atom) -> queue:is_queue(bad);
probe(is_queue_bad_tuple0) -> queue:is_queue({});
probe(is_queue_bad_tuple1) -> queue:is_queue({[]});
probe(is_queue_bad_tuple3) -> queue:is_queue({[],[],[]});
probe(is_queue_bad_rear) -> queue:is_queue({bad,[]});
probe(is_queue_bad_front) -> queue:is_queue({[],bad});
probe(is_queue_empty) -> queue:is_queue({[],[]});
probe(is_queue_rear_one) -> queue:is_queue({[a],[]});
probe(is_queue_rear_two) -> queue:is_queue({[b,a],[]});
probe(is_queue_front_one) -> queue:is_queue({[],[a]});
probe(is_queue_front_two) -> queue:is_queue({[],[a,b]});
probe(is_queue_split_two) -> queue:is_queue({[b],[a]});
probe(is_empty_bad_atom) -> queue:is_empty(bad);
probe(is_empty_bad_tuple0) -> queue:is_empty({});
probe(is_empty_bad_tuple1) -> queue:is_empty({[]});
probe(is_empty_bad_tuple3) -> queue:is_empty({[],[],[]});
probe(is_empty_bad_rear) -> queue:is_empty({bad,[]});
probe(is_empty_bad_front) -> queue:is_empty({[],bad});
probe(is_empty_empty) -> queue:is_empty({[],[]});
probe(is_empty_rear_one) -> queue:is_empty({[a],[]});
probe(is_empty_rear_two) -> queue:is_empty({[b,a],[]});
probe(is_empty_front_one) -> queue:is_empty({[],[a]});
probe(is_empty_front_two) -> queue:is_empty({[],[a,b]});
probe(is_empty_split_two) -> queue:is_empty({[b],[a]});
probe(len_bad_atom) -> queue:len(bad);
probe(len_bad_tuple0) -> queue:len({});
probe(len_bad_tuple1) -> queue:len({[]});
probe(len_bad_tuple3) -> queue:len({[],[],[]});
probe(len_bad_rear) -> queue:len({bad,[]});
probe(len_bad_front) -> queue:len({[],bad});
probe(len_empty) -> queue:len({[],[]});
probe(len_rear_one) -> queue:len({[a],[]});
probe(len_rear_two) -> queue:len({[b,a],[]});
probe(len_front_one) -> queue:len({[],[a]});
probe(len_front_two) -> queue:len({[],[a,b]});
probe(len_split_two) -> queue:len({[b],[a]});
probe(in_bad_atom) -> queue:in(item,bad);
probe(in_bad_tuple0) -> queue:in(item,{});
probe(in_bad_tuple1) -> queue:in(item,{[]});
probe(in_bad_tuple3) -> queue:in(item,{[],[],[]});
probe(in_bad_rear) -> queue:in(item,{bad,[]});
probe(in_bad_front) -> queue:in(item,{[],bad});
probe(in_empty) -> queue:in(item,{[],[]});
probe(in_rear_one) -> queue:in(item,{[a],[]});
probe(in_rear_two) -> queue:in(item,{[b,a],[]});
probe(in_front_one) -> queue:in(item,{[],[a]});
probe(in_front_two) -> queue:in(item,{[],[a,b]});
probe(in_split_two) -> queue:in(item,{[b],[a]});
probe(out_bad_atom) -> queue:out(bad);
probe(out_bad_tuple0) -> queue:out({});
probe(out_bad_tuple1) -> queue:out({[]});
probe(out_bad_tuple3) -> queue:out({[],[],[]});
probe(out_bad_rear) -> queue:out({bad,[]});
probe(out_bad_front) -> queue:out({[],bad});
probe(out_empty) -> queue:out({[],[]});
probe(out_rear_one) -> queue:out({[a],[]});
probe(out_rear_two) -> queue:out({[b,a],[]});
probe(out_front_one) -> queue:out({[],[a]});
probe(out_front_two) -> queue:out({[],[a,b]});
probe(out_split_two) -> queue:out({[b],[a]});
probe(to_list_bad_atom) -> queue:to_list(bad);
probe(to_list_bad_tuple0) -> queue:to_list({});
probe(to_list_bad_tuple1) -> queue:to_list({[]});
probe(to_list_bad_tuple3) -> queue:to_list({[],[],[]});
probe(to_list_bad_rear) -> queue:to_list({bad,[]});
probe(to_list_bad_front) -> queue:to_list({[],bad});
probe(to_list_empty) -> queue:to_list({[],[]});
probe(to_list_rear_one) -> queue:to_list({[a],[]});
probe(to_list_rear_two) -> queue:to_list({[b,a],[]});
probe(to_list_front_one) -> queue:to_list({[],[a]});
probe(to_list_front_two) -> queue:to_list({[],[a,b]});
probe(to_list_split_two) -> queue:to_list({[b],[a]});
probe(is_queue_nil) -> queue:is_queue([]);
probe(is_queue_integer) -> queue:is_queue(123);
probe(is_queue_float) -> queue:is_queue(1.5);
probe(is_queue_binary) -> queue:is_queue(<<1,2>>);
probe(is_queue_map) -> queue:is_queue(#{a=>1});
probe(is_queue_list) -> queue:is_queue([a,b]);
probe(is_queue_improper_value) -> queue:is_queue([a|tail]);
probe(is_queue_improper_rear_one) -> queue:is_queue({[a|tail],[]});
probe(is_queue_improper_front_one) -> queue:is_queue({[],[a|tail]});
probe(is_queue_improper_both) -> queue:is_queue({[a,b|rear_tail],[c,d|front_tail]});
probe(is_empty_improper_rear_one) -> queue:is_empty({[a|tail],[]});
probe(is_empty_improper_front_one) -> queue:is_empty({[],[a|tail]});
probe(is_empty_improper_both) -> queue:is_empty({[a,b|rear_tail],[c,d|front_tail]});
probe(len_improper_rear_one) -> queue:len({[a|tail],[]});
probe(len_improper_rear_three) -> queue:len({[a,b,c|tail],[]});
probe(len_improper_front_one) -> queue:len({[],[a|tail]});
probe(len_improper_front_three) -> queue:len({[],[a,b,c|tail]});
probe(len_improper_both) -> queue:len({[a,b|rear_tail],[c,d|front_tail]});
probe(in_improper_rear_one) -> queue:in(item,{[a|tail],[]});
probe(in_improper_front_one) -> queue:in(item,{[],[a|tail]});
probe(in_improper_both) -> queue:in(item,{[a,b|rear_tail],[c,d|front_tail]});
probe(out_improper_rear_one) -> queue:out({[a|tail],[]});
probe(out_improper_rear_three) -> queue:out({[a,b,c|tail],[]});
probe(out_improper_front_one) -> queue:out({[],[a|tail]});
probe(out_improper_front_three) -> queue:out({[],[a,b,c|tail]});
probe(out_improper_both) -> queue:out({[a,b|rear_tail],[c,d|front_tail]});
probe(to_list_improper_rear_one) -> queue:to_list({[a|tail],[]});
probe(to_list_improper_rear_three) -> queue:to_list({[a,b,c|tail],[]});
probe(to_list_improper_front_one) -> queue:to_list({[],[a|tail]});
probe(to_list_improper_front_three) -> queue:to_list({[],[a,b,c|tail]});
probe(to_list_improper_both) -> queue:to_list({[a,b|rear_tail],[c,d|front_tail]});
probe(out_rear_3) -> queue:out({lists:seq(1,3),[]});
probe(out_rear_4) -> queue:out({lists:seq(1,4),[]});
probe(out_rear_5) -> queue:out({lists:seq(1,5),[]});
probe(out_rear_32) -> queue:out({lists:seq(1,32),[]});
probe(out_rear_33) -> queue:out({lists:seq(1,33),[]});
probe(out_rear_257) -> queue:out({lists:seq(1,257),[]});
probe(out_r2f_0) -> queue:out({lists:seq(1,0),[front]});
probe(out_r2f_1) -> queue:out({lists:seq(1,1),[front]});
probe(out_r2f_2) -> queue:out({lists:seq(1,2),[front]});
probe(out_r2f_3) -> queue:out({lists:seq(1,3),[front]});
probe(out_r2f_4) -> queue:out({lists:seq(1,4),[front]});
probe(out_r2f_5) -> queue:out({lists:seq(1,5),[front]});
probe(out_r2f_32) -> queue:out({lists:seq(1,32),[front]});
probe(out_r2f_33) -> queue:out({lists:seq(1,33),[front]});
probe(out_r2f_improper_one) -> queue:out({[a|tail],[front]});
probe(out_r2f_improper_three) -> queue:out({[a,b,c|tail],[front]});
probe(from_list_0) -> queue:from_list(lists:seq(1,0));
probe(from_list_1) -> queue:from_list(lists:seq(1,1));
probe(from_list_2) -> queue:from_list(lists:seq(1,2));
probe(from_list_3) -> queue:from_list(lists:seq(1,3));
probe(from_list_4) -> queue:from_list(lists:seq(1,4));
probe(from_list_5) -> queue:from_list(lists:seq(1,5));
probe(from_list_32) -> queue:from_list(lists:seq(1,32));
probe(from_list_33) -> queue:from_list(lists:seq(1,33));
probe(from_list_256) -> queue:from_list(lists:seq(1,256));
probe(from_list_257) -> queue:from_list(lists:seq(1,257));
probe(from_list_improper_1) -> queue:from_list(bad_list(1));
probe(from_list_improper_2) -> queue:from_list(bad_list(2));
probe(from_list_improper_3) -> queue:from_list(bad_list(3));
probe(from_list_improper_257) -> queue:from_list(bad_list(257));
probe(from_list_bad_atom) -> queue:from_list(tail);
probe(from_list_bad_tuple) -> queue:from_list({a,b});
probe(from_list_bad_map) -> queue:from_list(#{a=>1});
probe(from_list_bad_integer) -> queue:from_list(123);
probe(len_long_improper_rear) -> queue:len({bad_list(257),[]});
probe(to_list_long_improper_rear) -> queue:to_list({bad_list(257),[]});
probe(len_long_improper_front) -> queue:len({[],bad_list(257)});
probe(to_list_long_improper_front) -> queue:to_list({[],bad_list(257)});
probe(len_long_improper_both) -> queue:len({bad_list(257),bad_list(257)});
probe(to_list_long_improper_both) -> queue:to_list({bad_list(257),bad_list(257)});
probe(legacy_empty) -> queue_core_empty:run();
probe(legacy_representation) -> queue_core_representation:run();
probe(legacy_ordering) -> queue_core_ordering:run();
probe(legacy_persistence) -> queue_core_persistence:run();
probe(legacy_heterogeneous) -> queue_core_heterogeneous:run();
probe(legacy_errors) -> queue_core_errors:run();
probe(legacy_improper) -> queue_core_improper:run();
probe(legacy_raw_shapes) -> queue_core_raw_shapes:run();
probe(legacy_raw_observer) -> queue_core_observe:run();
probe(sequence_257) -> sequence();
probe(roundtrip_257) -> roundtrip();
probe(caller_explicit) -> caller_a(bad,explicit);
probe(caller_length) -> caller_a({[a,b,c|tail],[]},length);
probe(caller_reverse) -> caller_a({[a,b,c|tail],[]},reverse);
probe(opaque_raw) -> opaque_raw();
probe(opaque_relationships) -> opaque_relationships().
bad_list(Count) -> lists:seq(1,Count) ++ tail.
enqueue([],Queue) -> Queue;
enqueue([Value|Values],Queue) -> enqueue(Values,queue:in(Value,Queue)).
drain(Queue,Values) ->
    case queue:out(Queue) of
        {empty,Empty} -> {lists:reverse(Values),Empty};
        {{value,Value},Next} -> drain(Next,[Value|Values])
    end.
sequence() ->
    Q0 = queue:new(), Q1 = queue:in(first,Q0), Q2 = queue:in(second,Q1),
    Q257 = enqueue(lists:seq(1,257),Q0),
    {Q0,Q1,Q2,Q257,drain(Q257,[]),queue:to_list(Q0),queue:to_list(Q1),queue:to_list(Q2)}.
roundtrip() ->
    Values = lists:seq(1,257), Queue = queue:from_list(Values),
    {Queue,queue:len(Queue),queue:to_list(Queue),drain(Queue,[])}.
caller_a(Queue,Kind) ->
    Result = caller_b(Queue,Kind),
    {caller_a_return,Result}.
caller_b(Queue,Kind) ->
    Result = caller_c(Queue,Kind),
    {caller_b_return,Result}.
caller_c(Queue,explicit) ->
    Result = queue:len(Queue), {caller_c_return,Result};
caller_c(Queue,length) ->
    Result = queue:len(Queue), {caller_c_return,Result};
caller_c(Queue,reverse) ->
    Result = queue:out(Queue), {caller_c_return,Result}.
local_identity(Value) -> Value.
opaque_values() ->
    Pid = self(), Ref = make_ref(), Local = fun local_identity/1, External = fun erlang:abs/1,
    [Pid,Ref,Local,External,#{opaque=>Ref},<<5:3>>,[a|tail],{1,1.0},1 bsl 128,
     external_port(),opaque_atom,[],1.5,-(1 bsl 128)].
opaque_raw() ->
    Values = opaque_values(), Queue = queue:from_list(Values),
    {Values,Queue,queue:to_list(Queue),drain(Queue,[])}.
opaque_relationships() ->
    Values = opaque_values(), Queue = queue:from_list(Values),
    {Removed,Empty} = drain(Queue,[]),
    Local = lists:nth(3,Values), External = lists:nth(4,Values),
    {queue:is_queue(Queue),queue:is_empty(Queue),queue:len(Queue),
     queue:to_list(Queue) =:= Values,Removed =:= Values,queue:is_empty(Empty),
     lists:nth(1,Removed) =:= self(),lists:nth(2,Removed) =:= lists:nth(2,Values),
     lists:nth(3,Removed) =:= Local,lists:nth(4,Removed) =:= External,
     Local(identity_value),External(-7),queue:to_list(Queue) =:= Values}.
external_port() ->
    <<131,Node/binary>> = term_to_binary('queue_opaque@remote'),
    binary_to_term(<<131,102,Node/binary,17:32,1:8>>).
