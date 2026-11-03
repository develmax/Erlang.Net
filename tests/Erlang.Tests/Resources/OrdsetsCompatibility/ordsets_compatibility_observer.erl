-module(ordsets_compatibility_observer).
-export([run/0,identity/1,keep/1,drop/1,replace/1,invalid/1,crash/1,pair/2,crash_pair/2]).

run() -> observe(cases(), []).

observe([], Acc) -> Acc;
observe([{F,A}|Rest], Acc) ->
    Result = probe(F,A),
    observe(Rest, [{F,A,Result}|Acc]).

probe(F,A) ->
    put(events, []),
    Result = try apply(ordsets,F,A) of
        V -> {ok,V}
    catch C:R:S -> {exception,C,R,S}
    end,
    {Result,erase(events)}.

cases() ->
    Shapes = [[], bad, [1], [1.0], [0.0], [-0.0], [1,2], [2,1],
              [1,1.0], [1|tail], [1,2|tail], [2,1|tail],
              [a,b], [{a,1},{a,2}], [#{1=>a},#{1.0=>a}],
              [<<0:1>>,<<1:1>>], [[1],[1.0]], [#{a=>1},#{a=>1.0}]],
    Elements = [-1,0,0.0,-0.0,1,1.0,2,9007199254740993,9007199254740992.0,
                a,{}, {a,1},#{},#{1=>a},#{1.0=>a},[],[1],<<0:1>>,<<1:1>>,#{k0=>0,k1=>1,k2=>2,k3=>3,k4=>4,k5=>5,k6=>6,k7=>7,k8=>8,k9=>9,k10=>10,k11=>11,k12=>12,k13=>13,k14=>14,k15=>15,k16=>16,k17=>17,k18=>18,k19=>19,k20=>20,k21=>21,k22=>22,k23=>23,k24=>24,k25=>25,k26=>26,k27=>27,k28=>28,k29=>29,k30=>30,k31=>31}, #{k0=>0.0,k1=>1.0,k2=>2.0,k3=>3.0,k4=>4.0,k5=>5.0,k6=>6.0,k7=>7.0,k8=>8.0,k9=>9.0,k10=>10.0,k11=>11.0,k12=>12.0,k13=>13.0,k14=>14.0,k15=>15.0,k16=>16.0,k17=>17.0,k18=>18.0,k19=>19.0,k20=>20.0,k21=>21.0,k22=>22.0,k23=>23.0,k24=>24.0,k25=>25.0,k26=>26.0,k27=>27.0,k28=>28.0,k29=>29.0,k30=>30.0,k31=>31.0}, #{k0=>0,k1=>1,k2=>2,k3=>3,k4=>4,k5=>5,k6=>6,k7=>7,k8=>8,k9=>9,k10=>10,k11=>11,k12=>12,k13=>13,k14=>14,k15=>15,k16=>16,k17=>17,k18=>18,k19=>19,k20=>20,k21=>21,k22=>22,k23=>23,k24=>24,k25=>25,k26=>26,k27=>27,k28=>28,k29=>29,k30=>30,k31=>31,k32=>32}, #{k0=>0.0,k1=>1.0,k2=>2.0,k3=>3.0,k4=>4.0,k5=>5.0,k6=>6.0,k7=>7.0,k8=>8.0,k9=>9.0,k10=>10.0,k11=>11.0,k12=>12.0,k13=>13.0,k14=>14.0,k15=>15.0,k16=>16.0,k17=>17.0,k18=>18.0,k19=>19.0,k20=>20.0,k21=>21.0,k22=>22.0,k23=>23.0,k24=>24.0,k25=>25.0,k26=>26.0,k27=>27.0,k28=>28.0,k29=>29.0,k30=>30.0,k31=>31.0,k32=>32.0}, #{k0=>0,k1=>1,k2=>2,k3=>3,k4=>4,k5=>5,k6=>6,k7=>7,k8=>8,k9=>9,k10=>10,k11=>11,k12=>12,k13=>13,k14=>14,k15=>15,k16=>16,k17=>17,k18=>18,k19=>19,k20=>20,k21=>21,k22=>22,k23=>23,k24=>24,k25=>25,k26=>26,k27=>27,k28=>28,k29=>29,k30=>30,k31=>31,k32=>32,k33=>33,k34=>34,k35=>35,k36=>36,k37=>37,k38=>38,k39=>39,k40=>40,k41=>41,k42=>42,k43=>43,k44=>44,k45=>45,k46=>46,k47=>47,k48=>48,k49=>49,k50=>50,k51=>51,k52=>52,k53=>53,k54=>54,k55=>55,k56=>56,k57=>57,k58=>58,k59=>59,k60=>60,k61=>61,k62=>62,k63=>63}, #{k0=>0.0,k1=>1.0,k2=>2.0,k3=>3.0,k4=>4.0,k5=>5.0,k6=>6.0,k7=>7.0,k8=>8.0,k9=>9.0,k10=>10.0,k11=>11.0,k12=>12.0,k13=>13.0,k14=>14.0,k15=>15.0,k16=>16.0,k17=>17.0,k18=>18.0,k19=>19.0,k20=>20.0,k21=>21.0,k22=>22.0,k23=>23.0,k24=>24.0,k25=>25.0,k26=>26.0,k27=>27.0,k28=>28.0,k29=>29.0,k30=>30.0,k31=>31.0,k32=>32.0,k33=>33.0,k34=>34.0,k35=>35.0,k36=>36.0,k37=>37.0,k38=>38.0,k39=>39.0,k40=>40.0,k41=>41.0,k42=>42.0,k43=>43.0,k44=>44.0,k45=>45.0,k46=>46.0,k47=>47.0,k48=>48.0,k49=>49.0,k50=>50.0,k51=>51.0,k52=>52.0,k53=>53.0,k54=>54.0,k55=>55.0,k56=>56.0,k57=>57.0,k58=>58.0,k59=>59.0,k60=>60.0,k61=>61.0,k62=>62.0,k63=>63.0}, fun erlang:abs/1, fun erlang:round/1, fun ordsets_compatibility_observer:identity/1],
    [{new,[]}] ++
    [{F,[S]} || F <- [is_set,size,is_empty,to_list,from_list,union,intersection], S <- Shapes] ++
    [{F,[X,Y]} || F <- [is_equal,union,intersection,is_disjoint,subtract,is_subset], X <- Shapes, Y <- Shapes] ++
    [{F,[E,S]} || F <- [is_element,add_element,del_element], E <- Elements, S <- Shapes] ++
    callback_cases(Shapes) ++ comparison_cases(Elements) ++
    many_cases(Shapes) ++ sort_cases().

many_cases(Shapes) ->
    [{F,[[X,Y]]} || F <- [union,intersection], X <- Shapes, Y <- Shapes] ++
    [{F,[[X,Y,Z]]} || F <- [union,intersection], X <- [[],[1],[1.0],[2],[1,2]],
        Y <- [[],[1],[1.0],[2],[1,2]], Z <- [[],[1],[1.0],[2],[1,2]]] ++
    [{F,[[X,Y|tail]]} || F <- [union,intersection], X <- Shapes, Y <- Shapes].

sort_cases() ->
    [{from_list,[sequence(N,[])]} || N <- sequence(96,[])] ++
    [{from_list,[alternating(N,[])]} || N <- sequence(96,[])] ++
    [{union,[[sequence(N,[]),alternating(N,[])]]} || N <- sequence(96,[])].

sequence(0,Acc) -> Acc;
sequence(N,Acc) -> sequence(N-1,[N|Acc]).
alternating(0,Acc) -> Acc;
alternating(N,Acc) -> alternating(N-1,[N rem 7, float(N rem 7)|Acc]).

comparison_cases(Elements) ->
    [{F,[[X],[Y]]} || F <- [is_equal,union,intersection,is_disjoint,subtract,is_subset], X <- Elements, Y <- Elements] ++
    [{from_list,[[X,Y,X]]} || X <- Elements, Y <- Elements] ++
    [{is_set,[[X,Y]]} || X <- Elements, Y <- Elements].

callback_cases(Shapes) ->
    Unary = [fun ordsets_compatibility_observer:identity/1, fun ordsets_compatibility_observer:keep/1, fun ordsets_compatibility_observer:drop/1, fun ordsets_compatibility_observer:replace/1,
             fun ordsets_compatibility_observer:invalid/1, fun ordsets_compatibility_observer:crash/1, fun erlang:abs/1, bad, fun ordsets_compatibility_observer:pair/2],
    [{F,[Fun,S]} || F <- [filter,map,filtermap], Fun <- Unary, S <- Shapes] ++
    [{fold,[Fun,initial,S]} || Fun <- [fun ordsets_compatibility_observer:pair/2,fun ordsets_compatibility_observer:crash_pair/2,bad,fun ordsets_compatibility_observer:identity/1], S <- Shapes].

identity(X) -> log(X), X.
keep(X) -> log(X), true.
drop(X) -> log(X), false.
replace(X) -> log(X), {true,0}.
invalid(X) -> log(X), invalid.
crash(X) -> log(X), erlang:error({callback,X}).
pair(X,A) -> log({X,A}), {X,A}.
crash_pair(X,A) -> log({X,A}), throw({callback,X,A}).
log(X) -> put(events,[X|get(events)]).
