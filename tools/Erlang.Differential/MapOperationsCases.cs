namespace Erlang.Differential;

public static class MapOperationsCases
{
    private static CompiledModuleCase Case(string name, string body) => new(
        "maps-operations-" + name,
        "-module(oracle_map_operations). -export([run/0]). run()->" + body + ",done.",
        Term.Tuple(Term.A("ok"), Term.A("done"))
    );

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } =
    [
        Case("get-default", "missing=maps:get(absent,#{},missing),value=maps:get(key,#{key=>value},missing)"),
        Case("find", "{ok,value}=maps:find(key,#{key=>value}),error=maps:find(absent,#{})"),
        Case(
            "from-list",
            "#{a:=last,b:=two}=maps:from_list([{a,first},{b,two},{a,last}]),#{1:=integer,1.0:=float}=maps:from_list([{1,integer},{1.0,float}])"
        ),
        Case("from-keys", "#{1:=value,1.0:=value}=maps:from_keys([1,1.0,1],value),#{}=maps:from_keys([],unused)"),
        Case("is-key", "true=maps:is_key(1,#{1=>integer}),false=maps:is_key(1.0,#{1=>integer})"),
        Case(
            "keys-values",
            "[1,2]=lists:sort(fun(A,B)->A=<B end,maps:keys(#{1=>a,2=>b})),[a,b]=lists:sort(fun(A,B)->A=<B end,maps:values(#{1=>a,2=>b}))"
        ),
        Case("merge", "#{a:=right,b:=left,c:=third}=maps:merge(#{a=>left,b=>left},#{a=>right,c=>third})"),
        Case("new", "#{}=maps:new(),0=maps:size(maps:new())"),
        Case("put", "M=#{1=>old},N=maps:put(1.0,new,M),2=maps:size(N),old=maps:get(1,M),new=maps:get(1.0,N)"),
        Case("remove", "#{1.0:=keep}=maps:remove(1,#{1=>remove,1.0=>keep}),#{}=maps:remove(absent,#{})"),
        Case("take", "{value,#{b:=other}}=maps:take(a,#{a=>value,b=>other}),error=maps:take(absent,#{})"),
        Case("to-list", "[{1,a},{2,b}]=lists:sort(fun(A,B)->A=<B end,maps:to_list(#{1=>a,2=>b}))"),
        Case(
            "update",
            "#{a:=new}=maps:update(a,new,#{a=>old}),try maps:update(a,new,#{}) of _->unexpected catch error:{badkey,a}->ok end"
        ),
        Case(
            "update-with-three",
            "#{a:=3}=maps:update_with(a,fun(V)->V+1 end,#{a=>2}),try maps:update_with(a,fun(V)->V end,#{}) of _->unexpected catch error:{badkey,a}->ok end"
        ),
        Case(
            "update-with-four",
            "#{a:=3}=maps:update_with(a,fun(V)->V+1 end,initial,#{a=>2}),#{a:=initial}=maps:update_with(a,fun(_)->throw(called) end,initial,#{})"
        ),
        Case("with", "#{1:=a}=maps:with([1,missing,1],#{1=>a,1.0=>b}),#{}=maps:with([],#{a=>b})"),
        Case("without", "#{1.0:=b}=maps:without([1,missing,1],#{1=>a,1.0=>b})"),
        Case("filter", "#{2:=20}=maps:filter(fun(K,V)->K=:=2 andalso V=:=20 end,#{1=>10,2=>20})"),
        Case("filtermap", "#{1:=10,3:=31}=maps:filtermap(fun(1,_)->true;(2,_)->false;(3,V)->{true,V+1} end,#{1=>10,2=>20,3=>30})"),
        Case("fold", "33=maps:fold(fun(K,V,A)->K+V+A end,0,#{1=>10,2=>20}),seed=maps:fold(fun(_,_,_)->unexpected end,seed,#{})"),
        Case("foreach", "ok=maps:foreach(fun(K,V)->put(K,V),ignored end,#{1=>10,2=>20}),10=get(1),20=get(2)"),
        Case("map", "#{1:=11,2:=22}=maps:map(fun(K,V)->K+V end,#{1=>10,2=>20})"),
        Case("ordered-fold", "[{2,20},{1,10}]=maps:fold(fun(K,V,A)->[{K,V}|A] end,[],maps:iterator(#{2=>20,1=>10},ordered))"),
        Case(
            "reversed-map-callback",
            "put(order,[]),#{1:=10,2:=20}=maps:map(fun(K,V)->put(order,[K|get(order)]),V end,maps:iterator(#{1=>10,2=>20},reversed)),[1,2]=get(order)"
        ),
        Case("iterator-filter", "#{2:=20}=maps:filter(fun(K,_)->K>1 end,maps:iterator(#{1=>10,2=>20},ordered))"),
        Case("iterator-filtermap", "#{1:=11}=maps:filtermap(fun(_,V)->{true,V+1} end,maps:iterator(#{1=>10},ordered))"),
        Case(
            "iterator-foreach",
            "put(order,[]),ok=maps:foreach(fun(K,_)->put(order,[K|get(order)]) end,maps:iterator(#{1=>10,2=>20},ordered)),[2,1]=get(order)"
        ),
        Case(
            "none",
            "#{}=maps:map(fun(_,V)->V end,none),#{}=maps:filter(fun(_,_)->true end,none),seed=maps:fold(fun(_,_,_)->unexpected end,seed,none)"
        ),
        Case(
            "bad-callback",
            "try maps:map(wrong,wrong) of _->unexpected catch error:badarg->ok end,try maps:filter(fun(_)->true end,#{}) of _->unexpected catch error:badarg->ok end"
        ),
        Case("bad-source", "try maps:map(fun(_,V)->V end,wrong) of _->unexpected catch error:{badmap,wrong}->ok end"),
        Case(
            "bad-continuation-order",
            "put(called,false),try maps:fold(fun(_,_,A)->put(called,true),A end,seed,{key,value,wrong}) of _->unexpected catch error:{badmap,{key,value,wrong}}->ok end,true=get(called)"
        ),
        Case(
            "callback-error-propagates",
            "try maps:map(fun(_,_)->error(badarg) end,#{a=>b}) of _->unexpected catch error:badarg->ok end"
        ),
        Case(
            "bad-filter-result",
            "try maps:filter(fun(_,_)->wrong end,#{a=>b}) of _->unexpected catch error:{case_clause,wrong}->ok end,try maps:filtermap(fun(_,_)->{false,new} end,#{a=>b}) of _->unexpected catch error:{case_clause,{false,new}}->ok end"
        ),
        Case(
            "bad-from-list",
            "try maps:from_list([{a,b}|wrong]) of _->unexpected catch error:badarg->ok end,try maps:from_list([{a}]) of _->unexpected catch error:badarg->ok end"
        ),
        Case(
            "bad-map-precedence",
            "try maps:update_with(a,wrong,wrong) of _->unexpected catch error:{badmap,wrong}->ok end,try maps:merge(wrong,other) of _->unexpected catch error:{badmap,wrong}->ok end"
        ),
        Case(
            "bad-update-callback",
            "try maps:update_with(a,wrong,#{}) of _->unexpected catch error:badarg->ok end,try maps:update_with(a,fun(_,_) -> wrong end,initial,#{}) of _->unexpected catch error:badarg->ok end"
        ),
        Case(
            "bad-select-shape",
            "try maps:with(wrong,#{}) of _->unexpected catch error:badarg->ok end,try maps:without(wrong,wrong) of _->unexpected catch error:{badmap,wrong}->ok end"
        ),
        Case("with-improper-tail", "try maps:with([a|wrong],#{a=>b}) of _->unexpected catch error:function_clause->ok end"),
        Case("without-improper-tail", "try maps:without([a|wrong],#{a=>b}) of _->unexpected catch error:function_clause->ok end"),
        Case(
            "duplicate-iterator-keys",
            "put(counter,0),#{a:=2}=maps:map(fun(_,_)->V=get(counter)+1,put(counter,V),V end,[ [a,a] | #{a=>old} ])"
        ),
        Case(
            "valid-iterator",
            "true=maps:is_iterator_valid(none),true=maps:is_iterator_valid(maps:iterator(#{a=>b})),false=maps:is_iterator_valid(wrong),false=maps:is_iterator_valid({a,b,wrong})"
        )
    ];
}
