-module(ordsets371_callbacks).
-export([cases/0,run/0,run_stacks/0,probe/1]).
-export([fold_callback/2,filter_even/1,map_reverse/1,map_zero/1,filtermap_zero/1,keep_identity/1,filtermap_mixed/1,error_one/1,throw_one/1,exit_one/1,invalid_return_one/1,error_two/2,throw_two/2,exit_two/2,invalid_return_two/2]).
cases() -> [fold_order_0,filter_even_0,map_reverse_0,filtermap_mixed_0,fold_order_1,filter_even_1,map_reverse_1,filtermap_mixed_1,fold_order_2,filter_even_2,map_reverse_2,filtermap_mixed_2,fold_order_3,filter_even_3,map_reverse_3,filtermap_mixed_3,fold_order_4,filter_even_4,map_reverse_4,filtermap_mixed_4,fold_order_5,filter_even_5,map_reverse_5,filtermap_mixed_5,fold_order_6,filter_even_6,map_reverse_6,filtermap_mixed_6,fold_order_7,filter_even_7,map_reverse_7,filtermap_mixed_7,fold_callback_kind_0_set_0,fold_callback_kind_0_set_1,fold_callback_kind_0_set_2,fold_callback_kind_0_set_3,fold_callback_kind_1_set_0,fold_callback_kind_1_set_1,fold_callback_kind_1_set_2,fold_callback_kind_1_set_3,fold_callback_kind_2_set_0,fold_callback_kind_2_set_1,fold_callback_kind_2_set_2,fold_callback_kind_2_set_3,fold_callback_kind_3_set_0,fold_callback_kind_3_set_1,fold_callback_kind_3_set_2,fold_callback_kind_3_set_3,fold_error,fold_throw,fold_exit,fold_invalid_return,filter_callback_kind_0_set_0,filter_callback_kind_0_set_1,filter_callback_kind_0_set_2,filter_callback_kind_0_set_3,filter_callback_kind_1_set_0,filter_callback_kind_1_set_1,filter_callback_kind_1_set_2,filter_callback_kind_1_set_3,filter_callback_kind_2_set_0,filter_callback_kind_2_set_1,filter_callback_kind_2_set_2,filter_callback_kind_2_set_3,filter_callback_kind_3_set_0,filter_callback_kind_3_set_1,filter_callback_kind_3_set_2,filter_callback_kind_3_set_3,filter_error,filter_throw,filter_exit,filter_invalid_return,map_callback_kind_0_set_0,map_callback_kind_0_set_1,map_callback_kind_0_set_2,map_callback_kind_0_set_3,map_callback_kind_1_set_0,map_callback_kind_1_set_1,map_callback_kind_1_set_2,map_callback_kind_1_set_3,map_callback_kind_2_set_0,map_callback_kind_2_set_1,map_callback_kind_2_set_2,map_callback_kind_2_set_3,map_callback_kind_3_set_0,map_callback_kind_3_set_1,map_callback_kind_3_set_2,map_callback_kind_3_set_3,map_error,map_throw,map_exit,map_invalid_return,filtermap_callback_kind_0_set_0,filtermap_callback_kind_0_set_1,filtermap_callback_kind_0_set_2,filtermap_callback_kind_0_set_3,filtermap_callback_kind_1_set_0,filtermap_callback_kind_1_set_1,filtermap_callback_kind_1_set_2,filtermap_callback_kind_1_set_3,filtermap_callback_kind_2_set_0,filtermap_callback_kind_2_set_1,filtermap_callback_kind_2_set_2,filtermap_callback_kind_2_set_3,filtermap_callback_kind_3_set_0,filtermap_callback_kind_3_set_1,filtermap_callback_kind_3_set_2,filtermap_callback_kind_3_set_3,filtermap_error,filtermap_throw,filtermap_exit,filtermap_invalid_return,map_deduplicates,filtermap_deduplicates,filtermap_keep_identity].
run() -> ordsets_core_support:run(ordsets371_callbacks,cases()).
run_stacks() -> ordsets_core_support:profiles(ordsets371_callbacks,cases()).
probe(fold_order_0) -> erlang:apply(ordsets,fold,[fun ordsets371_callbacks:fold_callback/2,[],[]]);
probe(filter_even_0) -> erlang:apply(ordsets,filter,[fun ordsets371_callbacks:filter_even/1,[]]);
probe(map_reverse_0) -> erlang:apply(ordsets,map,[fun ordsets371_callbacks:map_reverse/1,[]]);
probe(filtermap_mixed_0) -> erlang:apply(ordsets,filtermap,[fun ordsets371_callbacks:filtermap_mixed/1,[]]);
probe(fold_order_1) -> erlang:apply(ordsets,fold,[fun ordsets371_callbacks:fold_callback/2,[],[1,2,3,4,5,6]]);
probe(filter_even_1) -> erlang:apply(ordsets,filter,[fun ordsets371_callbacks:filter_even/1,[1,2,3,4,5,6]]);
probe(map_reverse_1) -> erlang:apply(ordsets,map,[fun ordsets371_callbacks:map_reverse/1,[1,2,3,4,5,6]]);
probe(filtermap_mixed_1) -> erlang:apply(ordsets,filtermap,[fun ordsets371_callbacks:filtermap_mixed/1,[1,2,3,4,5,6]]);
probe(fold_order_2) -> erlang:apply(ordsets,fold,[fun ordsets371_callbacks:fold_callback/2,[],[6,5,4,3,2,1]]);
probe(filter_even_2) -> erlang:apply(ordsets,filter,[fun ordsets371_callbacks:filter_even/1,[6,5,4,3,2,1]]);
probe(map_reverse_2) -> erlang:apply(ordsets,map,[fun ordsets371_callbacks:map_reverse/1,[6,5,4,3,2,1]]);
probe(filtermap_mixed_2) -> erlang:apply(ordsets,filtermap,[fun ordsets371_callbacks:filtermap_mixed/1,[6,5,4,3,2,1]]);
probe(fold_order_3) -> erlang:apply(ordsets,fold,[fun ordsets371_callbacks:fold_callback/2,[],[1,1.0,2]]);
probe(filter_even_3) -> erlang:apply(ordsets,filter,[fun ordsets371_callbacks:filter_even/1,[1,1.0,2]]);
probe(map_reverse_3) -> erlang:apply(ordsets,map,[fun ordsets371_callbacks:map_reverse/1,[1,1.0,2]]);
probe(filtermap_mixed_3) -> erlang:apply(ordsets,filtermap,[fun ordsets371_callbacks:filtermap_mixed/1,[1,1.0,2]]);
probe(fold_order_4) -> erlang:apply(ordsets,fold,[fun ordsets371_callbacks:fold_callback/2,[],[a]]);
probe(filter_even_4) -> erlang:apply(ordsets,filter,[fun ordsets371_callbacks:filter_even/1,[a]]);
probe(map_reverse_4) -> erlang:apply(ordsets,map,[fun ordsets371_callbacks:map_reverse/1,[a]]);
probe(filtermap_mixed_4) -> erlang:apply(ordsets,filtermap,[fun ordsets371_callbacks:filtermap_mixed/1,[a]]);
probe(fold_order_5) -> erlang:apply(ordsets,fold,[fun ordsets371_callbacks:fold_callback/2,[],[1|tail]]);
probe(filter_even_5) -> erlang:apply(ordsets,filter,[fun ordsets371_callbacks:filter_even/1,[1|tail]]);
probe(map_reverse_5) -> erlang:apply(ordsets,map,[fun ordsets371_callbacks:map_reverse/1,[1|tail]]);
probe(filtermap_mixed_5) -> erlang:apply(ordsets,filtermap,[fun ordsets371_callbacks:filtermap_mixed/1,[1|tail]]);
probe(fold_order_6) -> erlang:apply(ordsets,fold,[fun ordsets371_callbacks:fold_callback/2,[],tail]);
probe(filter_even_6) -> erlang:apply(ordsets,filter,[fun ordsets371_callbacks:filter_even/1,tail]);
probe(map_reverse_6) -> erlang:apply(ordsets,map,[fun ordsets371_callbacks:map_reverse/1,tail]);
probe(filtermap_mixed_6) -> erlang:apply(ordsets,filtermap,[fun ordsets371_callbacks:filtermap_mixed/1,tail]);
probe(fold_order_7) -> erlang:apply(ordsets,fold,[fun ordsets371_callbacks:fold_callback/2,[],{}]);
probe(filter_even_7) -> erlang:apply(ordsets,filter,[fun ordsets371_callbacks:filter_even/1,{}]);
probe(map_reverse_7) -> erlang:apply(ordsets,map,[fun ordsets371_callbacks:map_reverse/1,{}]);
probe(filtermap_mixed_7) -> erlang:apply(ordsets,filtermap,[fun ordsets371_callbacks:filtermap_mixed/1,{}]);
probe(fold_callback_kind_0_set_0) -> erlang:apply(ordsets,fold,[invalid,initial,[]]);
probe(fold_callback_kind_0_set_1) -> erlang:apply(ordsets,fold,[invalid,initial,[1]]);
probe(fold_callback_kind_0_set_2) -> erlang:apply(ordsets,fold,[invalid,initial,[1,2]]);
probe(fold_callback_kind_0_set_3) -> erlang:apply(ordsets,fold,[invalid,initial,[1|tail]]);
probe(fold_callback_kind_1_set_0) -> erlang:apply(ordsets,fold,[fun erlang:self/0,initial,[]]);
probe(fold_callback_kind_1_set_1) -> erlang:apply(ordsets,fold,[fun erlang:self/0,initial,[1]]);
probe(fold_callback_kind_1_set_2) -> erlang:apply(ordsets,fold,[fun erlang:self/0,initial,[1,2]]);
probe(fold_callback_kind_1_set_3) -> erlang:apply(ordsets,fold,[fun erlang:self/0,initial,[1|tail]]);
probe(fold_callback_kind_2_set_0) -> erlang:apply(ordsets,fold,[fun erlang:abs/1,initial,[]]);
probe(fold_callback_kind_2_set_1) -> erlang:apply(ordsets,fold,[fun erlang:abs/1,initial,[1]]);
probe(fold_callback_kind_2_set_2) -> erlang:apply(ordsets,fold,[fun erlang:abs/1,initial,[1,2]]);
probe(fold_callback_kind_2_set_3) -> erlang:apply(ordsets,fold,[fun erlang:abs/1,initial,[1|tail]]);
probe(fold_callback_kind_3_set_0) -> erlang:apply(ordsets,fold,[fun erlang:'/'/2,initial,[]]);
probe(fold_callback_kind_3_set_1) -> erlang:apply(ordsets,fold,[fun erlang:'/'/2,initial,[1]]);
probe(fold_callback_kind_3_set_2) -> erlang:apply(ordsets,fold,[fun erlang:'/'/2,initial,[1,2]]);
probe(fold_callback_kind_3_set_3) -> erlang:apply(ordsets,fold,[fun erlang:'/'/2,initial,[1|tail]]);
probe(fold_error) -> erlang:apply(ordsets,fold,[fun ordsets371_callbacks:error_two/2,initial,[1,2]]);
probe(fold_throw) -> erlang:apply(ordsets,fold,[fun ordsets371_callbacks:throw_two/2,initial,[1,2]]);
probe(fold_exit) -> erlang:apply(ordsets,fold,[fun ordsets371_callbacks:exit_two/2,initial,[1,2]]);
probe(fold_invalid_return) -> erlang:apply(ordsets,fold,[fun ordsets371_callbacks:invalid_return_two/2,initial,[1,2]]);
probe(filter_callback_kind_0_set_0) -> erlang:apply(ordsets,filter,[invalid,[]]);
probe(filter_callback_kind_0_set_1) -> erlang:apply(ordsets,filter,[invalid,[1]]);
probe(filter_callback_kind_0_set_2) -> erlang:apply(ordsets,filter,[invalid,[1,2]]);
probe(filter_callback_kind_0_set_3) -> erlang:apply(ordsets,filter,[invalid,[1|tail]]);
probe(filter_callback_kind_1_set_0) -> erlang:apply(ordsets,filter,[fun erlang:self/0,[]]);
probe(filter_callback_kind_1_set_1) -> erlang:apply(ordsets,filter,[fun erlang:self/0,[1]]);
probe(filter_callback_kind_1_set_2) -> erlang:apply(ordsets,filter,[fun erlang:self/0,[1,2]]);
probe(filter_callback_kind_1_set_3) -> erlang:apply(ordsets,filter,[fun erlang:self/0,[1|tail]]);
probe(filter_callback_kind_2_set_0) -> erlang:apply(ordsets,filter,[fun erlang:abs/1,[]]);
probe(filter_callback_kind_2_set_1) -> erlang:apply(ordsets,filter,[fun erlang:abs/1,[1]]);
probe(filter_callback_kind_2_set_2) -> erlang:apply(ordsets,filter,[fun erlang:abs/1,[1,2]]);
probe(filter_callback_kind_2_set_3) -> erlang:apply(ordsets,filter,[fun erlang:abs/1,[1|tail]]);
probe(filter_callback_kind_3_set_0) -> erlang:apply(ordsets,filter,[fun erlang:'/'/2,[]]);
probe(filter_callback_kind_3_set_1) -> erlang:apply(ordsets,filter,[fun erlang:'/'/2,[1]]);
probe(filter_callback_kind_3_set_2) -> erlang:apply(ordsets,filter,[fun erlang:'/'/2,[1,2]]);
probe(filter_callback_kind_3_set_3) -> erlang:apply(ordsets,filter,[fun erlang:'/'/2,[1|tail]]);
probe(filter_error) -> erlang:apply(ordsets,filter,[fun ordsets371_callbacks:error_one/1,[1,2]]);
probe(filter_throw) -> erlang:apply(ordsets,filter,[fun ordsets371_callbacks:throw_one/1,[1,2]]);
probe(filter_exit) -> erlang:apply(ordsets,filter,[fun ordsets371_callbacks:exit_one/1,[1,2]]);
probe(filter_invalid_return) -> erlang:apply(ordsets,filter,[fun ordsets371_callbacks:invalid_return_one/1,[1,2]]);
probe(map_callback_kind_0_set_0) -> erlang:apply(ordsets,map,[invalid,[]]);
probe(map_callback_kind_0_set_1) -> erlang:apply(ordsets,map,[invalid,[1]]);
probe(map_callback_kind_0_set_2) -> erlang:apply(ordsets,map,[invalid,[1,2]]);
probe(map_callback_kind_0_set_3) -> erlang:apply(ordsets,map,[invalid,[1|tail]]);
probe(map_callback_kind_1_set_0) -> erlang:apply(ordsets,map,[fun erlang:self/0,[]]);
probe(map_callback_kind_1_set_1) -> erlang:apply(ordsets,map,[fun erlang:self/0,[1]]);
probe(map_callback_kind_1_set_2) -> erlang:apply(ordsets,map,[fun erlang:self/0,[1,2]]);
probe(map_callback_kind_1_set_3) -> erlang:apply(ordsets,map,[fun erlang:self/0,[1|tail]]);
probe(map_callback_kind_2_set_0) -> erlang:apply(ordsets,map,[fun erlang:abs/1,[]]);
probe(map_callback_kind_2_set_1) -> erlang:apply(ordsets,map,[fun erlang:abs/1,[1]]);
probe(map_callback_kind_2_set_2) -> erlang:apply(ordsets,map,[fun erlang:abs/1,[1,2]]);
probe(map_callback_kind_2_set_3) -> erlang:apply(ordsets,map,[fun erlang:abs/1,[1|tail]]);
probe(map_callback_kind_3_set_0) -> erlang:apply(ordsets,map,[fun erlang:'/'/2,[]]);
probe(map_callback_kind_3_set_1) -> erlang:apply(ordsets,map,[fun erlang:'/'/2,[1]]);
probe(map_callback_kind_3_set_2) -> erlang:apply(ordsets,map,[fun erlang:'/'/2,[1,2]]);
probe(map_callback_kind_3_set_3) -> erlang:apply(ordsets,map,[fun erlang:'/'/2,[1|tail]]);
probe(map_error) -> erlang:apply(ordsets,map,[fun ordsets371_callbacks:error_one/1,[1,2]]);
probe(map_throw) -> erlang:apply(ordsets,map,[fun ordsets371_callbacks:throw_one/1,[1,2]]);
probe(map_exit) -> erlang:apply(ordsets,map,[fun ordsets371_callbacks:exit_one/1,[1,2]]);
probe(map_invalid_return) -> erlang:apply(ordsets,map,[fun ordsets371_callbacks:invalid_return_one/1,[1,2]]);
probe(filtermap_callback_kind_0_set_0) -> erlang:apply(ordsets,filtermap,[invalid,[]]);
probe(filtermap_callback_kind_0_set_1) -> erlang:apply(ordsets,filtermap,[invalid,[1]]);
probe(filtermap_callback_kind_0_set_2) -> erlang:apply(ordsets,filtermap,[invalid,[1,2]]);
probe(filtermap_callback_kind_0_set_3) -> erlang:apply(ordsets,filtermap,[invalid,[1|tail]]);
probe(filtermap_callback_kind_1_set_0) -> erlang:apply(ordsets,filtermap,[fun erlang:self/0,[]]);
probe(filtermap_callback_kind_1_set_1) -> erlang:apply(ordsets,filtermap,[fun erlang:self/0,[1]]);
probe(filtermap_callback_kind_1_set_2) -> erlang:apply(ordsets,filtermap,[fun erlang:self/0,[1,2]]);
probe(filtermap_callback_kind_1_set_3) -> erlang:apply(ordsets,filtermap,[fun erlang:self/0,[1|tail]]);
probe(filtermap_callback_kind_2_set_0) -> erlang:apply(ordsets,filtermap,[fun erlang:abs/1,[]]);
probe(filtermap_callback_kind_2_set_1) -> erlang:apply(ordsets,filtermap,[fun erlang:abs/1,[1]]);
probe(filtermap_callback_kind_2_set_2) -> erlang:apply(ordsets,filtermap,[fun erlang:abs/1,[1,2]]);
probe(filtermap_callback_kind_2_set_3) -> erlang:apply(ordsets,filtermap,[fun erlang:abs/1,[1|tail]]);
probe(filtermap_callback_kind_3_set_0) -> erlang:apply(ordsets,filtermap,[fun erlang:'/'/2,[]]);
probe(filtermap_callback_kind_3_set_1) -> erlang:apply(ordsets,filtermap,[fun erlang:'/'/2,[1]]);
probe(filtermap_callback_kind_3_set_2) -> erlang:apply(ordsets,filtermap,[fun erlang:'/'/2,[1,2]]);
probe(filtermap_callback_kind_3_set_3) -> erlang:apply(ordsets,filtermap,[fun erlang:'/'/2,[1|tail]]);
probe(filtermap_error) -> erlang:apply(ordsets,filtermap,[fun ordsets371_callbacks:error_one/1,[1,2]]);
probe(filtermap_throw) -> erlang:apply(ordsets,filtermap,[fun ordsets371_callbacks:throw_one/1,[1,2]]);
probe(filtermap_exit) -> erlang:apply(ordsets,filtermap,[fun ordsets371_callbacks:exit_one/1,[1,2]]);
probe(filtermap_invalid_return) -> erlang:apply(ordsets,filtermap,[fun ordsets371_callbacks:invalid_return_one/1,[1,2]]);
probe(map_deduplicates) -> erlang:apply(ordsets,map,[fun ordsets371_callbacks:map_zero/1,[1,2,3]]);
probe(filtermap_deduplicates) -> erlang:apply(ordsets,filtermap,[fun ordsets371_callbacks:filtermap_zero/1,[1,2,3]]);
probe(filtermap_keep_identity) -> erlang:apply(ordsets,filtermap,[fun ordsets371_callbacks:keep_identity/1,[1,1.0,2]]).

fold_callback(E,A) -> [{visited,E}|A].
filter_even(X) -> X rem 2 =:= 0.
map_reverse(X) -> -X.
map_zero(_) -> 0.
filtermap_zero(_) -> {true,0}.
keep_identity(_) -> true.
filtermap_mixed(X) -> case X rem 3 of 0 -> true; 1 -> false; 2 -> {true,-X} end.
error_one(X) -> erlang:error({callback_failure,X}).
throw_one(X) -> erlang:throw({callback_failure,X}).
exit_one(X) -> erlang:exit({callback_failure,X}).
invalid_return_one(_) -> invalid_return.
error_two(X,A) -> erlang:error({callback_failure,X,A}).
throw_two(X,A) -> erlang:throw({callback_failure,X,A}).
exit_two(X,A) -> erlang:exit({callback_failure,X,A}).
invalid_return_two(_,_) -> invalid_return.
