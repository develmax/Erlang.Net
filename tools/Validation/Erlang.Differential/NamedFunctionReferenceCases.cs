namespace Erlang.Differential;

public static class NamedFunctionReferenceCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } =
    [
        Create("auto-invoke", "F=fun is_atom/1,true=F(value),false=F(42)", ""),
        Create(
            "auto-metadata",
            "F=fun is_atom/1,{type,external}=erlang:fun_info(F,type),{module,erlang}=erlang:fun_info(F,module),{name,is_atom}=erlang:fun_info(F,name),{arity,1}=erlang:fun_info(F,arity),{env,[]}=erlang:fun_info(F,env)",
            ""
        ),
        Create("auto-identity", "F=fun is_atom/1,G=erlang:make_fun(erlang,is_atom,1),true=(F=:=G)", ""),
        Create("private", "F=fun private/1,42=F(41)", "private(X)->X+1."),
        Create(
            "local-metadata",
            "F=fun private/1,{type,local}=erlang:fun_info(F,type),{module,named_reference_contract}=erlang:fun_info(F,module),{name,private}=erlang:fun_info(F,name),{arity,1}=erlang:fun_info(F,arity),{env,[]}=erlang:fun_info(F,env)",
            "private(X)->X+1."
        ),
        Create("repeat", "F=fun private/1,G=fun private/1,true=(F=:=G)", "private(X)->X+1."),
        Create("forward", "F=fun answer/0,42=F()", "answer()->42."),
        Create("recursive", "F=fun count/1,3=F(3)", "count(0)->0;count(N)->F=fun count/1,1+F(N-1)."),
        Create("nested", "Make=fun()->fun private/1 end,F=Make(),42=F(41)", "private(X)->X+1."),
        Create("etf", "F=fun private/1,G=binary_to_term(term_to_binary(F)),true=(F=:=G),42=G(41)", "private(X)->X+1."),
        Create("badarity", "F=fun private/1,ok=try F() of _->unexpected catch error:{badarity,{F,[]}}->ok end", "private(X)->X+1."),
        Create(
            "local-bif-precedence",
            "F=fun is_atom/1,local=F(value),{type,local}=erlang:fun_info(F,type),{name,is_atom}=erlang:fun_info(F,name)",
            "is_atom(_)->local."
        )
    ];

    public static IReadOnlyList<CompiledDiagnosticCase> Diagnostics { get; } =
    [
        Reject("loaded-not-auto-imported", "loaded", 0),
        Reject("unknown", "qzx_missing", 0),
        Reject("wrong-arity", "is_atom", 2)
    ];

    private static CompiledModuleCase Create(string name, string body, string definitions) => new(
        "compiler-named-reference-" + name,
        "-module(named_reference_contract). -export([run/0]). run()->" + body + ",done. " + definitions,
        Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
    );

    private static CompiledDiagnosticCase Reject(string name, string function, int arity) => new(
        "named-reference-" + name,
        "-module(named_reference_rejection). -export([run/0]). run()->fun " + function + "/" + arity.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".",
        function,
        arity.ToString(System.Globalization.CultureInfo.InvariantCulture),
        DiagnosticCaseKind.UndefinedFunction
    );
}
