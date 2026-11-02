-module(native_append206_observer).
-export([run/0,worker/1]).
-file("native_append206_observer.erl",1).

worker(Parent) -> Parent ! {finished,run()}.
run() ->
    [observe({Op,L,R},fun()->apply_append(Op,L,R) end)
     || Op <- [plusplus,append], L <- lefts(), R <- rights()] ++
    [{sharing,Op,sharing(Op)} || Op <- [plusplus,append]].

lefts() -> [[],[1],[1,2],[{head,<<1,2>>},1 bsl 70],[1|bad],bad,0,#{a=>1},<<>>].
rights() -> [[],[tail],[tail|improper],tail,0,1.5,<<1,2>>,{tail,[1]},#{k=>[2]}].

apply_append(plusplus,L,R) -> L ++ R;
apply_append(append,L,R) -> erlang:append(L,R).

sharing(Op) ->
    H={head,<<37:1040>>}, L=[H,{other,2}], R=[{right,<<38:1040>>}],
    V=apply_append(Op,L,R),
    {erts_debug:same(apply_append(Op,[],R),R),
     erts_debug:same(apply_append(Op,L,[]),L),
     not erts_debug:same(V,L),
     erts_debug:same(hd(V),hd(L)),
     erts_debug:same(lists:nthtail(2,V),R),
     apply_append(Op,[1,2],tail)}.

observe(Name,F) ->
    try F() of V -> {Name,{returned,V}}
    catch C:R:S -> {Name,{caught,C,R,S}}
    end.
