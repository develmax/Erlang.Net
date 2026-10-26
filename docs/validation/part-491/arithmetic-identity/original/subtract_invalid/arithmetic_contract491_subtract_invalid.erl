-module(arithmetic_contract491_subtract_invalid).
-export([run/0,run_stacks/0,probe/1]).
run() -> ordsets_core_support:run(?MODULE,cases()).
run_stacks() -> ordsets_core_support:profiles(?MODULE,cases()).
cases() -> [c441,c442,c443,c444,c445,c446,c447,c448,c449,c450,c451,c452,c453,c454,c455,c456,c457,c458,c459,c460,c461,c462,c463,c464,c465,c466,c467,c468,c469,c470,c471,c472,c473,c474,c475,c476,c477,c478,c479,c480,c481,c482,c483,c484,c485,c486,c487,c488,c489,c490,c491].
probe(c441) -> erlang:apply(erlang,opaque('-'),[true,1]);
probe(c442) -> erlang:apply(erlang,opaque('-'),[1,true]);
probe(c443) -> erlang:apply(erlang,opaque('-'),[true,true]);
probe(c444) -> erlang:apply(erlang,opaque('-'),[false,1]);
probe(c445) -> erlang:apply(erlang,opaque('-'),[1,false]);
probe(c446) -> erlang:apply(erlang,opaque('-'),[false,false]);
probe(c447) -> erlang:apply(erlang,opaque('-'),[a,1]);
probe(c448) -> erlang:apply(erlang,opaque('-'),[1,a]);
probe(c449) -> erlang:apply(erlang,opaque('-'),[a,a]);
probe(c450) -> erlang:apply(erlang,opaque('-'),[[],1]);
probe(c451) -> erlang:apply(erlang,opaque('-'),[1,[]]);
probe(c452) -> erlang:apply(erlang,opaque('-'),[[],[]]);
probe(c453) -> erlang:apply(erlang,opaque('-'),[[a],1]);
probe(c454) -> erlang:apply(erlang,opaque('-'),[1,[a]]);
probe(c455) -> erlang:apply(erlang,opaque('-'),[[a],[a]]);
probe(c456) -> erlang:apply(erlang,opaque('-'),[[a|tail],1]);
probe(c457) -> erlang:apply(erlang,opaque('-'),[1,[a|tail]]);
probe(c458) -> erlang:apply(erlang,opaque('-'),[[a|tail],[a|tail]]);
probe(c459) -> erlang:apply(erlang,opaque('-'),[{},1]);
probe(c460) -> erlang:apply(erlang,opaque('-'),[1,{}]);
probe(c461) -> erlang:apply(erlang,opaque('-'),[{},{}]);
probe(c462) -> erlang:apply(erlang,opaque('-'),[{a},1]);
probe(c463) -> erlang:apply(erlang,opaque('-'),[1,{a}]);
probe(c464) -> erlang:apply(erlang,opaque('-'),[{a},{a}]);
probe(c465) -> erlang:apply(erlang,opaque('-'),[#{},1]);
probe(c466) -> erlang:apply(erlang,opaque('-'),[1,#{}]);
probe(c467) -> erlang:apply(erlang,opaque('-'),[#{},#{}]);
probe(c468) -> erlang:apply(erlang,opaque('-'),[#{a=>1},1]);
probe(c469) -> erlang:apply(erlang,opaque('-'),[1,#{a=>1}]);
probe(c470) -> erlang:apply(erlang,opaque('-'),[#{a=>1},#{a=>1}]);
probe(c471) -> erlang:apply(erlang,opaque('-'),[<<>>,1]);
probe(c472) -> erlang:apply(erlang,opaque('-'),[1,<<>>]);
probe(c473) -> erlang:apply(erlang,opaque('-'),[<<>>,<<>>]);
probe(c474) -> erlang:apply(erlang,opaque('-'),[<<1:1>>,1]);
probe(c475) -> erlang:apply(erlang,opaque('-'),[1,<<1:1>>]);
probe(c476) -> erlang:apply(erlang,opaque('-'),[<<1:1>>,<<1:1>>]);
probe(c477) -> erlang:apply(erlang,opaque('-'),[fun erlang:abs/1,1]);
probe(c478) -> erlang:apply(erlang,opaque('-'),[1,fun erlang:abs/1]);
probe(c479) -> erlang:apply(erlang,opaque('-'),[fun erlang:abs/1,fun erlang:abs/1]);
probe(c480) -> erlang:apply(erlang,opaque('-'),[fun() -> ok end,1]);
probe(c481) -> erlang:apply(erlang,opaque('-'),[1,fun() -> ok end]);
probe(c482) -> erlang:apply(erlang,opaque('-'),[fun() -> ok end,fun() -> ok end]);
probe(c483) -> erlang:apply(erlang,opaque('-'),[binary_to_term(<<131,103,100,0,6,"oracle",0,0,0,1,0,0,0,0,0>>),1]);
probe(c484) -> erlang:apply(erlang,opaque('-'),[1,binary_to_term(<<131,103,100,0,6,"oracle",0,0,0,1,0,0,0,0,0>>)]);
probe(c485) -> erlang:apply(erlang,opaque('-'),[binary_to_term(<<131,103,100,0,6,"oracle",0,0,0,1,0,0,0,0,0>>),binary_to_term(<<131,103,100,0,6,"oracle",0,0,0,1,0,0,0,0,0>>)]);
probe(c486) -> erlang:apply(erlang,opaque('-'),[binary_to_term(<<131,101,100,0,6,"oracle",0,0,0,1,0>>),1]);
probe(c487) -> erlang:apply(erlang,opaque('-'),[1,binary_to_term(<<131,101,100,0,6,"oracle",0,0,0,1,0>>)]);
probe(c488) -> erlang:apply(erlang,opaque('-'),[binary_to_term(<<131,101,100,0,6,"oracle",0,0,0,1,0>>),binary_to_term(<<131,101,100,0,6,"oracle",0,0,0,1,0>>)]);
probe(c489) -> erlang:apply(erlang,opaque('-'),[binary_to_term(<<131,102,100,0,6,"oracle",0,0,0,1,0>>),1]);
probe(c490) -> erlang:apply(erlang,opaque('-'),[1,binary_to_term(<<131,102,100,0,6,"oracle",0,0,0,1,0>>)]);
probe(c491) -> erlang:apply(erlang,opaque('-'),[binary_to_term(<<131,102,100,0,6,"oracle",0,0,0,1,0>>),binary_to_term(<<131,102,100,0,6,"oracle",0,0,0,1,0>>)]).
opaque(Value) -> put(arithmetic_contract491_input,Value),erase(arithmetic_contract491_input).
