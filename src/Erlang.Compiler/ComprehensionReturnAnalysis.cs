namespace Erlang.Compiler;

internal static class ComprehensionReturnAnalysis
{
    internal static bool KnownImproperSource(ComprehensionQualifier qualifier)
        => qualifier is ComprehensionQualifier.Generator
        {
            Source: Expr.List { Tail: Expr.Literal { Value: not Nil and not Cons } }
        };

    internal static bool TerminalBody(Expr.ListComprehension expression, ModuleDefinition module, FunctionDefinition? owner = null)
    {
        if (expression.Items.Count != 1 || expression.Qualifiers.Count == 0 || expression.Qualifiers[0] is not ComprehensionQualifier.Generator generator)
        {
            return false;
        }
        Expr sourceExpression = generator.Source;
        if (sourceExpression is Expr.Variable variable && owner is not null)
        {
            if (SourceArgumentBindings.LiteralList(module, owner, variable.Name) is not { } known)
            {
                return false;
            }
            sourceExpression = known;
        }
        if (sourceExpression is not Expr.List { Tail: null } source || source.Items.Count > ComprehensionReturnParameters.MaximumLiteralItems)
        {
            return false;
        }
        bool admitted = false;
        foreach (var item in source.Items)
        {
            var scope = new Dictionary<string, Term>(StringComparer.Ordinal);
            if (!Value(item, scope, out var value) || Match(generator.Pattern, value, scope) is not { } matched)
            {
                return false;
            }
            if (!matched)
            {
                if (generator.Strict)
                {
                    return false;
                }
                continue;
            }
            bool accepted = true;
            foreach (var qualifier in expression.Qualifiers.Skip(1))
            {
                if (qualifier is not ComprehensionQualifier.Filter filter || !Value(filter.Expression, scope, out var condition)
                    || condition is not Atom { Name: ErlangBooleanAtoms.True or ErlangBooleanAtoms.False } boolean)
                {
                    return false;
                }
                if (boolean.Name == ErlangBooleanAtoms.False)
                {
                    accepted = false;
                    break;
                }
            }
            if (accepted)
            {
                admitted = true;
                if (!Terminal(expression.Items[0], scope, module))
                {
                    return false;
                }
            }
        }

        return admitted;
    }

    private static bool Terminal(Expr expression, Dictionary<string, Term> scope, ModuleDefinition module)
    {
        if (expression is not Expr.Call call)
        {
            return false;
        }
        if (call.Module is not null)
        {
            return false;
        }
        var function = module.Functions.FirstOrDefault(value => value.Name == call.Function && value.Arity == call.Arguments.Count);
        if (function is null || !SourceReturnSafety.Allows(module, function.Name, function.Arity))
        {
            return false;
        }
        var arguments = new List<Term>();
        foreach (var argument in call.Arguments)
        {
            if (!Value(argument, scope, out var value))
            {
                return false;
            }
            arguments.Add(value);
        }
        foreach (var clause in function.Clauses)
        {
            var local = new Dictionary<string, Term>(StringComparer.Ordinal);
            bool matched = true;
            for (int index = 0; index < arguments.Count; index++)
            {
                if (Match(clause.Patterns[index], arguments[index], local) is not { } accepted)
                {
                    return false;
                }
                if (!accepted)
                {
                    matched = false;
                    break;
                }
            }
            if (!matched)
            {
                continue;
            }
            if (clause.Guard is not null)
            {
                if (!Value(clause.Guard, local, out var guard) || guard is not Atom boolean)
                {
                    return false;
                }
                if (boolean.Name == ErlangBooleanAtoms.False)
                {
                    continue;
                }
                if (boolean.Name != ErlangBooleanAtoms.True)
                {
                    return false;
                }
            }

            return clause.Body is Expr.Call terminal && terminal.Module == ErlangModuleNames.Module
                && (terminal.Function == ErlangModuleNames.Error && terminal.Arguments.Count is >= 1 and <= 3
                    || terminal.Function is ErlangModuleNames.Throw or ErlangModuleNames.Exit && terminal.Arguments.Count == 1);
        }

        return false;
    }

    private static bool? Match(Pattern pattern, Term value, Dictionary<string, Term> scope)
    {
        if (pattern is Pattern.Any)
        {
            return true;
        }
        if (pattern is Pattern.Literal literal)
        {
            return literal.Value.Equals(value);
        }
        if (pattern is Pattern.Variable variable)
        {
            if (variable.Name == VariableScopeNames.Wildcard)
            {
                return true;
            }
            if (scope.TryGetValue(variable.Name, out var previous))
            {
                return previous.Equals(value);
            }
            scope.Add(variable.Name, value);

            return true;
        }

        return null;
    }

    private static bool Value(Expr expression, Dictionary<string, Term> scope, out Term value)
    {
        if (expression is Expr.Literal literal)
        {
            value = literal.Value;

            return true;
        }
        if (expression is Expr.Variable variable && scope.TryGetValue(variable.Name, out var bound))
        {
            value = bound;

            return true;
        }
        if (expression is Expr.Binary { Operator: ErlangOperators.ExactEqual or ErlangOperators.ExactNotEqual } binary
            && Value(binary.Left, scope, out var left) && Value(binary.Right, scope, out var right))
        {
            bool equal = left.Equals(right);
            value = Term.A((binary.Operator == ErlangOperators.ExactEqual ? equal : !equal)
                ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False);

            return true;
        }
        value = Nil.Value;

        return false;
    }
}
