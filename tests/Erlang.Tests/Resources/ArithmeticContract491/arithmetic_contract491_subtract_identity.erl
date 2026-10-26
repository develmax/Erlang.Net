-module(arithmetic_contract491_subtract_identity).
-export([run/0,run_stacks/0,probe/1]).
run() -> ordsets_core_support:run(?MODULE,cases()).
run_stacks() -> ordsets_core_support:profiles(?MODULE,cases()).
cases() -> [c492,c493,c494,c495,c496,c497,c498,c499,c500,c501,c502,c503,c504,c505,c506,c507].
probe(c492) -> A=opaque((1 bsl 80)), B=opaque(0), R=erlang:apply(erlang,opaque('-'),[A,B]), erts_debug:same(R,A);
probe(c493) -> A=opaque((1 bsl 80)), B=opaque(0), R=erlang:apply(erlang,opaque('-'),[A,B]), erts_debug:same(R,B);
probe(c494) -> A=opaque(0), B=opaque((1 bsl 80)), R=erlang:apply(erlang,opaque('-'),[A,B]), erts_debug:same(R,A);
probe(c495) -> A=opaque(0), B=opaque((1 bsl 80)), R=erlang:apply(erlang,opaque('-'),[A,B]), erts_debug:same(R,B);
probe(c496) -> A=opaque((1 bsl 80)), B=opaque(1), R=erlang:apply(erlang,opaque('-'),[A,B]), erts_debug:same(R,A);
probe(c497) -> A=opaque((1 bsl 80)), B=opaque(1), R=erlang:apply(erlang,opaque('-'),[A,B]), erts_debug:same(R,B);
probe(c498) -> A=opaque(1), B=opaque((1 bsl 80)), R=erlang:apply(erlang,opaque('-'),[A,B]), erts_debug:same(R,A);
probe(c499) -> A=opaque(1), B=opaque((1 bsl 80)), R=erlang:apply(erlang,opaque('-'),[A,B]), erts_debug:same(R,B);
probe(c500) -> A=opaque((1 bsl 80)), B=opaque((1 bsl 81)), R=erlang:apply(erlang,opaque('-'),[A,B]), erts_debug:same(R,A);
probe(c501) -> A=opaque((1 bsl 80)), B=opaque((1 bsl 81)), R=erlang:apply(erlang,opaque('-'),[A,B]), erts_debug:same(R,B);
probe(c502) -> A=opaque(-(1 bsl 80)), B=opaque((1 bsl 81)), R=erlang:apply(erlang,opaque('-'),[A,B]), erts_debug:same(R,A);
probe(c503) -> A=opaque(-(1 bsl 80)), B=opaque((1 bsl 81)), R=erlang:apply(erlang,opaque('-'),[A,B]), erts_debug:same(R,B);
probe(c504) -> A=opaque((1 bsl 80)), B=opaque((1 bsl 79)), R=erlang:apply(erlang,opaque('-'),[A,B]), erts_debug:same(R,A);
probe(c505) -> A=opaque((1 bsl 80)), B=opaque((1 bsl 79)), R=erlang:apply(erlang,opaque('-'),[A,B]), erts_debug:same(R,B);
probe(c506) -> A=opaque((1 bsl 80)), B=opaque((1 bsl 80)), R=erlang:apply(erlang,opaque('-'),[A,B]), erts_debug:same(R,A);
probe(c507) -> A=opaque((1 bsl 80)), B=opaque((1 bsl 80)), R=erlang:apply(erlang,opaque('-'),[A,B]), erts_debug:same(R,B).
opaque(Value) -> put(arithmetic_contract491_input,Value),erase(arithmetic_contract491_input).
