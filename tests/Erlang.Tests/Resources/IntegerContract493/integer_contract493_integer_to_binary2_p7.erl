-module(integer_contract493_integer_to_binary2_p7).
-export([run/0,run_stacks/0,probe/1]).
run() -> ordsets_core_support:run(?MODULE,cases()).
run_stacks() -> ordsets_core_support:profiles(?MODULE,cases()).
cases() -> [c560,c561,c562,c563,c564,c565,c566,c567,c568,c569,c570,c571,c572,c573,c574,c575,c576,c577,c578,c579,c580,c581,c582,c583,c584,c585].
probe(c560) -> erlang:apply(erlang,opaque(integer_to_binary),[{a},16]);
probe(c561) -> erlang:apply(erlang,opaque(integer_to_binary),[{a},36]);
probe(c562) -> erlang:apply(erlang,opaque(integer_to_binary),[#{},2]);
probe(c563) -> erlang:apply(erlang,opaque(integer_to_binary),[#{},10]);
probe(c564) -> erlang:apply(erlang,opaque(integer_to_binary),[#{},16]);
probe(c565) -> erlang:apply(erlang,opaque(integer_to_binary),[#{},36]);
probe(c566) -> erlang:apply(erlang,opaque(integer_to_binary),[<<49>>,2]);
probe(c567) -> erlang:apply(erlang,opaque(integer_to_binary),[<<49>>,10]);
probe(c568) -> erlang:apply(erlang,opaque(integer_to_binary),[<<49>>,16]);
probe(c569) -> erlang:apply(erlang,opaque(integer_to_binary),[<<49>>,36]);
probe(c570) -> erlang:apply(erlang,opaque(integer_to_binary),[<<1:1>>,2]);
probe(c571) -> erlang:apply(erlang,opaque(integer_to_binary),[<<1:1>>,10]);
probe(c572) -> erlang:apply(erlang,opaque(integer_to_binary),[<<1:1>>,16]);
probe(c573) -> erlang:apply(erlang,opaque(integer_to_binary),[<<1:1>>,36]);
probe(c574) -> erlang:apply(erlang,opaque(integer_to_binary),[fun erlang:abs/1,2]);
probe(c575) -> erlang:apply(erlang,opaque(integer_to_binary),[fun erlang:abs/1,10]);
probe(c576) -> erlang:apply(erlang,opaque(integer_to_binary),[fun erlang:abs/1,16]);
probe(c577) -> erlang:apply(erlang,opaque(integer_to_binary),[fun erlang:abs/1,36]);
probe(c578) -> erlang:apply(erlang,opaque(integer_to_binary),[binary_to_term(<<131,103,100,0,6,"oracle",0,0,0,1,0,0,0,0,0>>),2]);
probe(c579) -> erlang:apply(erlang,opaque(integer_to_binary),[binary_to_term(<<131,103,100,0,6,"oracle",0,0,0,1,0,0,0,0,0>>),10]);
probe(c580) -> erlang:apply(erlang,opaque(integer_to_binary),[binary_to_term(<<131,103,100,0,6,"oracle",0,0,0,1,0,0,0,0,0>>),16]);
probe(c581) -> erlang:apply(erlang,opaque(integer_to_binary),[binary_to_term(<<131,103,100,0,6,"oracle",0,0,0,1,0,0,0,0,0>>),36]);
probe(c582) -> erlang:apply(erlang,opaque(integer_to_binary),[binary_to_term(<<131,102,100,0,6,"oracle",0,0,0,1,0>>),2]);
probe(c583) -> erlang:apply(erlang,opaque(integer_to_binary),[binary_to_term(<<131,102,100,0,6,"oracle",0,0,0,1,0>>),10]);
probe(c584) -> erlang:apply(erlang,opaque(integer_to_binary),[binary_to_term(<<131,102,100,0,6,"oracle",0,0,0,1,0>>),16]);
probe(c585) -> erlang:apply(erlang,opaque(integer_to_binary),[binary_to_term(<<131,102,100,0,6,"oracle",0,0,0,1,0>>),36]).
opaque(Value) -> put(integer_contract493_input,Value),erase(integer_contract493_input).
