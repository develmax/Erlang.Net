namespace Erlang.Compiler;

internal static class EunitAutoExport
{
    internal const string Test = "test";

    internal static bool IsTransform(Term option) =>
        option is TupleTerm { Items: [Atom { Name: "parse_transform" }, Atom { Name: "eunit_autoexport" }] };

    internal static bool IsExported(FunctionDefinition function) =>
        function.Arity == 0 && (function.Name.EndsWith("_test", StringComparison.Ordinal)
            || function.Name.EndsWith("_test_", StringComparison.Ordinal)
            || function.Name.EndsWith("_exported_", StringComparison.Ordinal))
        || function is { Name: "eunit_wrapper_", Arity: 1 };

    internal static FunctionDefinition Wrapper(string module) =>
        new(Test, 0, [new([], null, new Expr.Call("eunit", Test, [new Expr.Literal(Term.A(module))]))]);

    internal static Term AbstractWrapper(string module)
    {
        Term zero = Term.I(0);
        Term test = Term.A(Test);
        Term remote = Term.Tuple(Term.A("remote"), zero,
            Term.Tuple(Term.A("atom"), zero, Term.A("eunit")),
            Term.Tuple(Term.A("atom"), zero, test));
        Term call = Term.Tuple(Term.A("call"), zero, remote,
            Term.List(Term.Tuple(Term.A("atom"), zero, Term.A(module))));
        Term clause = Term.Tuple(Term.A("clause"), zero, Nil.Value, Nil.Value, Term.List(call));

        return Term.Tuple(Term.A("function"), zero, test, zero, Term.List(clause));
    }
}
