namespace Erlang.Compiler;

internal static class NifDeclarationSyntax
{
    internal static IReadOnlyList<(string Name, int Arity)> Read(Term declaration, int offset)
    {
        var result = new List<(string Name, int Arity)>();
        Term tail = declaration;
        while (tail is Cons item)
        {
            if (item.Head is not TupleTerm { Items.Count: 2 } pair
                || pair.Items[0] is not Atom name
                || pair.Items[1] is not Integer arity
                || arity.Value < int.MinValue || arity.Value > int.MaxValue)
                throw new CompileException(CompilerDiagnosticCodes.FunctionDefinition, NifDeclarationDiagnostics.InvalidEntry, offset);
            result.Add((name.Name, (int)arity.Value));
            tail = item.Tail;
        }
        if (tail is not Nil)
            throw new CompileException(CompilerDiagnosticCodes.Syntax, NifDeclarationDiagnostics.InvalidList, offset);

        return result;
    }

    internal static void Validate(ModuleDefinition module)
    {
        if (module.DeclaredNifs is not { } declarations)
            return;
        var defined = module.DeclaredFunctions.Select(function => (function.Name, function.Arity)).ToHashSet();
        foreach (var declaration in declarations)
        {
            if (!defined.Contains(declaration))
                throw new CompileException(
                    CompilerDiagnosticCodes.FunctionDefinition,
                    NifDeclarationDiagnostics.Undefined(declaration.Name, declaration.Arity),
                    0
                );
        }
    }
}
