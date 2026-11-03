-module(loader_map_keys565).
-export([run/0,literal/1,dynamic_source/2,dynamic_key/2]).
-compile({no_inline,[literal/1,dynamic_source/2,dynamic_key/2]}).

literal(R) -> #{first => R, second => R}.
dynamic_source(M,R) -> M#{first => R, second => R}.
dynamic_key(K,R) -> #{K => R}.

run() ->
    R = make_ref(),
    A = literal(R),
    B = literal(R),
    C = dynamic_source(#{},R),
    D = dynamic_key(first,R),
    E = loader_map_other565:literal(R),
    [{literal_sizes,{erts_debug:flat_size(A),erts_debug:size_shared(A)}},
     {dynamic_source_sizes,{erts_debug:flat_size(C),erts_debug:size_shared(C)}},
     {dynamic_key_sizes,{erts_debug:flat_size(D),erts_debug:size_shared(D)}},
     {same_image_key_tuple,erts_debug:same(erts_internal:map_to_tuple_keys(A),erts_internal:map_to_tuple_keys(B))},
     {dynamic_source_key_tuple,erts_debug:same(erts_internal:map_to_tuple_keys(A),erts_internal:map_to_tuple_keys(C))},
     {cross_image_key_tuple,erts_debug:same(erts_internal:map_to_tuple_keys(A),erts_internal:map_to_tuple_keys(E))},
     {bindings,{maps:get(first,A)=:=R,maps:get(second,A)=:=R,A=:=C,A=:=E}}].
