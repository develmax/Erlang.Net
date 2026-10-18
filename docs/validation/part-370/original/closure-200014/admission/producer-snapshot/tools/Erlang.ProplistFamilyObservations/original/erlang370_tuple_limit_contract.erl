-module(erlang370_tuple_limit_contract).
-export([cases/0,run/0,run_stacks/0,probe/1,dynamic_element/2]).
cases() -> [insert_runtime_only_summary,insert_exported_index_summary].
run() -> erlang370_pure_support:run(erlang370_tuple_limit_contract,cases()).
run_stacks() -> erlang370_pure_support:profiles(erlang370_tuple_limit_contract,cases()).
probe(insert_runtime_only_summary) ->
    Tuple=erlang370_pure_support:invoke(make_tuple,[16777215,a]),
    Changed=erlang370_pure_support:invoke(insert_element,[1,Tuple,b]),
    {erlang370_pure_support:invoke(tuple_size,[Tuple]),
     erlang370_pure_support:invoke(tuple_size,[Changed]),
     erlang370_pure_support:invoke(element,[1,Changed]),
     erlang370_pure_support:invoke(element,[16777216,Changed])};
probe(insert_exported_index_summary) ->
    Tuple=erlang370_pure_support:invoke(make_tuple,[16777215,a]),
    Changed=erlang370_pure_support:invoke(insert_element,[1,Tuple,b]),
    {erlang370_pure_support:invoke(tuple_size,[Tuple]),
     erlang370_pure_support:invoke(tuple_size,[Changed]),
     dynamic_element(1,Changed),dynamic_element(16777216,Changed)}.
dynamic_element(Index,Tuple) -> erlang:element(Index,Tuple).
