namespace Erlang.Differential;

public static class ErlangBasicCases
{
    private static readonly IReadOnlyList<(string Function, int Arity, string Arguments, string Expected)> exports =
    [
        ("+",1,"[-3]","-3"),("-",1,"[-3]","3"),("not",1,"[false]","true"),("bnot",1,"[7]","-8"),
        ("+",2,"[2,3]","5"),("-",2,"[2,3]","-1"),("*",2,"[2,3]","6"),("/",2,"[5,2]","2.5"),
        ("div",2,"[-5,2]","-2"),("rem",2,"[-5,2]","-1"),("band",2,"[3,6]","2"),("bor",2,"[3,6]","7"),
        ("bxor",2,"[3,6]","5"),("bsl",2,"[3,4]","48"),("bsr",2,"[-9,2]","-3"),
        ("and",2,"[true,false]","false"),("or",2,"[true,false]","true"),("xor",2,"[true,true]","false"),
        ("==",2,"[1,1.0]","true"),("/=",2,"[1,1.0]","false"),("=:=",2,"[1,1.0]","false"),("=/=",2,"[1,1.0]","true"),
        ("<",2,"[1,a]","true"),(">",2,"[a,1]","true"),("=<",2,"[1,1.0]","true"),(">=",2,"[1.0,1]","true"),
        ("++",2,"[[a,b],tail]","[a,b|tail]"),("--",2,"[[1,1.0,1],[1,1]]","[1.0]"),
        ("abs",1,"[-12345678901234567890]","12345678901234567890"),("float",1,"[3]","3.0"),
        ("round",1,"[-2.5]","-3"),("trunc",1,"[-2.9]","-2"),("floor",1,"[-2.1]","-3"),("ceil",1,"[-2.9]","-2"),
        ("min",2,"[1.0,1]","1.0"),("max",2,"[1.0,1]","1.0"),
        ("is_boolean",1,"[false]","true"),("is_function",1,"[fun()->ok end]","true"),
        ("is_function",2,"[fun(X)->X end,1]","true"),("is_port",1,"[self()]","false"),
        ("is_reference",1,"[make_ref()]","true"),("is_integer",3,"[5,5,9]","true"),
        ("size",1,"[<<7:3>>]","0"),("setelement",3,"[2,{a,b},c]","{a,c}"),
        ("append_element",2,"[{a},b]","{a,b}"),("insert_element",3,"[2,{a,b},c]","{a,c,b}"),
        ("delete_element",2,"[2,{a,b,c}]","{a,c}"),("make_tuple",2,"[2,empty]","{empty,empty}"),
        ("make_tuple",3,"[3,empty,[{2,a},{2,b},{3,c}]]","{empty,b,c}"),
        ("tuple_to_list",1,"[{a,b}]","[a,b]"),("list_to_tuple",1,"[[a,b]]","{a,b}"),
        ("append",2,"[[a],tail]","[a|tail]"),("subtract",2,"[[a,b,a],[a]]","[b,a]")
    ];

    public static string? FixtureName(string function, int arity) => exports.Any(export => export.Function == function && export.Arity == arity) || specialExports.Contains((function, arity)) ? Name(function, arity) : null;

    private static readonly IReadOnlyList<(string Function, int Arity)> specialExports =
    [
        ("!",2),("send",2),("get",0),("erase",0),("get_keys",0),("get_keys",1),("apply",2),("apply",3)
    ];

    private static string Name(string function, int arity) => "erlang-basic-export-" + function + "-" + arity;

    private static CompiledModuleCase Case(string name, string body) => new(
        name,
        "-module(oracle_erlang_basic). -export([run/0]). run()->" + body + ",done.",
        Term.Tuple(Term.A("ok"), Term.A("done"))
    );

    private static string Error(string call, string reason) => "try " + call + " of _->unexpected catch error:" + reason + "->ok end";

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = exports.Select(export => Case(Name(export.Function, export.Arity), export.Expected + "=apply(erlang,'" + export.Function + "'," + export.Arguments + ")")).ToList();
        foreach (string send in new[] { "!", "send" })
            cases.Add(Case(Name(send, 2), "sent=apply(erlang,'" + send + "',[self(),sent]),receive sent->ok after 1000->missing end"));
        cases.Add(Case(Name("get", 0), "erase(),put(a,1),put(b,2),[{a,1},{b,2}]=lists:sort(erlang:get())"));
        cases.Add(Case(Name("erase", 0), "erase(),put(a,1),[{a,1}]=erlang:erase(),[]=get()"));
        cases.Add(Case(Name("get_keys", 0), "erase(),put(b,1),put(a,1),[a,b]=lists:sort(get_keys())"));
        cases.Add(Case(Name("get_keys", 1), "erase(),put(a,1),put(b,1.0),[a]=get_keys(1),[b]=get_keys(1.0)"));
        cases.Add(Case(Name("apply", 2), "3=apply(fun(A,B)->A+B end,[1,2])," + Error("apply({lists,reverse},[[a,b]])", "{badfun,{lists,reverse}}")));
        cases.Add(Case(Name("apply", 3), "[c,b,a]=apply(lists,reverse,[[a,b,c]])"));
        cases.AddRange(new[]
        {
            Case("erlang-basic-round-ties", "[-3,-2,-1,1,2,3]=[round(X)||X<-[-2.5,-1.5,-0.5,0.5,1.5,2.5]]"),
            Case(
                "erlang-basic-round-large",
                "9007199254740992=round(9007199254740992.0),100000000000000000000=trunc(1.0e20),-100000000000000000000=ceil(-1.0e20)"
            ),
            Case(
                "erlang-basic-numeric-integer-identity",
                "N=123456789012345678901234567890,N=round(N),N=trunc(N),N=floor(N),N=ceil(N),N=abs(-N)"
            ),
            Case("erlang-basic-float-zero", "<<0:64>> = <<(abs(-0.0)):64/float>>,<<1:1,0:63>> = <<(float(-0.0)):64/float>>"),
            Case(
                "erlang-basic-min-max-ties",
                "1=min(1,1.0),1.0=min(1.0,1),1=max(1,1.0),1.0=max(1.0,1),{1}=min({1},{1.0}),[1.0]=max([1.0],[1])"
            ),
            Case(
                "erlang-basic-integer-range",
                "false=is_integer(5.0,0,10),false=is_integer(5,10,0),true=is_integer(100000000000000000000,0,100000000000000000000),"+Error("is_integer(wrong,a,b)","badarg")
            ),
            Case(
                "erlang-basic-function-arity",
                "false=is_function(fun()->ok end,1),false=is_function(fun()->ok end,100000000000000000000),"+Error("is_function(wrong,-1)","badarg")+","+Error("is_function(wrong,0.0)","badarg")
            ),
            Case("erlang-basic-size-bits", "0=size(<<7:3>>),1=size(<<7:9>>),2=byte_size(<<7:9>>),2=size({a,b})"),
            Case(
                "erlang-basic-tuple-immutable",
                "T={a,b},U=setelement(2,T,c),{a,b}=T,{a,c}=U,{x}=erlang:insert_element(1,{},x),{}=erlang:delete_element(1,{x}),{}=erlang:make_tuple(0,default,[])"
            ),
            Case(
                "erlang-basic-tuple-initialization",
                "{x,b,c}=erlang:make_tuple(3,x,[{3,a},{2,b},{3,c}]),"+Error("erlang:make_tuple(0,x,[{1,y}])","badarg")+","+Error("erlang:make_tuple(2,x,[{1,a}|wrong])","badarg")
            ),
            Case("erlang-basic-guards", "yes=guard(-2.5,fun(X)->X end),no=guard(wrong,wrong)"),
            Case("erlang-basic-apply-badfun", Error("apply(wrong,[])","{badfun,wrong}")),
            Case("erlang-basic-apply-badarity", "try apply(fun(_)->ok end,[]) of _->unexpected catch error:{badarity,_}->ok end"),
            Case("erlang-basic-apply-list-precedence", Error("apply(wrong,[a|tail])","badarg")),
            Case(
                "erlang-basic-apply-mfa-errors",
                Error("apply(lists,missing,[])","undef")+","+Error("apply(wrong,missing,wrong)","badarg")+","+Error("apply(1,missing,[])","badarg")
            ),
            Case("erlang-basic-apply-throw", "try apply(fun()->throw(kept) end,[]) of _->unexpected catch throw:kept->ok end"),
            Case(
                "erlang-basic-dictionary-exact",
                "erase(),put(1,value),put(1.0,value),Keys=get_keys(value),2=length(Keys),true=lists:member(1,Keys),true=lists:member(1.0,Keys),2=length(erase()),[]=get_keys()"
            ),
            Case(
                "erlang-basic-registered-send",
                "true=register(erlang_basic_send,self()),sent=erlang:send(erlang_basic_send,sent),receive sent->ok after 1000->missing end,true=unregister(erlang_basic_send)"
            ),
            Case("erlang-basic-operator-dynamic", "Op=operator(),true=apply(erlang,Op,[1,1.0])")
        });
        foreach (string function in new[] { "abs", "float", "round", "trunc", "ceil", "floor", "size", "list_to_tuple", "tuple_to_list" })
            cases.Add(Case("erlang-basic-badarg-" + function, Error("apply(erlang,'" + function + "',[wrong])", "badarg")));
        foreach (string call in new[] { "setelement(0,{},x)", "erlang:insert_element(2,{},x)", "erlang:delete_element(1,{})", "erlang:append_element(wrong,x)", "erlang:make_tuple(-1,x)", "erlang:make_tuple(16777216,x)", "erlang:make_tuple(2,x,[wrong])", "erlang:make_tuple(2,x,[{0,y}])", "list_to_tuple([a|tail])" })
            cases.Add(Case("erlang-basic-tuple-error-" + cases.Count, Error(call, "badarg")));
        foreach (string operation in new[] { "+", "-", "*", "/", "div", "rem", "band", "bor", "bxor", "bsl", "bsr" })
            cases.Add(Case("erlang-basic-badarith-" + operation, Error("apply(erlang,'" + operation + "',[wrong,1])", "badarith")));
        foreach (string operation in new[] { "and", "or", "xor" })
            cases.Add(Case("erlang-basic-boolean-error-" + operation, Error("apply(erlang,'" + operation + "',[true,wrong])", "badarg")));
        cases.Add(Case(
            "erlang-basic-integer-shifts",
            "-1=apply(erlang,'bsr',[-2,100000000000000000000]),8=apply(erlang,'bsl',[16,-1]),-3=apply(erlang,'bsr',[-9,2])"
        ));
        cases.Add(Case("erlang-basic-float-overflow", Error("float(1 bsl 2000)", "badarg")));
        cases.Add(Case(
            "erlang-basic-divide-zero",
            Error("apply(erlang,'/',[1,0])", "badarith") + "," + Error("apply(erlang,'div',[1,0])", "badarith")
        ));

        cases.Add(Case(
            "erlang-basic-apply-tuple-rejection",
            Error("apply({lists,reverse},[a|tail])", "badarg") + "," + Error("apply({1,2},[])", "{badfun,{1,2}}") + "," + Error("apply({lists,reverse,[]},[])", "{badfun,{lists,reverse,[]}}")
        ));

        return cases.Select(fixture => fixture.Name == "erlang-basic-guards" ? fixture with { Source = fixture.Source + " guard(X,F) when is_number(X),abs(X)=:=2.5,round(X)=:=-3,is_function(F,1),is_boolean(true),is_integer(5,0,9),size({a})=:=1,min(X,0)=:=X->yes;guard(_,_) ->no." } : fixture.Name == "erlang-basic-operator-dynamic" ? fixture with { Source = fixture.Source + " operator()->'=='." } : fixture).ToArray();
    }
}
