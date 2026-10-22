-module(ordsets_full_contract).
-export([run/0]).
run() ->
    [1,2,3] = ordsets:from_list([3,1,2,1]),
    [1,3] = ordsets:del_element(2,[1,2,3]),
    [1,2] = ordsets:union([2.0],[1,2]),
    [1,2.0] = ordsets:union([1,2.0],[2]),
    [2] = ordsets:intersection([2.0],[1,2]),
    [2.0] = ordsets:intersection([1,2.0],[2]),
    false = ordsets:is_disjoint([2.0],[1,2]),
    true = ordsets:is_disjoint([1],[2,3]),
    [1,2,3] = ordsets:union([[1,2],[2,3]]),
    [3] = ordsets:intersection([[1,2,3],[2,3],[3,4]]),
    bad = ordsets:intersection([bad]),
    [] = ordsets:intersection([],bad),
    [1,3] = ordsets:subtract([1,2,3],[2.0]),
    true = ordsets:is_subset([1.0,3],[1,2,3]),
    false = ordsets:is_subset([1],[2]),
    [3,2,1] = ordsets:fold(fun(E,A)->[E|A] end,[],[1,2,3]),
    [2,2,4] = ordsets:filter(fun(E)->E rem 2 =:= 0 end,[1,2,2,3,4]),
    [-3,-2,-1] = ordsets:map(fun(E)->-E end,[1,2,3]),
    [0] = ordsets:map(fun(_)->0 end,[1,2,3]),
    [0] = ordsets:filtermap(fun(_)->{true,0} end,[1,2,3]),
    [1,3] = ordsets:filtermap(fun(E)->E rem 2 =/= 0 end,[1,2,3]),
    {error,function_clause} = failure(fun()->ordsets:intersection([]) end),
    {error,function_clause} = failure(fun()->ordsets:union(bad,bad) end),
    {error,function_clause} = failure(fun()->ordsets:del_element(2,[1|tail]) end),
    {error,function_clause} = failure(fun()->ordsets:fold(invalid,initial,[]) end),
    {error,{bad_generator,tail}} = failure(fun()->ordsets:filter(fun(_)->true end,[1|tail]) end),
    {error,{bad_filter,invalid}} = failure(fun()->ordsets:filter(fun(_)->invalid end,[1]) end),
    {error,{case_clause,invalid}} = failure(fun()->ordsets:filtermap(fun(_)->invalid end,[1]) end),
    ok.

failure(F) -> try F() of V -> {unexpected,V} catch C:R -> {C,R} end.
