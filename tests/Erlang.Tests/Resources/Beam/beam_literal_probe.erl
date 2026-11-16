-module(beam_literal_probe).
-export([tuple/0,list/0,binary/0,map/0,repeat/0,plus/0,minus/0,big/0]).
-zeta(first).
-alpha([a,b]).
-zeta(second).
-vsn("literal-v1").
tuple() -> {ok, [1,2], <<1,2>>}.
list() -> [alpha, beta | gamma].
binary() -> <<1,2,3:4>>.
map() -> #{b => 2, a => 1}.
repeat() -> {ok, [1,2], <<1,2>>}.
plus() -> 0.0.
minus() -> -0.0.
big() -> 123456789012345678901234567890.
