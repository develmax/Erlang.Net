namespace Erlang.Compiler;

internal static class DynamicApplySyntax
{
    internal static RuntimeTailCall? ResolveTailTarget(
        Expr.Call call,
        string? resolvedModule,
        IReadOnlyList<Term> arguments
    )
    {
        if (resolvedModule != ErlangModuleNames.Module
            || call.Function != ErlangBasicBifNames.Apply
            || arguments.Count != ErlangApplyLayout.ModuleApplyArity
            || arguments[ErlangApplyLayout.ModuleArgument] is not Atom module
            || arguments[ErlangApplyLayout.FunctionArgument] is not Atom function)
            return null;

        var values = new List<Term>();
        Term remaining = arguments[ErlangApplyLayout.ArgumentList];
        while (remaining is Cons cell)
        {
            if (values.Count == ErlangApplyLimits.MaximumArgumentCount)
                return null;
            values.Add(cell.Head);
            remaining = cell.Tail;
        }
        if (remaining is not Nil)
            return null;
        if (module.Name == ErlangModuleNames.Module
            && (function.Name == ErlangModuleNames.Error && values.Count is 1 or 2 or 3
                || function.Name is ErlangModuleNames.Exit or ErlangModuleNames.Throw && values.Count == 1))
            return null;

        return new RuntimeTailCall(module.Name, function.Name, values.ToArray());
    }
}
