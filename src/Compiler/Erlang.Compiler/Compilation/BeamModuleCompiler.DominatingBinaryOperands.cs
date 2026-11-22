namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static FunctionDefinition FoldDominatingBinaryOperands(FunctionDefinition function)
    {
        Clause[]? clauses = null;
        for (int index = 0; index < function.Clauses.Count; index++)
        {
            Clause clause = function.Clauses[index];
            if (clause.Body is not Expr.Sequence sequence
                || !sequence.Items.SkipLast(1).Any(item =>
                    item is Expr.Match { Pattern: Pattern.Variable, Value: Expr.Literal }))
            {
                continue;
            }

            Expr.Sequence? body = ReadDominatingBinaryOperands(clause, sequence);
            if (body is not null)
            {
                clauses ??= function.Clauses.ToArray();
                clauses[index] = clause with { Body = body };
            }
        }

        return clauses is null ? function : function with { Clauses = clauses };
    }

    private static Expr.Sequence? ReadDominatingBinaryOperands(Clause clause, Expr.Sequence sequence)
    {
        var bound = clause.Patterns.SelectMany(Semantics.Variables).ToHashSet(StringComparer.Ordinal);
        var constants = new Dictionary<string, Term>(StringComparer.Ordinal);
        Expr[]? rewritten = null;
        try
        {
            for (int index = 0; index < sequence.Items.Count; index++)
            {
                Expr statement = sequence.Items[index];
                if (constants.Count > 0 && LocalFunctionEntries.Walk(statement).Any(UsesConstantBinaryOperand))
                {
                    bool changed = false;
                    Expr replacement = BeamTargetedInlining.Rewrite(
                        statement,
                        RewriteBinaryOperands,
                        rewriteScope: item => item is Expr.Fun ? item : null
                    );
                    if (changed)
                    {
                        rewritten ??= sequence.Items.ToArray();
                        rewritten[index] = replacement;
                    }

                    Expr RewriteBinaryOperands(Expr item)
                    {
                        if (item is not Expr.Bits bits || !UsesConstantBinaryOperand(bits))
                        {
                            return item;
                        }

                        changed = true;

                        return bits with
                        {
                            Segments = bits.Segments.Select(segment => segment with
                            {
                                Value = Substitute(segment.Value)!,
                                Size = Substitute(segment.Size)
                            }).ToArray()
                        };
                    }
                }

                // A match against an earlier binding tests a value. It cannot
                // replace a fact established by an earlier successful edge.
                var binding = statement as Expr.Match;
                string? name = binding?.Pattern is Pattern.Variable { Name: not VariableScopeNames.Wildcard } variable
                    && !bound.Contains(variable.Name)
                    && !bound.Contains(VariableScopeNames.UnsafePrefix + variable.Name) ? variable.Name : null;
                Semantics.TrackSourceBindings(statement, bound);
                if (name is not null && binding?.Value is Expr.Literal literal)
                {
                    constants.Add(name, literal.Value);
                }
            }
        }
        catch (Exception error) when (error is CompileException or NotSupportedException)
        {
            // A synthetic clause can contain a different binding representation.
            return null;
        }

        return rewritten is null ? null : sequence with { Items = rewritten };

        bool UsesConstantBinaryOperand(Expr expression) => expression is Expr.Bits bits
            && bits.Segments.Any(segment => IsConstant(segment.Value) || IsConstant(segment.Size));

        bool IsConstant(Expr? expression) => expression is Expr.Variable variable && constants.ContainsKey(variable.Name);

        Expr? Substitute(Expr? expression) => expression is Expr.Variable variable
            && constants.TryGetValue(variable.Name, out Term? value)
            ? new Expr.Literal(value) : expression;
    }
}
