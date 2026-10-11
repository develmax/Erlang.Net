namespace Erlang.Differential;

public static class ExternalFunctionCases
{
    private static CompiledModuleCase Case(string name, string body) => new(
        "erlang-function-" + name,
        "-module(oracle_function_bifs). -export([run/0,id/1]). run()->" + body + ",done. id(X)->X. hidden()->ok.",
        Term.Tuple(Term.A("ok"), Term.A("done"))
    );

    private static string Error(string call) => "try erlang:" + call + " of _->unexpected catch error:badarg->ok end";

    public static string? FixtureName(string function, int arity)
    {
        string name = "erlang-function-export-" + function + "-" + arity;

        return Modules.Any(fixture => fixture.Name == name) ? name : null;
    }

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();
        cases.Add(Case(
            "export-function_exported-3",
            "true=erlang:function_exported(erlang,length,1),true=erlang:function_exported(oracle_function_bifs,id,1),false=erlang:function_exported(oracle_function_bifs,hidden,0)"
        ));
        cases.Add(Case("export-is_builtin-3", "true=erlang:is_builtin(erlang,length,1),false=erlang:is_builtin(oracle_function_bifs,id,1)"));
        cases.Add(Case(
            "export-make_fun-3",
            "F=erlang:make_fun(erlang,length,1),3=F([a,b,c]),3=erlang:apply(F,[[a,b,c]]),true=is_function(F,1),false=is_function(F,0)"
        ));
        cases.Add(Case(
            "external-equality",
            "A=erlang:make_fun(erlang,length,1),B=erlang:make_fun(erlang,length,1),true=(A=:=B),true=(A==B),false=(A<B),false=(A>B)"
        ));
        cases.Add(Case(
            "external-map-key",
            "A=erlang:make_fun(erlang,length,1),B=erlang:make_fun(erlang,length,1),M=#{A=>first,B=>second},1=map_size(M),second=maps:get(A,M),second=maps:get(B,M)"
        ));
        cases.Add(Case(
            "external-unloaded-equality",
            "A=erlang:make_fun(oracle_missing_function,target,2),B=erlang:make_fun(oracle_missing_function,target,2),true=(A=:=B),false=erlang:function_exported(oracle_missing_function,target,2)"
        ));
        cases.Add(Case(
            "external-undefined-call",
            "F=erlang:make_fun(oracle_missing_function,target,0),try F() of _->unexpected catch error:undef->ok end"
        ));
        cases.Add(Case(
            "external-hidden-call",
            "F=erlang:make_fun(oracle_function_bifs,hidden,0),try F() of _->unexpected catch error:undef->ok end"
        ));
        cases.Add(Case("external-registered-call", "F=erlang:make_fun(oracle_function_bifs,id,1),marker=F(marker)"));
        cases.Add(Case(
            "external-badarity",
            "F=erlang:make_fun(erlang,length,1),try F() of _->unexpected catch error:{badarity,{G,[]}}->true=(F=:=G) end"
        ));
        cases.Add(Case("external-arity-zero", "F=erlang:make_fun(erlang,self,0),P=self(),P=F()"));
        cases.Add(Case("external-arity-255", "F=erlang:make_fun(oracle_missing_function,target,255),true=is_function(F,255)"));
        cases.Add(Case(
            "external-operator",
            "F=erlang:make_fun(erlang,'+',2),3=F(1,2),true=erlang:is_builtin(erlang,'+',2),true=erlang:is_builtin(erlang,'/',2),true=erlang:is_builtin(erlang,'=/=',2)"
        ));
        cases.Add(Case("external-kind-order", "A=fun()->ok end,B=erlang:make_fun(erlang,self,0),true=(A<B),true=(B>A),false=(A=:=B)"));
        cases.Add(Case("external-module-order", "A=erlang:make_fun(a,z,255),B=erlang:make_fun(b,a,0),true=(A<B)"));
        cases.Add(Case("external-name-order", "A=erlang:make_fun(a,a,255),B=erlang:make_fun(a,b,0),true=(A<B)"));
        cases.Add(Case("external-arity-order", "A=erlang:make_fun(a,a,1),B=erlang:make_fun(a,a,2),true=(A<B),false=(A=:=B)"));
        cases.Add(Case("external-atom-case-order", "A=erlang:make_fun('A',a,0),B=erlang:make_fun(a,a,0),true=(A<B)"));
        cases.Add(Case(
            "external-cross-process",
            "Parent=self(),F=erlang:make_fun(erlang,self,0),{Child,Ref}=spawn_monitor(fun()->receive {call,G}->Parent!{reply,self(),G()} end end),Child!{call,F},receive {reply,Child,Child}->ok end,receive {'DOWN',Ref,process,Child,normal}->ok end"
        ));
        cases.Add(Case(
            "builtin-special-apply",
            "true=erlang:is_builtin(erlang,apply,2),true=erlang:is_builtin(erlang,apply,3),false=erlang:is_builtin(erlang,apply,1)"
        ));
        cases.Add(Case("builtin-special-yield", "true=erlang:is_builtin(erlang,yield,0),false=erlang:is_builtin(erlang,yield,1)"));
        cases.Add(Case(
            "builtin-native-list",
            "true=erlang:is_builtin(lists,member,2),true=erlang:is_builtin(lists,keyfind,3),true=erlang:is_builtin(lists,keymember,3),true=erlang:is_builtin(lists,keysearch,3),true=erlang:is_builtin(lists,reverse,2),false=erlang:is_builtin(lists,append,2)"
        ));
        cases.Add(Case(
            "builtin-native-and-wrapper",
            "true=erlang:is_builtin(erlang,integer_to_binary,1),true=erlang:is_builtin(erlang,universaltime_to_posixtime,1),true=erlang:is_builtin(erlang,integer_to_binary,2),false=erlang:is_builtin(erlang,fun_info,1)"
        ));
        cases.Add(Case(
            "builtin-source-export",
            "false=erlang:is_builtin(oracle_function_bifs,id,1),false=erlang:is_builtin(oracle_missing_function,target,0)"
        ));
        foreach (string function in new[] { "function_exported", "is_builtin" })
        {
            cases.Add(Case(function + "-negative", "false=erlang:" + function + "(erlang,length,-1)"));
            cases.Add(Case(
                function + "-large-valid-small",
                "false=erlang:" + function + "(erlang,length,256),false=erlang:" + function + "(erlang,length,(1 bsl 59)-1)"
            ));
            cases.Add(Case(
                function + "-int32-conversion",
                "true=erlang:" + function + "(erlang,length,(1 bsl 32)+1),true=erlang:" + function + "(erlang,length,1-(1 bsl 32))"
            ));
            foreach (var (name, arguments) in new[]
            {
                ("module", "1,length,1"),
                ("function", "erlang,1,1"),
                ("float-arity", "erlang,length,1.0"),
                ("atom-arity", "erlang,length,bad"),
                ("big-positive", "erlang,length,1 bsl 59"),
                ("big-negative", "erlang,length,-(1 bsl 59)-1")
            })
                cases.Add(Case(function + "-bad-" + name, Error(function + "(" + arguments + ")")));
        }
        foreach (var (name, arguments) in new[]
        {
            ("module", "1,length,1"),
            ("function", "erlang,1,1"),
            ("float-arity", "erlang,length,1.0"),
            ("atom-arity", "erlang,length,bad"),
            ("negative", "erlang,length,-1"),
            ("256", "erlang,length,256"),
            ("wrapped-arity", "erlang,length,(1 bsl 32)+1"),
            ("big", "erlang,length,1 bsl 100")
        })
            cases.Add(Case("make_fun-bad-" + name, Error("make_fun(" + arguments + ")")));

        return cases;
    }
}
