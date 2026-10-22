-module(beam_hamt_native430).
-export([run/0,run_stacks/0,worker/2,own/0,profile/0,capture/1]).

cases() ->
    [{Kind,N} || Kind <- [info,children_shape], N <- [0,1,32,33,64,256,1024]] ++
    [{type,Name} || Name <- [atom,nil,list,improper,empty_tuple,tuple,float,zero,
        small_min,small_max,big_negative,big_positive,flat_map,hash_map,
        local_fun,export_fun,pid,reference,external_pid,external_reference,
        port,external_port,heap_binary,reference_binary,small_reference_slice,
        large_reference_slice,bitstring,large_bits,internal_node]].

run() -> [{Name,observe(Name)} || Name <- cases()].

run_stacks() ->
    Old = erlang:system_flag(backtrace_depth,8),
    erlang:system_flag(backtrace_depth,Old),
    try [{default,Old,run()} | [depth(D) || D <- [0,1,2,8]]]
    after erlang:system_flag(backtrace_depth,Old) end.

depth(D) ->
    erlang:system_flag(backtrace_depth,D),
    Effective = erlang:system_flag(backtrace_depth,D),
    {D,Effective,run()}.

observe(Name) ->
    {Pid,Ref} = spawn_monitor(?MODULE,worker,[self(),Name]),
    receive {Name,Outcome} -> receive {'DOWN',Ref,process,Pid,normal} -> Outcome end
    after 15000 ->
        exit(Pid,kill),receive {'DOWN',Ref,process,Pid,_} -> ok end,
        exit({worker_timeout,Name})
    end.

worker(Parent,Name) ->
    Outcome = try {value,perform(Name)} catch Class:Reason:Stack -> {exception,Class,Reason,Stack} end,
    Parent ! {Name,Outcome}.

perform({info,N}) -> erts_debug:map_info(map(N));
perform({children_shape,N}) -> shape(erts_internal:map_hashmap_children(map(N)));
perform({type,Name}) -> erts_internal:term_type(argument(Name)).

map(N) -> maps:from_list([{I,{I,payload}} || I <- lists:seq(1,N)]).

shape([]) -> [];
shape([Child|Rest]) ->
    [case erts_internal:term_type(Child) of
         list -> leaf(Child);
         hashmap_node -> {hashmap_node,shape(erts_internal:map_hashmap_children(Child))};
         tuple -> {collision,[leaf(Pair) || Pair <- tuple_to_list(Child)]}
     end | shape(Rest)].

leaf([Key|Value]) -> {leaf,Key,Value}.

argument(atom) -> hello;
argument(nil) -> [];
argument(list) -> [1,2];
argument(improper) -> [1|2];
argument(empty_tuple) -> {};
argument(tuple) -> {1,2};
argument(float) -> 1.25;
argument(zero) -> 0;
argument(small_min) -> -(1 bsl 59);
argument(small_max) -> (1 bsl 59)-1;
argument(big_negative) -> -(1 bsl 59)-1;
argument(big_positive) -> 1 bsl 59;
argument(flat_map) -> map(2);
argument(hash_map) -> map(33);
argument(local_fun) -> fun() -> own end;
argument(export_fun) -> fun erlang:self/0;
argument(pid) -> self();
argument(reference) -> make_ref();
argument(external_pid) -> previous_creation(self());
argument(external_reference) -> previous_creation(make_ref());
argument(port) -> list_to_port("#Port<0.1>");
argument(external_port) -> previous_creation(list_to_port("#Port<0.1>"));
argument(heap_binary) -> binary:copy(<<42>>,32);
argument(reference_binary) -> binary:copy(<<42>>,128);
argument(small_reference_slice) -> binary:part(binary:copy(<<42>>,256),{1,16});
argument(large_reference_slice) -> binary:part(binary:copy(<<42>>,256),{1,128});
argument(bitstring) -> <<1:1>>;
argument(large_bits) -> <<(binary:copy(<<42>>,128))/binary,1:1>>;
argument(internal_node) ->
    hd([Child || Child <- erts_internal:map_hashmap_children(map(256)),is_map(Child)]).

previous_creation(Reference) when is_reference(Reference) ->
    Binary=term_to_binary(Reference),
    <<131,90,Words:16,_/binary>>=Binary,
    N=byte_size(Binary)-Words*4-4,
    <<Prefix:N/binary,Creation:32,Tail/binary>>=Binary,
    binary_to_term(<<Prefix/binary,(Creation bxor 1):32,Tail/binary>>);
previous_creation(Term) ->
    Binary = term_to_binary(Term),N = byte_size(Binary)-4,
    <<Prefix:N/binary,Creation:32>> = Binary,
    binary_to_term(<<Prefix/binary,(Creation bxor 1):32>>).

own() ->
    Map = map(256),
    Children = erts_internal:map_hashmap_children(Map),
    Again = erts_internal:map_hashmap_children(Map),
    Nodes = [Child || Child <- Children,is_map(Child)],
    {lists:all(fun({A,B}) -> erts_debug:same(A,B) end,lists:zip(Children,Again)),
     length(Nodes)>0,
     lists:all(fun(Node) -> erts_internal:term_type(Node)=:=hashmap_node end,Nodes),
     lists:sort(leaves(Children))=:=lists:sort(maps:to_list(Map))}.

leaves([]) -> [];
leaves([Child|Rest]) ->
    Here = case erts_internal:term_type(Child) of
               list -> [begin [K|V]=Child,{K,V} end];
               hashmap_node -> leaves(erts_internal:map_hashmap_children(Child));
               tuple -> leaves(tuple_to_list(Child))
           end,
    Here ++ leaves(Rest).

profile() -> {erlang:system_info(wordsize),erlang:system_info(otp_release)}.

capture([Directory]) ->
    ok=prim_file:write_file(Directory++"/profile.etf",term_to_binary(profile(),[{minor_version,2}])),
    ok=prim_file:write_file(Directory++"/values.etf",term_to_binary(run(),[{minor_version,2}])),
    ok=prim_file:write_file(Directory++"/stacks.etf",term_to_binary(run_stacks(),[{minor_version,2}])),
    ok=prim_file:write_file(Directory++"/own.etf",term_to_binary(own(),[{minor_version,2}])),
    erlang:halt(0).
