-module(atom_contract492_atom_to_binary2_p1).
-export([run/0,run_stacks/0,probe/1]).
run() -> ordsets_core_support:run(?MODULE,cases()).
run_stacks() -> ordsets_core_support:profiles(?MODULE,cases()).
cases() -> [c80,c81,c82,c83,c84,c85,c86,c87,c88,c89,c90,c91,c92,c93,c94,c95,c96,c97,c98,c99].
probe(c80) -> erlang:apply(erlang,opaque(atom_to_binary),[fun erlang:abs/1,latin1]);
probe(c81) -> erlang:apply(erlang,opaque(atom_to_binary),[fun() -> ok end,utf8]);
probe(c82) -> erlang:apply(erlang,opaque(atom_to_binary),[fun() -> ok end,unicode]);
probe(c83) -> erlang:apply(erlang,opaque(atom_to_binary),[fun() -> ok end,latin1]);
probe(c84) -> erlang:apply(erlang,opaque(atom_to_binary),[binary_to_term(<<131,103,100,0,6,"oracle",0,0,0,1,0,0,0,0,0>>),utf8]);
probe(c85) -> erlang:apply(erlang,opaque(atom_to_binary),[binary_to_term(<<131,103,100,0,6,"oracle",0,0,0,1,0,0,0,0,0>>),unicode]);
probe(c86) -> erlang:apply(erlang,opaque(atom_to_binary),[binary_to_term(<<131,103,100,0,6,"oracle",0,0,0,1,0,0,0,0,0>>),latin1]);
probe(c87) -> erlang:apply(erlang,opaque(atom_to_binary),[binary_to_term(<<131,101,100,0,6,"oracle",0,0,0,1,0>>),utf8]);
probe(c88) -> erlang:apply(erlang,opaque(atom_to_binary),[binary_to_term(<<131,101,100,0,6,"oracle",0,0,0,1,0>>),unicode]);
probe(c89) -> erlang:apply(erlang,opaque(atom_to_binary),[binary_to_term(<<131,101,100,0,6,"oracle",0,0,0,1,0>>),latin1]);
probe(c90) -> erlang:apply(erlang,opaque(atom_to_binary),[binary_to_term(<<131,102,100,0,6,"oracle",0,0,0,1,0>>),utf8]);
probe(c91) -> erlang:apply(erlang,opaque(atom_to_binary),[binary_to_term(<<131,102,100,0,6,"oracle",0,0,0,1,0>>),unicode]);
probe(c92) -> erlang:apply(erlang,opaque(atom_to_binary),[binary_to_term(<<131,102,100,0,6,"oracle",0,0,0,1,0>>),latin1]);
probe(c93) -> erlang:apply(erlang,opaque(atom_to_binary),[a,a]);
probe(c94) -> erlang:apply(erlang,opaque(atom_to_binary),[a,0]);
probe(c95) -> erlang:apply(erlang,opaque(atom_to_binary),[a,1.0]);
probe(c96) -> erlang:apply(erlang,opaque(atom_to_binary),[a,[]]);
probe(c97) -> erlang:apply(erlang,opaque(atom_to_binary),[a,[utf8]]);
probe(c98) -> erlang:apply(erlang,opaque(atom_to_binary),[a,{}]);
probe(c99) -> erlang:apply(erlang,opaque(atom_to_binary),[a,<<>>]).
opaque(Value) -> put(atom_contract492_input,Value),erase(atom_contract492_input).
