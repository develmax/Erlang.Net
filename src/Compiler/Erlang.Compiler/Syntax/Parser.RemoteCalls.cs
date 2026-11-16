namespace Erlang.Compiler;

public sealed partial class Parser
{
    private Expr ReadRemoteCall(Expr module)
    {
        Expr function;
        if (Current.Kind == LexerTokenKinds.Variable)
        {
            function = new Expr.Variable(tokens[position++].Text);
        }
        else if (Take(ErlangSyntaxTokens.OpenParenthesis))
        {
            function = Expression();
            Expect(ErlangSyntaxTokens.CloseParenthesis);
        }
        else
        {
            function = new Expr.Literal(Term.A(Name()));
        }

        Expect(ErlangSyntaxTokens.OpenParenthesis);
        var arguments = Arguments();
        if (module is Expr.Literal { Value: Atom moduleAtom } && function is Expr.Literal { Value: Atom functionAtom })
        {
            return RecordPredicateCall(moduleAtom.Name, functionAtom.Name, arguments);
        }

        return new Expr.Call(ErlangModuleNames.Module, AutoImportNames.Apply, [module, function, new Expr.List(arguments)])
        {
            DynamicRemote = true
        };
    }
}
