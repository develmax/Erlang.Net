-module(ordinary_reference563).
-export([run/0]).

run() ->
    [{wordsize, erlang:system_info(wordsize)},
     {ordinary_make_ref, outcome(fun ordinary/0)},
     {etf_roundtrip_boundary, outcome(fun roundtrip/0)}].

ordinary() ->
    Ref = erlang:make_ref(),
    Other = erlang:make_ref(),
    SharedTuple = {Ref, Ref},
    IndependentTuple = {Ref, Other},
    SharedList = [Ref, Ref],
    IndependentList = [Ref, Other],
    SharedMap = #{first => Ref, second => Ref},
    IndependentMap = #{first => Ref, second => Other},
    #{first := First, second := Second} = SharedMap,
    [{is_reference, is_reference(Ref)},
     {self_same, erts_debug:same(Ref, Ref)},
     {distinct_identity, Ref =/= Other},
     {independent_same, erts_debug:same(Ref, Other)},
     {scalar_sizes, sizes(Ref)},
     {shared_tuple_sizes, sizes(SharedTuple)},
     {independent_tuple_sizes, sizes(IndependentTuple)},
     {shared_list_sizes, sizes(SharedList)},
     {independent_list_sizes, sizes(IndependentList)},
     {shared_map_sizes, sizes(SharedMap)},
     {independent_map_sizes, sizes(IndependentMap)},
     {shared_map_values_same, erts_debug:same(First, Second)}].

roundtrip() ->
    Ref = erlang:make_ref(),
    Encoded = term_to_binary(Ref),
    Decoded = binary_to_term(Encoded),
    SafeDecoded = binary_to_term(Encoded, [safe]),
    {Left, Right} = binary_to_term(term_to_binary({Ref, Ref})),
    [{encoded_is_binary, is_binary(Encoded)},
     {decoded_is_reference, is_reference(Decoded)},
     {decoded_identity_equal, Ref =:= Decoded},
     {safe_decoded_identity_equal, Ref =:= SafeDecoded},
     {reencoded_bytes_equal_in_this_vm, term_to_binary(Decoded) =:= Encoded},
     {decoded_physical_same, erts_debug:same(Ref, Decoded)},
     {safe_decoded_physical_same, erts_debug:same(Ref, SafeDecoded)},
     {decoded_scalar_sizes, sizes(Decoded)},
     {decoded_pair_identity_equal, Left =:= Right},
     {decoded_pair_physical_same, erts_debug:same(Left, Right)},
     {decoded_pair_original_identity_equal, Ref =:= Left},
     {decoded_pair_original_physical_same, erts_debug:same(Ref, Left)},
     {decoded_pair_sizes, sizes({Left, Right})}].

sizes(Value) ->
    [{flat, erts_debug:flat_size(Value)},
     {shared, erts_debug:size_shared(Value)}].

outcome(Fun) ->
    try Fun() of
        Value -> {returned, Value}
    catch
        Class:Reason:Stack -> {raised, Class, Reason, Stack}
    end.
