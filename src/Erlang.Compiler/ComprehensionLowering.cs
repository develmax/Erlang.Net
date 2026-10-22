namespace Erlang.Compiler;

internal static class ComprehensionLowering
{
    internal static bool EliminatesHelper(ComprehensionQualifier qualifier)
        => qualifier is ComprehensionQualifier.Generator
        {
            Strict: false,
            Source: Expr.List { Items.Count: 1, Tail: null }
        };

    internal static int CapturedArity(Expr.ListComprehension expression, int qualifierIndex)
    {
        var definitions = ComprehensionGenerator.Flatten([expression.Qualifiers[qualifierIndex]]).ToArray();
        var body = new Expr.ListComprehension(expression.Items, expression.Qualifiers.Skip(qualifierIndex + 1).ToArray());
        var function = new Expr.Fun([new Clause(definitions.Select(value => value.Pattern).ToArray(), null, body)]);
        var names = new HashSet<string>(StringComparer.Ordinal);
        Collect(body);
        var bindings = names.ToDictionary(value => value, _ => (Term)Nil.Value, StringComparer.Ordinal);
        int captures = ClosureCaptures.Capture(function, bindings).Count;
        int pendingTails = expression.Qualifiers.Take(qualifierIndex).Count(value =>
            value is not ComprehensionQualifier.Filter && !EliminatesHelper(value));

        return captures + pendingTails;

        void Collect(Expr item)
        {
            if (item is Expr.Variable variable)
            {
                names.Add(variable.Name);
            }
            foreach (var child in LocalFunctionEntries.Children(item))
            {
                Collect(child);
            }
        }
    }
}
