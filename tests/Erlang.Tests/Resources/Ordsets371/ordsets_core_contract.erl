-module(ordsets_core_contract).
-export([run/0,trace/2]).
run() ->
    [] = ordsets:new(),
    true = ordsets:is_empty([]),
    false = ordsets:is_empty(bad),
    bad = ordsets:to_list(bad),
    true = ordsets:is_set([1,2,a]),
    false = ordsets:is_set([1,1.0]),
    false = ordsets:is_set([b,a|tail]),
    3 = ordsets:size([a,b,c]),
    true = ordsets:is_equal([1|2],[1.0|2.0]),
    false = ordsets:is_equal([a,b],[b,a]),
    true = ordsets:is_element(1.0,[1,2]),
    false = ordsets:is_element(a,[b|tail]),
    [1,2] = ordsets:add_element(1.0,[1,2]),
    [a,b|tail] = ordsets:add_element(a,[b|tail]),
    [a,b,c] = ordsets:add_element(c,[a,b]),
    {error,function_clause} = failure(fun() -> ordsets:is_set([a|tail]) end),
    {error,function_clause} = failure(fun() -> ordsets:is_equal(bad,[]) end),
    {error,function_clause} = failure(fun() -> ordsets:is_element(c,[a,b|tail]) end),
    {error,function_clause} = failure(fun() -> ordsets:add_element(c,[a,b|tail]) end),
    {error,badarg} = failure(fun() -> ordsets:size([a|tail]) end),
    ok.

failure(F) -> try F() of V -> {unexpected,V} catch C:R -> {C,R} end.

trace(Element, Set) ->
    try [ordsets:add_element(Element, Set)] of V -> {unexpected,V}
    catch Class:Reason:Stack -> {Class,Reason,Stack} end.
