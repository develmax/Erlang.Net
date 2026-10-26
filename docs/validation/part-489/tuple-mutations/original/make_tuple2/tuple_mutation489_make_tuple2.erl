-module(tuple_mutation489_make_tuple2).
-export([run/0,run_stacks/0,probe/1]).
run() -> ordsets_core_support:run(?MODULE,cases()).
run_stacks() -> ordsets_core_support:profiles(?MODULE,cases()).
cases() -> [make_267,
            make_274,
            make_281,
            make_288,
            make_295,
            make_302,
            make_309,
            make_316,
            make_323,
            make_330,
            make_337].
probe(make_267) -> erlang:apply(erlang,opaque(make_tuple),[-18446744073709551616,{default,<<1:9>>}]);
probe(make_274) -> erlang:apply(erlang,opaque(make_tuple),[-1,{default,<<1:9>>}]);
probe(make_281) -> erlang:apply(erlang,opaque(make_tuple),[0,{default,<<1:9>>}]);
probe(make_288) -> erlang:apply(erlang,opaque(make_tuple),[1,{default,<<1:9>>}]);
probe(make_295) -> erlang:apply(erlang,opaque(make_tuple),[2,{default,<<1:9>>}]);
probe(make_302) -> erlang:apply(erlang,opaque(make_tuple),[33,{default,<<1:9>>}]);
probe(make_309) -> erlang:apply(erlang,opaque(make_tuple),[257,{default,<<1:9>>}]);
probe(make_316) -> erlang:apply(erlang,opaque(make_tuple),[16777216,{default,<<1:9>>}]);
probe(make_323) -> erlang:apply(erlang,opaque(make_tuple),[18446744073709551616,{default,<<1:9>>}]);
probe(make_330) -> erlang:apply(erlang,opaque(make_tuple),[a,{default,<<1:9>>}]);
probe(make_337) -> erlang:apply(erlang,opaque(make_tuple),[1.0,{default,<<1:9>>}]).
opaque(Value) -> put(tuple_mutation489_input,Value),erase(tuple_mutation489_input).
