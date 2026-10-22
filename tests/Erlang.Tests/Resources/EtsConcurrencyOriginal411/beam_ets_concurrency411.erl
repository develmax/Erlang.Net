-module(beam_ets_concurrency411).
-export([run/0,run_stacks/0,worker/2]).
cases() -> [defaults,read_true,read_false,write_true,write_false,write_auto,both,read_true_false,read_false_true,write_auto_false,write_false_auto,write_auto_true,write_true_auto,private_flags,ordered_flags,bag_auto,duplicate_auto,debug_min,debug_max,debug_zero,debug_above_max,debug_ordered,debug_ordered_private,debug_then_false_ordered,debug_then_auto_ordered,debug_ordered_then_bag,debug_ordered_then_set,read_auto,read_integer,write_invalid,read_short_tuple,write_long_tuple,invalid_read_then_valid,invalid_write_then_valid,valid_then_invalid,improper_options,bare_read,logger_flags,application_flags].
run() -> [{Case,observe(Case)} || Case <- cases()].
run_stacks() -> [with_depth(Depth) || Depth <- [default,0,1,2,8]].
with_depth(default) -> {default,run()};
with_depth(Depth) -> Old=erlang:system_flag(backtrace_depth,Depth),try {Depth,run()} after erlang:system_flag(backtrace_depth,Old) end.
observe(Case) ->
    Parent=self(),Ref=make_ref(),{Pid,Mon}=spawn_monitor(?MODULE,worker,[{Parent,Ref},Case]),
    receive {Ref,Value} -> receive {'DOWN',Mon,process,Pid,normal} -> Value end end.
worker({Parent,Ref},Case) ->
    Outcome=try probe(Case) of Value -> {value,Value} catch C:R:S -> {caught,C,R,S} end,
    Parent ! {Ref,Outcome}.
probe(Case) ->
    Name=ets_concurrency411,Name=ets:new(Name,options(Case)),
    true=ets:insert(Name,[{2,b},{1,a}]),
    Result={ets:lookup(Name,1),ets:member(Name,2),ets:select(Name,[{{1,'$1'},[],['$1']}]),ets:delete(Name)},
    Result.
options(defaults) -> [named_table];
options(read_true) -> [named_table,{read_concurrency,true}];
options(read_false) -> [named_table,{read_concurrency,false}];
options(write_true) -> [named_table,{write_concurrency,true}];
options(write_false) -> [named_table,{write_concurrency,false}];
options(write_auto) -> [named_table,{write_concurrency,auto}];
options(both) -> [named_table,{read_concurrency,true},{write_concurrency,auto}];
options(read_true_false) -> [named_table,{read_concurrency,true},{read_concurrency,false}];
options(read_false_true) -> [named_table,{read_concurrency,false},{read_concurrency,true}];
options(write_auto_false) -> [named_table,{write_concurrency,auto},{write_concurrency,false}];
options(write_false_auto) -> [named_table,{write_concurrency,false},{write_concurrency,auto}];
options(write_auto_true) -> [named_table,{write_concurrency,auto},{write_concurrency,true}];
options(write_true_auto) -> [named_table,{write_concurrency,true},{write_concurrency,auto}];
options(private_flags) -> [named_table,private,{read_concurrency,true},{write_concurrency,auto}];
options(ordered_flags) -> [named_table,ordered_set,{read_concurrency,true},{write_concurrency,auto}];
options(bag_auto) -> [named_table,bag,{write_concurrency,auto}];
options(duplicate_auto) -> [named_table,duplicate_bag,{write_concurrency,auto}];
options(debug_min) -> [named_table,{write_concurrency,{debug_hash_fixed_number_of_locks,1}}];
options(debug_max) -> [named_table,{write_concurrency,{debug_hash_fixed_number_of_locks,32768}}];
options(debug_zero) -> [named_table,{write_concurrency,{debug_hash_fixed_number_of_locks,0}}];
options(debug_above_max) -> [named_table,{write_concurrency,{debug_hash_fixed_number_of_locks,32769}}];
options(debug_ordered) -> [named_table,{write_concurrency,{debug_hash_fixed_number_of_locks,8}},ordered_set];
options(debug_ordered_private) -> [named_table,ordered_set,private,{write_concurrency,{debug_hash_fixed_number_of_locks,8}}];
options(debug_then_false_ordered) -> [named_table,{write_concurrency,{debug_hash_fixed_number_of_locks,8}},{write_concurrency,false},ordered_set];
options(debug_then_auto_ordered) -> [named_table,ordered_set,{write_concurrency,{debug_hash_fixed_number_of_locks,8}},{write_concurrency,auto}];
options(debug_ordered_then_bag) -> [named_table,ordered_set,{write_concurrency,{debug_hash_fixed_number_of_locks,8}},bag];
options(debug_ordered_then_set) -> [named_table,ordered_set,{write_concurrency,{debug_hash_fixed_number_of_locks,8}},set];
options(read_auto) -> [named_table,{read_concurrency,auto}];
options(read_integer) -> [named_table,{read_concurrency,1}];
options(write_invalid) -> [named_table,{write_concurrency,invalid}];
options(read_short_tuple) -> [named_table,{read_concurrency}];
options(write_long_tuple) -> [named_table,{write_concurrency,true,extra}];
options(invalid_read_then_valid) -> [named_table,{read_concurrency,invalid},{read_concurrency,true}];
options(invalid_write_then_valid) -> [named_table,{write_concurrency,invalid},{write_concurrency,auto}];
options(valid_then_invalid) -> [named_table,{write_concurrency,auto},{read_concurrency,invalid}];
options(improper_options) -> [named_table,{read_concurrency,true}|invalid];
options(bare_read) -> [named_table,read_concurrency];
options(logger_flags) -> [set,protected,named_table,{read_concurrency,true},{write_concurrency,true}];
options(application_flags) -> [set,public,named_table,{read_concurrency,true}].
