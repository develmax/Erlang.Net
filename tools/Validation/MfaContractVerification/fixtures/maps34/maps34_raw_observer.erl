-module(maps34_raw_observer).
-export([run/0,names/0]).
names()->[raw_filter_2,raw_filtermap_2,raw_find_2,raw_fold_3,raw_foreach_2,raw_from_keys_2,raw_from_list_1,raw_get_2,raw_get_3,raw_groups_from_list_2,raw_groups_from_list_3,raw_intersect_2,raw_intersect_with_3,raw_is_iterator_valid_1,raw_is_key_2,raw_iterator_1,raw_iterator_2,raw_keys_1,raw_map_2,raw_merge_2,raw_merge_with_3,raw_new_0,raw_next_1,raw_put_3,raw_remove_2,raw_size_1,raw_take_2,raw_to_list_1,raw_update_3,raw_update_with_3,raw_update_with_4,raw_values_1,raw_with_2,raw_without_2].
run()->Names=names(),Default={default,[{N,capture(N)} || N<-Names]},Old=erlang:system_flag(backtrace_depth,8),try [Default|[at_depth(D,Names)||D<-[0,1,2,8]]] after erlang:system_flag(backtrace_depth,Old) end.
at_depth(D,Names)->erlang:system_flag(backtrace_depth,D),{D,[{N,capture(N)}||N<-Names]}.
capture(N)->put(maps_batch_effects,[]),try probe(N) of V->{returned,V,get(maps_batch_effects)} catch C:R:S->{raised,C,R,S,get(maps_batch_effects)} end.
probe(raw_filter_2)->maps:filter(fun(_,_) -> put(maps_batch_effects,[callback]),throw(callback_marker) end,#{a=>1});
probe(raw_filtermap_2)->maps:filtermap(fun(_,_) -> put(maps_batch_effects,[callback]),throw(callback_marker) end,#{a=>1});
probe(raw_find_2)->maps:find(k,wrong);
probe(raw_fold_3)->maps:fold(fun(_,_,_) -> put(maps_batch_effects,[callback]),throw(callback_marker) end,seed,#{a=>1});
probe(raw_foreach_2)->maps:foreach(fun(_,_) -> put(maps_batch_effects,[callback]),throw(callback_marker) end,#{a=>1});
probe(raw_from_keys_2)->maps:from_keys([a|wrong],value);
probe(raw_from_list_1)->maps:from_list([{a,1}|wrong]);
probe(raw_get_2)->maps:get(missing,#{});
probe(raw_get_3)->maps:get(k,wrong,default);
probe(raw_groups_from_list_2)->maps:groups_from_list(fun(_) -> put(maps_batch_effects,[callback]),throw(callback_marker) end,[a]);
probe(raw_groups_from_list_3)->maps:groups_from_list(fun(_) -> put(maps_batch_effects,[key_callback]),throw(callback_marker) end,fun(X)->X end,[a]);
probe(raw_intersect_2)->maps:intersect(first_wrong,second_wrong);
probe(raw_intersect_with_3)->maps:intersect_with(fun(_,_,_) -> put(maps_batch_effects,[callback]),throw(callback_marker) end,#{a=>1},#{a=>2});
probe(raw_is_iterator_valid_1)->maps:is_iterator_valid(wrong);
probe(raw_is_key_2)->maps:is_key(k,wrong);
probe(raw_iterator_1)->maps:iterator(wrong);
probe(raw_iterator_2)->maps:iterator(#{},wrong);
probe(raw_keys_1)->maps:keys(wrong);
probe(raw_map_2)->maps:map(fun(_,_) -> put(maps_batch_effects,[callback]),throw(callback_marker) end,#{a=>1});
probe(raw_merge_2)->maps:merge(first_wrong,second_wrong);
probe(raw_merge_with_3)->maps:merge_with(fun(_,_,_) -> put(maps_batch_effects,[callback]),throw(callback_marker) end,#{a=>1},#{a=>2});
probe(raw_new_0)->maps:new();
probe(raw_next_1)->maps:next(wrong);
probe(raw_put_3)->maps:put(k,v,wrong);
probe(raw_remove_2)->maps:remove(k,wrong);
probe(raw_size_1)->maps:size(wrong);
probe(raw_take_2)->maps:take(k,wrong);
probe(raw_to_list_1)->maps:to_list(wrong);
probe(raw_update_3)->maps:update(missing,v,#{});
probe(raw_update_with_3)->maps:update_with(missing,fun(X)->X end,#{});
probe(raw_update_with_4)->maps:update_with(k,wrong,initial,#{});
probe(raw_values_1)->maps:values(wrong);
probe(raw_with_2)->maps:with([k|wrong],#{k=>v});
probe(raw_without_2)->maps:without([k|wrong],#{k=>v}).
