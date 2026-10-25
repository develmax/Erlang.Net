-module(integer_contract493_integer_to_binary1_p0).
-export([run/0,run_stacks/0,probe/1]).
run() -> ordsets_core_support:run(?MODULE,cases()).
run_stacks() -> ordsets_core_support:profiles(?MODULE,cases()).
cases() -> [c0,c1,c2,c3,c4,c5,c6,c7,c8,c9,c10,c11,c12,c13,c14,c15,c16,c17,c18,c19,c20,c21,c22,c23,c24,c25,c26,c27,c28].
probe(c0) -> erlang:apply(erlang,opaque(integer_to_binary),[0]);
probe(c1) -> erlang:apply(erlang,opaque(integer_to_binary),[1]);
probe(c2) -> erlang:apply(erlang,opaque(integer_to_binary),[-1]);
probe(c3) -> erlang:apply(erlang,opaque(integer_to_binary),[2]);
probe(c4) -> erlang:apply(erlang,opaque(integer_to_binary),[-2]);
probe(c5) -> erlang:apply(erlang,opaque(integer_to_binary),[576460752303423487]);
probe(c6) -> erlang:apply(erlang,opaque(integer_to_binary),[576460752303423488]);
probe(c7) -> erlang:apply(erlang,opaque(integer_to_binary),[-576460752303423488]);
probe(c8) -> erlang:apply(erlang,opaque(integer_to_binary),[-576460752303423489]);
probe(c9) -> erlang:apply(erlang,opaque(integer_to_binary),[18446744073709551616]);
probe(c10) -> erlang:apply(erlang,opaque(integer_to_binary),[-18446744073709551616]);
probe(c11) -> erlang:apply(erlang,opaque(integer_to_binary),[((1 bsl 1024)-1)]);
probe(c12) -> erlang:apply(erlang,opaque(integer_to_binary),[(1 bsl 2048)]);
probe(c13) -> erlang:apply(erlang,opaque(integer_to_binary),[-(1 bsl 2048)]);
probe(c14) -> erlang:apply(erlang,opaque(integer_to_binary),[a]);
probe(c15) -> erlang:apply(erlang,opaque(integer_to_binary),[true]);
probe(c16) -> erlang:apply(erlang,opaque(integer_to_binary),[1.0]);
probe(c17) -> erlang:apply(erlang,opaque(integer_to_binary),[-0.0]);
probe(c18) -> erlang:apply(erlang,opaque(integer_to_binary),[[]]);
probe(c19) -> erlang:apply(erlang,opaque(integer_to_binary),[[49]]);
probe(c20) -> erlang:apply(erlang,opaque(integer_to_binary),[[49|tail]]);
probe(c21) -> erlang:apply(erlang,opaque(integer_to_binary),[{}]);
probe(c22) -> erlang:apply(erlang,opaque(integer_to_binary),[{a}]);
probe(c23) -> erlang:apply(erlang,opaque(integer_to_binary),[#{}]);
probe(c24) -> erlang:apply(erlang,opaque(integer_to_binary),[<<49>>]);
probe(c25) -> erlang:apply(erlang,opaque(integer_to_binary),[<<1:1>>]);
probe(c26) -> erlang:apply(erlang,opaque(integer_to_binary),[fun erlang:abs/1]);
probe(c27) -> erlang:apply(erlang,opaque(integer_to_binary),[binary_to_term(<<131,103,100,0,6,"oracle",0,0,0,1,0,0,0,0,0>>)]);
probe(c28) -> erlang:apply(erlang,opaque(integer_to_binary),[binary_to_term(<<131,102,100,0,6,"oracle",0,0,0,1,0>>)]).
opaque(Value) -> put(integer_contract493_input,Value),erase(integer_contract493_input).
