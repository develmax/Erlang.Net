-module(binary_search_contract).
-export([run/0, failures/0, bad/0, caller/0]).
run() ->
    B = <<"ababa">>,
    {0,2} = binary:match(B,[<<"a">>,<<"ab">>]),
    {2,2} = binary:match(B,[<<"a">>,<<"ab">>],[{scope,{5,-3}}|tail]),
    [{0,2},{2,2},{4,1}] = binary:matches(B,[<<"a">>,<<"ab">>]),
    [{2,1},{4,1}] = binary:matches(B,<<"a">>,[{scope,{2,3}}]),
    [<<>>,<<"baba">>] = binary:split(B,<<"a">>),
    [<<"b">>,<<"b">>] = binary:split(B,<<"a">>,[global,trim_all]),
    {ac,Ref} = CP = binary:compile_pattern([<<"a">>,<<"ab">>]),
    true = is_reference(Ref),
    CP = binary_to_term(term_to_binary(CP)),
    {0,2} = binary:match(B,CP),
    {error,badarg,[{binary,match,[not_binary,<<"a">>],
                    [{error_info,#{module:=erl_stdlib_errors}}]}|
                   [{binary_search_contract,bad,0,_},
                    {binary_search_contract,caller,0,_}|_]]} = failures(),
    {ok,done}.
failures() ->
    try caller() of V -> {unexpected,V}
    catch C:R:S -> {C,R,S} end.
caller() ->
    Result = bad(),
    {returned,Result}.
bad() -> binary:match(not_binary,<<"a">>).
