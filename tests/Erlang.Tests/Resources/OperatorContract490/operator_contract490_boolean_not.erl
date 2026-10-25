-module(operator_contract490_boolean_not).
-export([run/0,run_stacks/0,probe/1]).
run() -> ordsets_core_support:run(?MODULE,cases()).
run_stacks() -> ordsets_core_support:profiles(?MODULE,cases()).
cases() -> [c0,c1,c2,c3,c4,c5,c6,c7,c8,c9,c10,c11,c12,c13,c14,c15,c16,c17,c18,c19,c20,c21,c22,c23,c24,c25,c26,c27,c28,c29,c30,c31,c32,c33,c34,c35,c36,c37].
probe(c0) -> erlang:apply(erlang,opaque('not'),[true]);
probe(c1) -> erlang:apply(erlang,opaque('not'),[false]);
probe(c2) -> erlang:apply(erlang,opaque('not'),[a]);
probe(c3) -> erlang:apply(erlang,opaque('not'),[[]]);
probe(c4) -> erlang:apply(erlang,opaque('not'),[[a]]);
probe(c5) -> erlang:apply(erlang,opaque('not'),[[a|tail]]);
probe(c6) -> erlang:apply(erlang,opaque('not'),[{}]);
probe(c7) -> erlang:apply(erlang,opaque('not'),[{a}]);
probe(c8) -> erlang:apply(erlang,opaque('not'),[#{}]);
probe(c9) -> erlang:apply(erlang,opaque('not'),[#{a=>1}]);
probe(c10) -> erlang:apply(erlang,opaque('not'),[<<>>]);
probe(c11) -> erlang:apply(erlang,opaque('not'),[<<1:1>>]);
probe(c12) -> erlang:apply(erlang,opaque('not'),[fun erlang:abs/1]);
probe(c13) -> erlang:apply(erlang,opaque('not'),[fun() -> ok end]);
probe(c14) -> erlang:apply(erlang,opaque('not'),[binary_to_term(<<131,103,100,0,6,"oracle",0,0,0,1,0,0,0,0,0>>)]);
probe(c15) -> erlang:apply(erlang,opaque('not'),[binary_to_term(<<131,101,100,0,6,"oracle",0,0,0,1,0>>)]);
probe(c16) -> erlang:apply(erlang,opaque('not'),[binary_to_term(<<131,102,100,0,6,"oracle",0,0,0,1,0>>)]);
probe(c17) -> erlang:apply(erlang,opaque('not'),[0]);
probe(c18) -> erlang:apply(erlang,opaque('not'),[1]);
probe(c19) -> erlang:apply(erlang,opaque('not'),[-1]);
probe(c20) -> erlang:apply(erlang,opaque('not'),[134217727]);
probe(c21) -> erlang:apply(erlang,opaque('not'),[134217728]);
probe(c22) -> erlang:apply(erlang,opaque('not'),[-134217729]);
probe(c23) -> erlang:apply(erlang,opaque('not'),[576460752303423487]);
probe(c24) -> erlang:apply(erlang,opaque('not'),[576460752303423488]);
probe(c25) -> erlang:apply(erlang,opaque('not'),[-576460752303423489]);
probe(c26) -> erlang:apply(erlang,opaque('not'),[18446744073709551616]);
probe(c27) -> erlang:apply(erlang,opaque('not'),[-18446744073709551616]);
probe(c28) -> erlang:apply(erlang,opaque('not'),[(1 bsl 2048)]);
probe(c29) -> erlang:apply(erlang,opaque('not'),[-(1 bsl 2048)]);
probe(c30) -> erlang:apply(erlang,opaque('not'),[0.0]);
probe(c31) -> erlang:apply(erlang,opaque('not'),[-0.0]);
probe(c32) -> erlang:apply(erlang,opaque('not'),[1.5]);
probe(c33) -> erlang:apply(erlang,opaque('not'),[-1.5]);
probe(c34) -> erlang:apply(erlang,opaque('not'),[1.7976931348623157e308]);
probe(c35) -> erlang:apply(erlang,opaque('not'),[-1.7976931348623157e308]);
probe(c36) -> erlang:apply(erlang,opaque('not'),[2.2250738585072014e-308]);
probe(c37) -> erlang:apply(erlang,opaque('not'),[4.9406564584124654e-324]).
opaque(Value) -> put(operator_contract490_input,Value),erase(operator_contract490_input).
