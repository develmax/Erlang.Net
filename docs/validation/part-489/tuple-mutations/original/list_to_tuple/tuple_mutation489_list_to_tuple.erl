-module(tuple_mutation489_list_to_tuple).
-export([run/0,run_stacks/0,probe/1]).
run() -> ordsets_core_support:run(?MODULE,cases()).
run_stacks() -> ordsets_core_support:profiles(?MODULE,cases()).
cases() -> [from_list_241,
            from_list_243,
            from_list_245,
            from_list_247,
            from_list_249,
            from_list_251,
            from_list_253,
            from_list_255,
            from_list_257,
            from_list_259,
            from_list_261,
            list_262,
            list_263,
            list_264,
            list_265,
            list_266].
probe(from_list_241) -> erlang:apply(erlang,opaque(list_to_tuple),[{}]);
probe(from_list_243) -> erlang:apply(erlang,opaque(list_to_tuple),[{a}]);
probe(from_list_245) -> erlang:apply(erlang,opaque(list_to_tuple),[{a,b}]);
probe(from_list_247) -> erlang:apply(erlang,opaque(list_to_tuple),[a]);
probe(from_list_249) -> erlang:apply(erlang,opaque(list_to_tuple),[[]]);
probe(from_list_251) -> erlang:apply(erlang,opaque(list_to_tuple),[[a]]);
probe(from_list_253) -> erlang:apply(erlang,opaque(list_to_tuple),[[a|tail]]);
probe(from_list_255) -> erlang:apply(erlang,opaque(list_to_tuple),[#{}]);
probe(from_list_257) -> erlang:apply(erlang,opaque(list_to_tuple),[<<>>]);
probe(from_list_259) -> erlang:apply(erlang,opaque(list_to_tuple),[<<1:9>>]);
probe(from_list_261) -> erlang:apply(erlang,opaque(list_to_tuple),[fun erlang:abs/1]);
probe(list_262) -> erlang:apply(erlang,opaque(list_to_tuple),[[]]);
probe(list_263) -> erlang:apply(erlang,opaque(list_to_tuple),[[a]]);
probe(list_264) -> erlang:apply(erlang,opaque(list_to_tuple),[[a,b]]);
probe(list_265) -> erlang:apply(erlang,opaque(list_to_tuple),[[{a,<<1:9>>},fun erlang:abs/1]]);
probe(list_266) -> erlang:apply(erlang,opaque(list_to_tuple),[[a|tail]]).
opaque(Value) -> put(tuple_mutation489_input,Value),erase(tuple_mutation489_input).
