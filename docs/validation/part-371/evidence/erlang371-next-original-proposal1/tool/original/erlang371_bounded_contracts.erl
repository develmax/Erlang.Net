-module(erlang371_bounded_contracts).
-export([cases/0,run/0,run_stacks/0,probe/1]).
cases() -> [hd_guard_accept,hd_guard_reject,hd_guard_native_record,hd_non_tail_success,hd_non_tail_badarg,hd_invalid_pid,hd_invalid_reference,hd_invalid_function,hd_invalid_tuple,hd_identity_payload,tl_guard_accept,tl_guard_reject,tl_guard_native_record,tl_non_tail_success,tl_non_tail_badarg,tl_invalid_pid,tl_invalid_reference,tl_invalid_function,tl_invalid_tuple,tl_identity_payload,tuple_size_guard_accept,tuple_size_guard_reject,tuple_size_guard_native_record,tuple_size_non_tail_success,tuple_size_non_tail_badarg,tuple_size_arity_257,is_integer_guard_inside,is_integer_guard_outside,is_integer_guard_reversed,is_integer_guard_invalid_min,is_integer_guard_invalid_max,is_integer_guard_both_invalid,is_integer_guard_native_record,is_integer_non_tail_success,is_integer_non_tail_invalid_min,is_integer_non_tail_invalid_max,is_integer_non_tail_both_invalid,is_integer_big_equal,is_integer_big_below,is_integer_big_above,is_integer_noninteger_nil,is_integer_noninteger_cons,is_integer_noninteger_tuple,is_integer_noninteger_map,is_integer_noninteger_binary,is_integer_noninteger_bits,is_integer_noninteger_pid,is_integer_noninteger_reference,is_integer_noninteger_function,is_integer_noninteger_native_record].
run() -> erlang370_pure_support:run(erlang371_bounded_contracts,cases()).
run_stacks() -> erlang370_pure_support:profiles(erlang371_bounded_contracts,cases()).
probe(hd_guard_accept) -> guard_hd([sentinel|sentinel]);
probe(hd_guard_reject) -> guard_hd([]);
probe(hd_guard_native_record) -> guard_hd(records:create(bounded_owner,rec,[{value,1}],#{is_exported=>true}));
probe(hd_non_tail_success) -> context_hd([sentinel|sentinel]);
probe(hd_non_tail_badarg) -> context_hd(invalid);
probe(hd_invalid_pid) -> erlang370_pure_support:invoke(hd,[binary_to_term(<<131,88,119,13,"external@host",0,0,0,1,0,0,0,0,0,0,0,0>>)]);
probe(hd_invalid_reference) -> erlang370_pure_support:invoke(hd,[binary_to_term(<<131,90,0,1,119,13,"external@host",0,0,0,0,0,0,0,1>>)]);
probe(hd_invalid_function) -> erlang370_pure_support:invoke(hd,[fun erlang:self/0]);
probe(hd_invalid_tuple) -> erlang370_pure_support:invoke(hd,[{a,b}]);
probe(hd_identity_payload) -> begin P=binary_to_term(<<131,88,119,13,"external@host",0,0,0,1,0,0,0,0,0,0,0,0>>), R=binary_to_term(<<131,90,0,1,119,13,"external@host",0,0,0,0,0,0,0,1>>), F=fun erlang:self/0, erlang370_pure_support:invoke(hd,[[{P,R,F}|{P,R,F}]]) end;
probe(tl_guard_accept) -> guard_tl([sentinel|sentinel]);
probe(tl_guard_reject) -> guard_tl([]);
probe(tl_guard_native_record) -> guard_tl(records:create(bounded_owner,rec,[{value,1}],#{is_exported=>true}));
probe(tl_non_tail_success) -> context_tl([sentinel|sentinel]);
probe(tl_non_tail_badarg) -> context_tl(invalid);
probe(tl_invalid_pid) -> erlang370_pure_support:invoke(tl,[binary_to_term(<<131,88,119,13,"external@host",0,0,0,1,0,0,0,0,0,0,0,0>>)]);
probe(tl_invalid_reference) -> erlang370_pure_support:invoke(tl,[binary_to_term(<<131,90,0,1,119,13,"external@host",0,0,0,0,0,0,0,1>>)]);
probe(tl_invalid_function) -> erlang370_pure_support:invoke(tl,[fun erlang:self/0]);
probe(tl_invalid_tuple) -> erlang370_pure_support:invoke(tl,[{a,b}]);
probe(tl_identity_payload) -> begin P=binary_to_term(<<131,88,119,13,"external@host",0,0,0,1,0,0,0,0,0,0,0,0>>), R=binary_to_term(<<131,90,0,1,119,13,"external@host",0,0,0,0,0,0,0,1>>), F=fun erlang:self/0, erlang370_pure_support:invoke(tl,[[{P,R,F}|{P,R,F}]]) end;
probe(tuple_size_guard_accept) -> guard_tuple_size({a,b});
probe(tuple_size_guard_reject) -> guard_tuple_size([]);
probe(tuple_size_guard_native_record) -> guard_tuple_size(records:create(bounded_owner,rec,[{value,1}],#{is_exported=>true}));
probe(tuple_size_non_tail_success) -> context_tuple_size({a,b});
probe(tuple_size_non_tail_badarg) -> context_tuple_size(invalid);
probe(tuple_size_arity_257) -> erlang370_pure_support:invoke(tuple_size,[erlang:make_tuple(257,payload)]);
probe(is_integer_guard_inside) -> guard_integer(5,0,10);
probe(is_integer_guard_outside) -> guard_integer(11,0,10);
probe(is_integer_guard_reversed) -> guard_integer(5,10,0);
probe(is_integer_guard_invalid_min) -> guard_integer(5,invalid,10);
probe(is_integer_guard_invalid_max) -> guard_integer(5,0,invalid);
probe(is_integer_guard_both_invalid) -> guard_integer(atom,invalid,invalid);
probe(is_integer_guard_native_record) -> guard_integer(records:create(bounded_owner,rec,[{value,1}],#{is_exported=>true}),0,10);
probe(is_integer_non_tail_success) -> context_integer(5,0,10);
probe(is_integer_non_tail_invalid_min) -> context_integer(atom,invalid,10);
probe(is_integer_non_tail_invalid_max) -> context_integer(atom,0,invalid);
probe(is_integer_non_tail_both_invalid) -> context_integer(atom,invalid,invalid);
probe(is_integer_big_equal) -> erlang370_pure_support:invoke(is_integer,[1 bsl 4096,1 bsl 4096,1 bsl 4096]);
probe(is_integer_big_below) -> erlang370_pure_support:invoke(is_integer,[-(1 bsl 4096),-(1 bsl 4096)+1,1 bsl 4096]);
probe(is_integer_big_above) -> erlang370_pure_support:invoke(is_integer,[1 bsl 4096,-(1 bsl 4096),(1 bsl 4096)-1]);
probe(is_integer_noninteger_nil) -> erlang370_pure_support:invoke(is_integer,[[],-(1 bsl 4096),1 bsl 4096]);
probe(is_integer_noninteger_cons) -> erlang370_pure_support:invoke(is_integer,[[a|tail],-(1 bsl 4096),1 bsl 4096]);
probe(is_integer_noninteger_tuple) -> erlang370_pure_support:invoke(is_integer,[{},-(1 bsl 4096),1 bsl 4096]);
probe(is_integer_noninteger_map) -> erlang370_pure_support:invoke(is_integer,[#{},-(1 bsl 4096),1 bsl 4096]);
probe(is_integer_noninteger_binary) -> erlang370_pure_support:invoke(is_integer,[<<>>,-(1 bsl 4096),1 bsl 4096]);
probe(is_integer_noninteger_bits) -> erlang370_pure_support:invoke(is_integer,[<<1:1>>,-(1 bsl 4096),1 bsl 4096]);
probe(is_integer_noninteger_pid) -> erlang370_pure_support:invoke(is_integer,[binary_to_term(<<131,88,119,13,"external@host",0,0,0,1,0,0,0,0,0,0,0,0>>),-(1 bsl 4096),1 bsl 4096]);
probe(is_integer_noninteger_reference) -> erlang370_pure_support:invoke(is_integer,[binary_to_term(<<131,90,0,1,119,13,"external@host",0,0,0,0,0,0,0,1>>),-(1 bsl 4096),1 bsl 4096]);
probe(is_integer_noninteger_function) -> erlang370_pure_support:invoke(is_integer,[fun erlang:self/0,-(1 bsl 4096),1 bsl 4096]);
probe(is_integer_noninteger_native_record) -> erlang370_pure_support:invoke(is_integer,[records:create(bounded_owner,rec,[{value,1}],#{is_exported=>true}),-(1 bsl 4096),1 bsl 4096]).
guard_hd(V) when erlang:hd(V) =:= sentinel -> true;
guard_hd(_) -> false.
guard_tl(V) when erlang:tl(V) =:= sentinel -> true;
guard_tl(_) -> false.
guard_tuple_size(V) when erlang:tuple_size(V) >= 0 -> true;
guard_tuple_size(_) -> false.
guard_integer(V,L,U) when erlang:is_integer(V,L,U) -> true;
guard_integer(_,_,_) -> false.
context_hd(V) -> Result=erlang:hd(V), {returned,Result}.
context_tl(V) -> Result=erlang:tl(V), {returned,Result}.
context_tuple_size(V) -> Result=erlang:tuple_size(V), {returned,Result}.
context_integer(V,L,U) -> Result=erlang:is_integer(V,L,U), {returned,Result}.
