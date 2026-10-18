-module(ordsets372_fold_filter).
-export([cases/0,run/0,run_stacks/0,probe/1,fold_error/2,fold_throw/2,fold_exit/2,filter_error/1,filter_bad/1]).
cases() -> [fold_late_error_local,fold_late_error_external,
 fold_late_throw_local,fold_late_throw_external,fold_late_exit_local,fold_late_exit_external,
 fold_local_payload,fold_external_payload,
 filter_mixed_error_local,filter_mixed_error_external,
 filter_mixed_bad_local,filter_mixed_bad_external,
 filter_improper_keep_drop,filter_improper_drop_keep,filter_improper_all_drop,
 filter_keep_compound,filter_keep_duplicate_payload,
 caller_fold_error,caller_fold_success,caller_filter_error,caller_filter_success].
run() -> ordsets_core_support:run(ordsets372_fold_filter,cases()).
run_stacks() -> ordsets_core_support:profiles(ordsets372_fold_filter,cases()).
probe(fold_late_error_local) -> ordsets:fold(fun(first,A)->[first|A];(second,A)->erlang:error({late,A}) end,initial,[first,second]);
probe(fold_late_error_external) -> ordsets:fold(fun ordsets372_fold_filter:fold_error/2,initial,[first,second]);
probe(fold_late_throw_local) -> ordsets:fold(fun(first,A)->[first|A];(second,A)->erlang:throw({late,A}) end,initial,[first,second]);
probe(fold_late_throw_external) -> ordsets:fold(fun ordsets372_fold_filter:fold_throw/2,initial,[first,second]);
probe(fold_late_exit_local) -> ordsets:fold(fun(first,A)->[first|A];(second,A)->erlang:exit({late,A}) end,initial,[first,second]);
probe(fold_late_exit_external) -> ordsets:fold(fun ordsets372_fold_filter:fold_exit/2,initial,[first,second]);
probe(fold_local_payload) ->
 Payload={captured,#{key=>{value,fun erlang:abs/1}}},
 ordsets:fold(fun(E,A)->{Payload,E,A} end,{initial,Payload},[z,a,z]);
probe(fold_external_payload) -> ordsets:fold(fun erlang:'++'/2,[tail],[[z],[a],[z]]);
probe(filter_mixed_error_local) -> ordsets:filter(fun(first)->true;(second)->false;(third)->erlang:error({late,third}) end,[first,second,third]);
probe(filter_mixed_error_external) -> ordsets:filter(fun ordsets372_fold_filter:filter_error/1,[first,second,third]);
probe(filter_mixed_bad_local) -> ordsets:filter(fun(first)->true;(second)->false;(third)->{invalid,#{payload=>[third]}} end,[first,second,third]);
probe(filter_mixed_bad_external) -> ordsets:filter(fun ordsets372_fold_filter:filter_bad/1,[first,second,third]);
probe(filter_improper_keep_drop) -> ordsets:filter(fun(first)->true;(second)->false end,[first,second|{tail,#{k=>v}}]);
probe(filter_improper_drop_keep) -> ordsets:filter(fun(first)->false;(second)->true end,[first,second|{tail,#{k=>v}}]);
probe(filter_improper_all_drop) -> ordsets:filter(fun(_)->false end,[first,second|{tail,#{k=>v}}]);
probe(filter_keep_compound) ->
 Payload={payload,#{key=>fun erlang:abs/1}},
 ordsets:filter(fun(_)->true end,[Payload,{other,[a,b]},Payload]);
probe(filter_keep_duplicate_payload) ->
 Payload=#{key=>{value,fun erlang:abs/1}},
 ordsets:filter(fun(_)->true end,[Payload,Payload,#{key=>different}]);
probe(caller_fold_error) -> fold_caller(error);
probe(caller_fold_success) -> fold_caller(success);
probe(caller_filter_error) -> filter_caller(error);
probe(caller_filter_success) -> filter_caller(success).
fold_caller(error) ->
 Result=ordsets:fold(fun ordsets372_fold_filter:fold_error/2,initial,[first,second]),
 {non_tail_fold,Result,continued};
fold_caller(success) ->
 Result=ordsets:fold(fun(E,A)->[E|A] end,[],[first,second]),
 {non_tail_fold,Result,continued}.
filter_caller(error) ->
 Result=ordsets:filter(fun ordsets372_fold_filter:filter_error/1,[first,second,third]),
 {non_tail_filter,Result,continued};
filter_caller(success) ->
 Result=ordsets:filter(fun(first)->true;(second)->false;(third)->true end,[first,second,third]),
 {non_tail_filter,Result,continued}.
fold_error(first,A) -> [first|A];
fold_error(second,A) -> erlang:error({late,A}).
fold_throw(first,A) -> [first|A];
fold_throw(second,A) -> erlang:throw({late,A}).
fold_exit(first,A) -> [first|A];
fold_exit(second,A) -> erlang:exit({late,A}).
filter_error(first) -> true;
filter_error(second) -> false;
filter_error(third) -> erlang:error({late,third}).
filter_bad(first) -> true;
filter_bad(second) -> false;
filter_bad(third) -> {invalid,#{payload=>[third]}}.
