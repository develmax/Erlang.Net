-module(beam_ets_counter417).
-export([run/0,run_stacks/0,worker/2,capture/1]).

definitions() -> [
    {implicit_positive,set,k,5,none},
    {implicit_negative,set,k,-12,none},
    {implicit_big,set,k,1 bsl 100,none},
    {tuple_positive,set,k,{2,3},none},
    {tuple_big,set,k,{2,1 bsl 100},none},
    {threshold_positive,set,k,{2,5,12,0},none},
    {threshold_negative,set,k,{2,-5,8,100},none},
    {threshold_equal,set,k,{2,2,12,0},none},
    {threshold_zero,set,k,{2,0,9,99},none},
    {threshold_big,set,k,{2,1 bsl 100,1 bsl 80,-1 bsl 120},none},
    {list_multi,set,k,[{2,1},{3,2}],none},
    {list_repeat,set,k,[{2,1},{2,-3},{2,100,20,-5}],none},
    {empty_list,set,k,[],none},
    {default_insert,set,missing,3,{ignored,4,8}},
    {default_key_replaced,set,missing,{3,5},{ignored,4,8}},
    {default_existing,set,k,{2,1},{ignored,bad,8}},
    {default_empty_insert,set,missing,[],{ignored,4,8}},
    {bad_default_atom,set,k,1,invalid_default},
    {bad_default_arity,set,k,1,{}},
    {bad_increment_float,set,k,{2,1.0},none},
    {bad_position_float,set,k,{2.0,1},none},
    {bad_position_big,set,k,{1 bsl 100,1},none},
    {bad_threshold,set,k,{2,1,bad,0},none},
    {bad_warp,set,k,{2,1,20,bad},none},
    {bad_improper_list,set,k,[{2,1}|bad],none},
    {bad_list_element,set,k,[{2,1},bad],none},
    {bad_op_arity,set,k,{2,1,2},none},
    {bad_argument_atom,set,k,bad,none},
    {bad_key_position,set,k,{1,1},none},
    {bad_zero_position,set,k,{0,1},none},
    {bad_high_position,set,k,{9,1},none},
    {bad_negative_position,set,k,{-1,1},none},
    {bad_old_counter,bad_old,k,{2,1},none},
    {missing_key,set,missing,1,none},
    {missing_key_precedes_bad_ops,set,missing,bad,none},
    {bag_type,bag,k,1,none},
    {duplicate_type,duplicate_bag,k,1,none},
    {bag_default_type,bag,k,1,{k,0}},
    {default_precedes_bag,bag,k,bad,invalid_default},
    {missing_table3,missing_table,k,1,none},
    {missing_table4,missing_table,k,1,{k,0}},
    {wrong_type_table3,wrong_table,k,1,none},
    {wrong_type_table4,wrong_table,k,1,{k,0}},
    {ordered_numeric_key,ordered_numeric,1.0,1,none},
    {set_numeric_distinct,set_numeric,1.0,1,none},
    {keypos_implicit,keypos,k,5,none},
    {rollback_existing,set,k,[{2,1},{3,bad}],none},
    {rollback_default,set,missing,[{2,1},{3,bad}],{ignored,4,8}}].
cases() -> [Name || {Name,_,_,_,_} <- definitions()].
run() -> [{Name,observe(Name)} || Name <- cases()].
run_stacks() ->
    Old = erlang:system_flag(backtrace_depth,8),
    erlang:system_flag(backtrace_depth,Old),
    try [{default,Old,run()} | [at_depth(D) || D <- [0,1,2,8]]]
    after erlang:system_flag(backtrace_depth,Old) end.
at_depth(D) ->
    erlang:system_flag(backtrace_depth,D),
    Effective = erlang:system_flag(backtrace_depth,D),
    {D,Effective,run()}.
observe(Name) ->
    {Pid,Ref} = spawn_monitor(?MODULE,worker,[self(),Name]),
    receive {Name,V} -> receive {'DOWN',Ref,process,Pid,normal} -> V end
    after 10000 -> exit(Pid,kill), receive {'DOWN',Ref,process,Pid,_} -> ok end,
                  exit(worker_timeout) end.
worker(Parent,Name) ->
    Outcome = try probe(Name) of V -> {returned,V}
              catch Class:Reason:Stack -> {caught,Class,Reason,Stack} end,
    Parent ! {Name,Outcome}, ok.
probe(Name) ->
    {Name,Mode,Key,Ops,Default} = lists:keyfind(Name,1,definitions()),
    case Mode of
        missing_table -> invoke(missing_counter_table417,Key,Ops,Default);
        wrong_table -> invoke(42,Key,Ops,Default);
        _ ->
            Kind = case Mode of bag -> bag; duplicate_bag -> duplicate_bag;
                                ordered_numeric -> ordered_set; _ -> set end,
            Extra = case Mode of keypos -> [{keypos,2}]; _ -> [] end,
            T = ets:new(counter_original417,[named_table,public,Kind | Extra]),
            try
                Row = case Mode of bad_old -> {k,not_integer,20};
                                   keypos -> {dummy,k,10,20};
                                   ordered_numeric -> {1,10,20};
                                   set_numeric -> {1,10,20}; _ -> {k,10,20} end,
                true = ets:insert(T,Row),
                Outcome = try invoke(T,Key,Ops,Default) of V -> {returned,V}
                          catch Class:Reason:Stack -> {caught,Class,Reason,Stack} end,
                {Outcome,ets:lookup(T,Key)}
            after ets:delete(T) end
    end.
invoke(T,Key,Ops,none) -> ets:update_counter(T,Key,Ops);
invoke(T,Key,Ops,Default) -> ets:update_counter(T,Key,Ops,Default).
capture([Output]) ->
    V = run(), S = run_stacks(),
    ok = prim_file:write_file(Output ++ "/values.etf",term_to_binary(V,[{minor_version,2}])),
    ok = prim_file:write_file(Output ++ "/stacks.etf",term_to_binary(S,[{minor_version,2}])),
    erlang:halt(0).
