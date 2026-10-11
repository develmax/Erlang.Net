namespace Erlang.Differential;

public static class MapsCompletionCases
{
    private static CompiledModuleCase Case(string name, string body) => new(
        "maps-completion-" + name,
        "-module(oracle_maps_completion). -export([run/0]). run()->" + body + ",done.",
        Term.Tuple(Term.A("ok"), Term.A("done"))
    );

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>
        {
            Case("intersect", "#{a:=right}=maps:intersect(#{a=>left,b=>only_left},#{a=>right,c=>only_right})"),
            Case(
                "intersect-swap",
                "#{a:=left}=maps:intersect(#{a=>right},#{a=>left,b=>extra}),#{a:=right}=maps:intersect(#{a=>left,b=>extra},#{a=>right})"
            ),
            Case("intersect-empty", "0=maps:size(maps:intersect(#{},#{a=>1})),0=maps:size(maps:intersect(#{a=>1},#{}))"),
            Case(
                "intersect-exact-keys",
                "#{1:=right_integer,1.0:=right_float}=maps:intersect(#{1=>integer,1.0=>float},#{1=>right_integer,1.0=>right_float}),0=maps:size(maps:intersect(#{1=>integer},#{1.0=>float}))"
            ),
            Case(
                "intersect-with",
                "#{a:={a,left,right}}=maps:intersect_with(fun(K,L,R)->{K,L,R} end,#{a=>left,b=>other},#{a=>right,c=>other})"
            ),
            Case(
                "intersect-with-first-smaller",
                "#{a:={left,right}}=maps:intersect_with(fun(_,L,R)->{L,R} end,#{a=>left},#{a=>right,b=>extra})"
            ),
            Case(
                "intersect-with-second-smaller",
                "#{a:={left,right}}=maps:intersect_with(fun(_,L,R)->{L,R} end,#{a=>left,b=>extra},#{a=>right})"
            ),
            Case(
                "intersect-with-disjoint",
                "0=maps:size(maps:intersect_with(fun(_,_,_)->error(called) end,#{a=>1},#{b=>2})),0=maps:size(maps:intersect_with(fun(_,_,_)->error(called) end,#{},#{}))"
            ),
            Case(
                "intersect-with-callback-once",
                "put(calls,0),#{a:=3}=maps:intersect_with(fun(_,L,R)->put(calls,get(calls)+1),L+R end,#{a=>1,b=>4},#{a=>2,c=>5}),1=get(calls)"
            ),
            Case(
                "merge-with",
                "#{a:={a,left,right},b:=only_left,c:=only_right}=maps:merge_with(fun(K,L,R)->{K,L,R} end,#{a=>left,b=>only_left},#{a=>right,c=>only_right})"
            ),
            Case(
                "merge-with-first-smaller",
                "#{a:={left,right},b:=extra}=maps:merge_with(fun(_,L,R)->{L,R} end,#{a=>left},#{a=>right,b=>extra})"
            ),
            Case(
                "merge-with-second-smaller",
                "#{a:={left,right},b:=extra}=maps:merge_with(fun(_,L,R)->{L,R} end,#{a=>left,b=>extra},#{a=>right})"
            ),
            Case(
                "merge-with-disjoint",
                "#{a:=1,b:=2}=maps:merge_with(fun(_,_,_)->error(called) end,#{a=>1},#{b=>2}),0=maps:size(maps:merge_with(fun(_,_,_)->error(called) end,#{},#{}))"
            ),
            Case(
                "merge-with-exact-keys",
                "#{1:={integer,right_integer},1.0:={float,right_float}}=maps:merge_with(fun(_,L,R)->{L,R} end,#{1=>integer,1.0=>float},#{1=>right_integer,1.0=>right_float})"
            ),
            Case(
                "merge-with-immutable",
                "L=#{a=>1},R=#{a=>2,b=>3},#{a:=3,b:=3}=maps:merge_with(fun(_,A,B)->A+B end,L,R),1=maps:get(a,L),2=maps:get(a,R)"
            ),
            Case("groups-two", "#{0:=[2,4],1:=[1,3]}=maps:groups_from_list(fun(X)->X rem 2 end,[1,2,3,4])"),
            Case("groups-three", "#{0:=[4,16],1:=[1,9]}=maps:groups_from_list(fun(X)->X rem 2 end,fun(X)->X*X end,[1,2,3,4])"),
            Case(
                "groups-empty",
                "0=maps:size(maps:groups_from_list(fun(_)->error(called) end,[])),0=maps:size(maps:groups_from_list(fun(_)->error(called) end,fun(_)->error(called) end,[]))"
            ),
            Case("groups-exact-keys", "#{1:=[1,1],1.0:=[1.0]}=maps:groups_from_list(fun(X)->X end,[1,1.0,1])"),
            Case("groups-composite-keys", "#{[a]:=[[a],[a]],{a}:=[{a}]}=maps:groups_from_list(fun(X)->X end,[[a],{a},[a]])"),
            Case(
                "groups-reverse-callbacks",
                "put(order,[]),#{same:=[1,2,3]}=maps:groups_from_list(fun(X)->put(order,[X|get(order)]),same end,[1,2,3]),[1,2,3]=get(order)"
            ),
            Case(
                "groups-key-before-value",
                "put(order,[]),#{same:=[10,20,30]}=maps:groups_from_list(fun(X)->put(order,[{key,X}|get(order)]),same end,fun(X)->put(order,[{value,X}|get(order)]),X*10 end,[1,2,3]),[{value,1},{key,1},{value,2},{key,2},{value,3},{key,3}]=get(order)"
            ),
            Case(
                "groups-improper-no-callback",
                "put(called,false),try maps:groups_from_list(fun(_)->put(called,true),same end,[a|wrong]) of _->unexpected catch error:badarg->ok end,false=get(called)"
            ),
            Case(
                "groups-three-improper-no-callback",
                "put(called,false),try maps:groups_from_list(fun(_)->put(called,true),same end,fun(_)->put(called,true),value end,[a|wrong]) of _->unexpected catch error:badarg->ok end,false=get(called)"
            ),
            Case(
                "groups-key-error",
                "try maps:groups_from_list(fun(_)->error(callback_failed) end,[a]) of _->unexpected catch error:callback_failed->ok end"
            ),
            Case(
                "groups-value-error",
                "try maps:groups_from_list(fun(_)->same end,fun(_)->error(callback_failed) end,[a]) of _->unexpected catch error:callback_failed->ok end"
            ),
            Case(
                "groups-value-not-called-after-key-error",
                "put(called,false),try maps:groups_from_list(fun(_)->error(key_failed) end,fun(_)->put(called,true),value end,[a]) of _->unexpected catch error:key_failed->ok end,false=get(called)"
            ),
            Case("groups-throw", "try maps:groups_from_list(fun(_)->throw(thrown) end,[a]) of _->unexpected catch throw:thrown->ok end"),
            Case(
                "groups-exit",
                "try maps:groups_from_list(fun(_)->same end,fun(_)->exit(exited) end,[a]) of _->unexpected catch exit:exited->ok end"
            ),
            Case("to-list-ordered", "[{a,1},{b,2}]=maps:to_list(maps:iterator(#{a=>1,b=>2},ordered))"),
            Case("to-list-reversed", "[{b,2},{a,1}]=maps:to_list(maps:iterator(#{a=>1,b=>2},reversed))"),
            Case("to-list-custom", "[{b,2},{a,1}]=maps:to_list(maps:iterator(#{a=>1,b=>2},fun(A,B)->A>B end))"),
            Case("to-list-none", "put(input,none),[]=maps:to_list(get(input))"),
            Case("to-list-tuple-chain", "put(input,{a,1,{b,2,none}}),[{a,1},{b,2}]=maps:to_list(get(input))"),
            Case(
                "to-list-bad-continuation",
                "put(input,{a,1,wrong}),T=get(input),try maps:to_list(T) of _->unexpected catch error:{badmap,T}->ok end"
            ),
            Case(
                "to-list-missing-key",
                "put(input,[[missing]|#{a=>1}]),T=get(input),try maps:to_list(T) of _->unexpected catch error:{badmap,T}->ok end"
            ),
            Case(
                "iterator-valid-first-only",
                "I=[[a,missing]|#{a=>1}],true=maps:is_iterator_valid(I),try maps:to_list(I) of _->unexpected catch error:{badmap,I}->ok end"
            ),
            Case(
                "all-iteration-consistency-small",
                "M=#{a=>1,b=>2,z=>26,m=>13},K=maps:keys(M),V=maps:values(M),V=[maps:get(X,M)||X<-K],P=maps:to_list(M),P=[{X,maps:get(X,M)}||X<-K],P=maps:to_list(maps:iterator(M)),P=[{X,Y}||X:=Y<-M]"
            ),
            Case(
                "all-iteration-consistency-large",
                "M=maps:from_list([{X,X*10}||X<-lists:seq(1,256)]),K=maps:keys(M),V=maps:values(M),V=[maps:get(X,M)||X<-K],P=maps:to_list(M),P=maps:to_list(maps:iterator(M)),P=[{X,Y}||X:=Y<-M],256=maps:size(M)"
            ),
            Case(
                "combine-large",
                "A=maps:from_list([{X,X}||X<-lists:seq(1,128)]),B=maps:from_list([{X,X*10}||X<-lists:seq(65,192)]),I=maps:intersect(A,B),64=maps:size(I),650=maps:get(65,I),J=maps:intersect_with(fun(_,L,R)->L+R end,A,B),715=maps:get(65,J),U=maps:merge_with(fun(_,L,R)->L+R end,A,B),192=maps:size(U),1=maps:get(1,U),1920=maps:get(192,U),715=maps:get(65,U)"
            ),
            Case(
                "groups-large",
                "G=maps:groups_from_list(fun(X)->X rem 2 end,lists:seq(1,256)),2=maps:size(G),128=length(maps:get(0,G)),128=length(maps:get(1,G)),2=hd(maps:get(0,G)),1=hd(maps:get(1,G))"
            )
        };
        foreach (string operation in new[] { "intersect_with", "merge_with" })
        {
            cases.Add(Case(
                operation + "-bad-callback-precedence",
                "try maps:" + operation + "(wrong,first_wrong,second_wrong) of _->unexpected catch error:badarg->ok end"
            ));
            cases.Add(Case(
                operation + "-bad-arity-empty",
                "try maps:" + operation + "(fun(_,_) -> wrong end,#{},#{}) of _->unexpected catch error:badarg->ok end"
            ));
            cases.Add(Case(
                operation + "-first-bad-map",
                "try maps:" + operation + "(fun(_,_,_)->value end,first_wrong,second_wrong) of _->unexpected catch error:{badmap,first_wrong}->ok end"
            ));
            cases.Add(Case(
                operation + "-second-bad-map",
                "try maps:" + operation + "(fun(_,_,_)->value end,#{},second_wrong) of _->unexpected catch error:{badmap,second_wrong}->ok end"
            ));
            cases.Add(Case(
                operation + "-callback-error",
                "try maps:" + operation + "(fun(_,_,_)->error(callback_failed) end,#{a=>1},#{a=>2}) of _->unexpected catch error:callback_failed->ok end"
            ));
            cases.Add(Case(
                operation + "-callback-throw",
                "try maps:" + operation + "(fun(_,_,_)->throw(thrown) end,#{a=>1},#{a=>2}) of _->unexpected catch throw:thrown->ok end"
            ));
            cases.Add(Case(
                operation + "-callback-exit",
                "try maps:" + operation + "(fun(_,_,_)->exit(exited) end,#{a=>1},#{a=>2}) of _->unexpected catch exit:exited->ok end"
            ));
        }
        cases.Add(Case(
            "intersect-bad-map-precedence",
            "try maps:intersect(first_wrong,second_wrong) of _->unexpected catch error:{badmap,first_wrong}->ok end,try maps:intersect(#{},second_wrong) of _->unexpected catch error:{badmap,second_wrong}->ok end"
        ));
        cases.Add(Case(
            "groups-bad-fun",
            "try maps:groups_from_list(wrong,[]) of _->unexpected catch error:badarg->ok end,try maps:groups_from_list(fun(_,_) -> wrong end,[]) of _->unexpected catch error:badarg->ok end"
        ));
        cases.Add(Case(
            "groups-three-bad-funs",
            "try maps:groups_from_list(wrong,fun(X)->X end,[]) of _->unexpected catch error:badarg->ok end,try maps:groups_from_list(fun(X)->X end,wrong,[]) of _->unexpected catch error:badarg->ok end"
        ));
        cases.Add(Case(
            "groups-bad-list",
            "try maps:groups_from_list(fun(X)->X end,wrong) of _->unexpected catch error:badarg->ok end,try maps:groups_from_list(fun(X)->X end,fun(X)->X end,wrong) of _->unexpected catch error:badarg->ok end"
        ));

        cases.Add(Case(
            "ordered-numeric-key-ranks",
            "M=#{-1.0=>a,0.0=>b,-1=>c,0=>d},[{-1,c},{0,d},{-1.0,a},{0.0,b}]=maps:to_list(maps:iterator(M,ordered)),[{0.0,b},{-1.0,a},{0,d},{-1,c}]=maps:to_list(maps:iterator(M,reversed))"
        ));
        cases.Add(Case(
            "composite-map-keys",
            "A=#{x=>1},B=#{x=>1.0},2=maps:size(maps:from_keys([A,B],value)),#{A:=[A],B:=[B]}=maps:groups_from_list(fun(X)->X end,[A,B])"
        ));
        cases.Add(Case(
            "from-keys-bad-list",
            "try maps:from_keys(wrong,value) of _->unexpected catch error:badarg->ok end,try maps:from_keys([a|wrong],value) of _->unexpected catch error:badarg->ok end"
        ));
        cases.Add(Case(
            "iterator-bad-options",
            "try maps:iterator(wrong,ordered) of _->unexpected catch error:badarg->ok end,try maps:iterator(#{},wrong) of _->unexpected catch error:badarg->ok end,try maps:iterator(#{},fun(_)->true end) of _->unexpected catch error:badarg->ok end"
        ));
        cases.Add(Case("next-tuple-pass-through", "T={a,b,wrong},T=maps:next(T),false=maps:is_iterator_valid(T),none=maps:next(none)"));
        cases.Add(Case(
            "next-bad-iterator",
            "try maps:next(wrong) of _->unexpected catch error:badarg->ok end,try maps:next([]) of _->unexpected catch error:badarg->ok end"
        ));
        cases.Add(Case(
            "foreach-ignores-return",
            "put(called,false),ok=maps:foreach(fun(_,_) ->put(called,true),#{anything=>returned} end,#{a=>1}),true=get(called)"
        ));
        cases.Add(Case(
            "merge-bad-map-precedence",
            "try maps:merge(first_wrong,second_wrong) of _->unexpected catch error:{badmap,first_wrong}->ok end,try maps:merge(#{},second_wrong) of _->unexpected catch error:{badmap,second_wrong}->ok end"
        ));
        foreach (string call in new[] { "get(k,wrong)", "get(k,wrong,default)", "find(k,wrong)", "is_key(k,wrong)", "keys(wrong)", "values(wrong)", "size(wrong)", "put(k,v,wrong)", "remove(k,wrong)", "take(k,wrong)", "update(k,v,wrong)", "to_list(wrong)", "iterator(wrong)" })
            cases.Add(Case(
                "bad-map-" + call[..call.IndexOf('(')] + "-" + call.Count(value => value == ','),
                "try maps:" + call + " of _->unexpected catch error:{badmap,wrong}->ok end"
            ));
        foreach (string operation in new[] { "filter", "filtermap", "map", "foreach" })
            cases.Add(Case(operation + "-bad-callback-before-map", "try maps:" + operation + "(wrong,wrong) of _->unexpected catch error:badarg->ok end"));
        cases.Add(Case("fold-bad-callback-before-map", "try maps:fold(wrong,seed,wrong) of _->unexpected catch error:badarg->ok end"));

        return cases;
    }
}
