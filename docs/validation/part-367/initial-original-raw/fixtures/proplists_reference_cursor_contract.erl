-module(proplists_reference_cursor_contract).
-export([cases/0,run/0,run_stacks/0]).

cases() -> [flat_aliases,hamt_aliases,prefix_alias].

run() ->
    Old = erts_debug:set_internal_state(available_internal_state,true),
    try [{Name,capture(Name)} || Name <- cases()]
    after erts_debug:set_internal_state(available_internal_state,Old) end.

run_stacks() ->
    Default = {default,run()},
    Old = erlang:system_flag(backtrace_depth,8),
    try [Default | [at_depth(Depth) || Depth <- [0,1,2,8]]]
    after erlang:system_flag(backtrace_depth,Old) end.

at_depth(Depth) ->
    erlang:system_flag(backtrace_depth,Depth),
    {Depth,run()}.

capture(Name) ->
    try probe(Name) of Value -> {ok,Value}
    catch Class:Reason:Stack -> {error,Class,Reason,Stack} end.

observe(Cursor) ->
    try proplists:from_map(Cursor) of Value -> {ok,Value}
    catch Class:Reason:Stack -> {error,Class,Reason,Stack} end.

reference(First,Count) ->
    <<131,Node/binary>> = term_to_binary('hash_reference@remote'),
    Tail = list_to_binary(lists:duplicate(4*(Count-1),0)),
    binary_to_term(<<131,90,Count:16,Node/binary,1:32,First:32,Tail/binary>>).

hash(Term) -> erts_debug:get_internal_state({internal_hash,Term}).

probe(flat_aliases) ->
    One = reference(7,1),
    Map = #{One=>present},
    {Map,aliases(7,One,Map)};
probe(hamt_aliases) ->
    One = reference(7,1),
    Base = maps:from_list([{I,I} || I <- lists:seq(1,33)]),
    Map = Base#{One=>present},
    {Map,aliases(7,One,Map)};
probe(prefix_alias) ->
    {First,One,Two,H1,H2} = find_pair(0,512),
    Root = H1 band 15,
    Pairs = select_base(0,4096,33,Root,[]),
    Base = maps:from_list(Pairs),
    Map = Base#{One=>present},
    Cursor = [[Two]|Map],
    BaseHashes = [{Key,hash(Key)} || {Key,_Value} <- Pairs],
    {First,One,Two,One =:= Two,H1,H2,Root,BaseHashes,Map,
     maps:get(Two,Map,missing),Cursor,observe(Cursor)}.

aliases(First,One,Map) ->
    [alias(Count,First,One,Map) || Count <- lists:seq(1,5)].

alias(Count,First,One,Map) ->
    Ref = reference(First,Count),
    Cursor = [[Ref]|Map],
    {Count,Ref,Ref =:= One,term_to_binary(Ref),hash(Ref),
     maps:get(Ref,Map,missing),Cursor,observe(Cursor)}.

find_pair(First,Limit) when First < Limit ->
    One = reference(First,1),
    Two = reference(First,2),
    H1 = hash(One),
    H2 = hash(Two),
    case H1 =/= H2 andalso (H1 band 15) =:= (H2 band 15) of
        true -> {First,One,Two,H1,H2};
        false -> find_pair(First+1,Limit)
    end;
find_pair(_First,Limit) ->
    erlang:error({reference_prefix_search_exhausted,Limit}).

select_base(_Next,_Limit,0,_Root,Pairs) -> lists:reverse(Pairs);
select_base(Next,Limit,Needed,Root,Pairs) when Next < Limit ->
    case (hash(Next) band 15) =:= Root of
        true -> select_base(Next+1,Limit,Needed,Root,Pairs);
        false -> select_base(Next+1,Limit,Needed-1,Root,[{Next,Next}|Pairs])
    end;
select_base(_Next,Limit,Needed,_Root,_Pairs) ->
    erlang:error({reference_base_search_exhausted,Limit,Needed}).
