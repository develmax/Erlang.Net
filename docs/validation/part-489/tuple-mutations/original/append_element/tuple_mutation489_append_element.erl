-module(tuple_mutation489_append_element).
-export([run/0,run_stacks/0,probe/1]).
run() -> ordsets_core_support:run(?MODULE,cases()).
run_stacks() -> ordsets_core_support:profiles(?MODULE,cases()).
cases() -> [append_240,
            append_242,
            append_244,
            append_246,
            append_248,
            append_250,
            append_252,
            append_254,
            append_256,
            append_258,
            append_260].
probe(append_240) -> erlang:apply(erlang,opaque(append_element),[{},{new,<<1:9>>}]);
probe(append_242) -> erlang:apply(erlang,opaque(append_element),[{a},{new,<<1:9>>}]);
probe(append_244) -> erlang:apply(erlang,opaque(append_element),[{a,b},{new,<<1:9>>}]);
probe(append_246) -> erlang:apply(erlang,opaque(append_element),[a,{new,<<1:9>>}]);
probe(append_248) -> erlang:apply(erlang,opaque(append_element),[[],{new,<<1:9>>}]);
probe(append_250) -> erlang:apply(erlang,opaque(append_element),[[a],{new,<<1:9>>}]);
probe(append_252) -> erlang:apply(erlang,opaque(append_element),[[a|tail],{new,<<1:9>>}]);
probe(append_254) -> erlang:apply(erlang,opaque(append_element),[#{},{new,<<1:9>>}]);
probe(append_256) -> erlang:apply(erlang,opaque(append_element),[<<>>,{new,<<1:9>>}]);
probe(append_258) -> erlang:apply(erlang,opaque(append_element),[<<1:9>>,{new,<<1:9>>}]);
probe(append_260) -> erlang:apply(erlang,opaque(append_element),[fun erlang:abs/1,{new,<<1:9>>}]).
opaque(Value) -> put(tuple_mutation489_input,Value),erase(tuple_mutation489_input).
