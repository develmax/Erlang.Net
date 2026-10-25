namespace Erlang.Differential;

public static class ListFunctionsCases
{
    private static CompiledModuleCase Case(string name, string body) => new(
        "lists-functional-" + name,
        "-module(oracle_list_functions). -export([run/0]). run()->" + body + ",done.",
        Term.Tuple(Term.A("ok"), Term.A("done"))
    );

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } =
    [
        Case("foldl", "[3,2,1]=lists:foldl(fun(X,A)->[X|A] end,[],[1,2,3]),seed=lists:foldl(fun(_,_) -> unused end,seed,[])"),
        Case(
            "foldr",
            "[1,2,3]=lists:foldr(fun(X,A)->[X|A] end,[],[1,2,3]),put(order,[]),6=lists:foldr(fun(X,A)->put(order,[X|get(order)]),X+A end,0,[1,2,3]),[1,2,3]=get(order)"
        ),
        Case("foreach", "put(order,[]),ok=lists:foreach(fun(X)->put(order,[X|get(order)]),ignored end,[1,2,3]),[3,2,1]=get(order)"),
        Case("partition", "{[1,3],[2,4]}=lists:partition(fun(X)->X rem 2=:=1 end,[1,2,3,4])"),
        Case(
            "all",
            "true=lists:all(fun(X)->X>0 end,[1,2,3]),false=lists:all(fun(X)->X>1 end,[1,2]),true=lists:all(fun(_)->unexpected end,[])"
        ),
        Case(
            "any",
            "true=lists:any(fun(X)->X=:=2 end,[1,2]),false=lists:any(fun(_)->false end,[1,2]),false=lists:any(fun(_)->unexpected end,[])"
        ),
        Case("takewhile", "[1,2]=lists:takewhile(fun(X)->X<3 end,[1,2,3,1]),[]=lists:takewhile(fun(_)->false end,[1|wrong])"),
        Case("dropwhile", "[3,1]=lists:dropwhile(fun(X)->X<3 end,[1,2,3,1]),[1|wrong]=lists:dropwhile(fun(_)->false end,[1|wrong])"),
        Case("filtermap", "[1,30]=lists:filtermap(fun(1)->true;(2)->false;(3)->{true,30} end,[1,2,3])"),
        Case("mapfoldl", "{[1,3,6],6}=lists:mapfoldl(fun(X,A)->{X+A,X+A} end,0,[1,2,3])"),
        Case("mapfoldr", "{[6,5,3],6}=lists:mapfoldr(fun(X,A)->{X+A,X+A} end,0,[1,2,3])"),
        Case("short-circuit-improper", "false=lists:all(fun(_)->false end,[1|wrong]),true=lists:any(fun(_)->true end,[1|wrong])"),
        Case(
            "callback-arity-empty",
            "try lists:foldl(wrong,seed,[]) of _->unexpected catch error:function_clause->ok end,try lists:any(fun(_,_) -> true end,[]) of _->unexpected catch error:function_clause->ok end"
        ),
        Case(
            "foldl-initial-invalid",
            "try lists:foldl(fun(_,A)->A end,seed,wrong) of _->unexpected catch error:{case_clause,wrong}->ok end"
        ),
        Case("all-initial-invalid", "try lists:all(fun(_)->true end,wrong) of _->unexpected catch error:{case_clause,wrong}->ok end"),
        Case("any-initial-invalid", "try lists:any(fun(_)->true end,wrong) of _->unexpected catch error:{case_clause,wrong}->ok end"),
        Case(
            "foldl-improper-effects",
            "put(called,false),try lists:foldl(fun(_,A)->put(called,true),A end,seed,[1|wrong]) of _->unexpected catch error:function_clause->ok end,true=get(called)"
        ),
        Case(
            "foldr-improper-no-effects",
            "put(called,false),try lists:foldr(fun(_,A)->put(called,true),A end,seed,[1|wrong]) of _->unexpected catch error:function_clause->ok end,false=get(called)"
        ),
        Case(
            "predicate-case-clause",
            "try lists:partition(fun(_)->wrong end,[1]) of _->unexpected catch error:{case_clause,wrong}->ok end,try lists:all(fun(_)->wrong end,[1]) of _->unexpected catch error:{case_clause,wrong}->ok end"
        ),
        Case(
            "filtermap-case-clause",
            "try lists:filtermap(fun(_)->{false,new} end,[1]) of _->unexpected catch error:{case_clause,{false,new}}->ok end"
        ),
        Case(
            "mapfold-badmatch",
            "try lists:mapfoldl(fun(_,_) -> wrong end,seed,[1]) of _->unexpected catch error:{badmatch,wrong}->ok end,try lists:mapfoldr(fun(_,_) -> wrong end,seed,[1]) of _->unexpected catch error:{badmatch,wrong}->ok end"
        ),
        Case(
            "foreach-error-propagates",
            "try lists:foreach(fun(_)->throw(callback) end,[1]) of _->unexpected catch throw:callback->ok end"
        ),
        Case(
            "dropwhile-tail-error",
            "try lists:dropwhile(fun(_)->true end,[1|wrong]) of _->unexpected catch error:function_clause->ok end"
        ),
        Case(
            "mapfold-right-malformed-no-effects",
            "put(called,false),try lists:mapfoldr(fun(X,A)->put(called,true),{X,A} end,seed,[1|wrong]) of _->unexpected catch error:function_clause->ok end,false=get(called)"
        )
    ];
}
