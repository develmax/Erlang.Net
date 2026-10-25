-module(arithmetic_contract491_integer_and_identity).
-export([run/0,run_stacks/0,probe/1]).
run() -> ordsets_core_support:run(?MODULE,cases()).
run_stacks() -> ordsets_core_support:profiles(?MODULE,cases()).
cases() -> [c244,c245,c246,c247,c248,c249,c250,c251,c252,c253,c254,c255,c256,c257,c258,c259].
probe(c244) -> A=opaque((1 bsl 80)), B=opaque(0), R=erlang:apply(erlang,opaque('band'),[A,B]), erts_debug:same(R,A);
probe(c245) -> A=opaque((1 bsl 80)), B=opaque(0), R=erlang:apply(erlang,opaque('band'),[A,B]), erts_debug:same(R,B);
probe(c246) -> A=opaque(0), B=opaque((1 bsl 80)), R=erlang:apply(erlang,opaque('band'),[A,B]), erts_debug:same(R,A);
probe(c247) -> A=opaque(0), B=opaque((1 bsl 80)), R=erlang:apply(erlang,opaque('band'),[A,B]), erts_debug:same(R,B);
probe(c248) -> A=opaque((1 bsl 80)), B=opaque(1), R=erlang:apply(erlang,opaque('band'),[A,B]), erts_debug:same(R,A);
probe(c249) -> A=opaque((1 bsl 80)), B=opaque(1), R=erlang:apply(erlang,opaque('band'),[A,B]), erts_debug:same(R,B);
probe(c250) -> A=opaque(1), B=opaque((1 bsl 80)), R=erlang:apply(erlang,opaque('band'),[A,B]), erts_debug:same(R,A);
probe(c251) -> A=opaque(1), B=opaque((1 bsl 80)), R=erlang:apply(erlang,opaque('band'),[A,B]), erts_debug:same(R,B);
probe(c252) -> A=opaque((1 bsl 80)), B=opaque((1 bsl 81)), R=erlang:apply(erlang,opaque('band'),[A,B]), erts_debug:same(R,A);
probe(c253) -> A=opaque((1 bsl 80)), B=opaque((1 bsl 81)), R=erlang:apply(erlang,opaque('band'),[A,B]), erts_debug:same(R,B);
probe(c254) -> A=opaque(-(1 bsl 80)), B=opaque((1 bsl 81)), R=erlang:apply(erlang,opaque('band'),[A,B]), erts_debug:same(R,A);
probe(c255) -> A=opaque(-(1 bsl 80)), B=opaque((1 bsl 81)), R=erlang:apply(erlang,opaque('band'),[A,B]), erts_debug:same(R,B);
probe(c256) -> A=opaque((1 bsl 80)), B=opaque((1 bsl 79)), R=erlang:apply(erlang,opaque('band'),[A,B]), erts_debug:same(R,A);
probe(c257) -> A=opaque((1 bsl 80)), B=opaque((1 bsl 79)), R=erlang:apply(erlang,opaque('band'),[A,B]), erts_debug:same(R,B);
probe(c258) -> A=opaque((1 bsl 80)), B=opaque((1 bsl 80)), R=erlang:apply(erlang,opaque('band'),[A,B]), erts_debug:same(R,A);
probe(c259) -> A=opaque((1 bsl 80)), B=opaque((1 bsl 80)), R=erlang:apply(erlang,opaque('band'),[A,B]), erts_debug:same(R,B).
opaque(Value) -> put(arithmetic_contract491_input,Value),erase(arithmetic_contract491_input).
