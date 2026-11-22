namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private FunctionDefinition SinkCheckedCallArgumentFields(FunctionDefinition function)
    {
        if (!SsaTypeInferenceEnabled)
        {
            return function;
        }

        Clause[]? changed = null;
        int nextAlias = 0;
        for (int index = 0; index < function.Clauses.Count; index++)
        {
            Clause clause = function.Clauses[index];
            if (clause.Body is not Expr.Case { Value: Expr.Call selector } decision || !HasBindingFreeSelector(selector))
            {
                continue;
            }

            var entry = clause.Patterns.SelectMany(Semantics.Variables).ToHashSet(StringComparer.Ordinal);
            Clause[]? arms = null;
            for (int armIndex = 0; armIndex < decision.Clauses.Count; armIndex++)
            {
                Clause arm = decision.Clauses[armIndex];
                if (SinkCheckedCallArgumentFields(arm, entry, ref nextAlias) is { } rewritten)
                {
                    arms ??= decision.Clauses.ToArray();
                    arms[armIndex] = rewritten;
                }
            }

            if (arms is not null)
            {
                changed ??= function.Clauses.ToArray();
                changed[index] = clause with { Body = decision with { Clauses = arms } };
            }
        }

        return changed is null ? function : function with { Clauses = changed };
    }

    private static Clause? SinkCheckedCallArgumentFields(Clause arm, HashSet<string> entry, ref int nextAlias)
    {
        if (arm is not
            {
                Guard: not null,
                Patterns: [
                    Pattern.Tuple { Items: [Pattern.Literal { Value: Atom }, var payload] } outer
                ],
                Body: Expr.Case { Value: Expr.Call { DynamicRemote: false } call } continuation
            })
        {
            return null;
        }

        int[] nested = call.Arguments.Select((argument, index) => (argument, index))
            .Where(item => item.argument is Expr.Call
            { Module: null, DynamicRemote: false, Arguments: [Expr.Variable] })
            .Select(item => item.index).ToArray();
        int computation = nested is [var onlyArgument] ? onlyArgument : -1;
        if (computation == 0 || !call.Arguments.Where((_, index) => index != computation)
            .All(argument => argument is Expr.Variable || AttributeLiteral.TryNormalizeConstant(argument, out _)))
        {
            return null;
        }


        var (record, alias) = payload switch
        {
            Pattern.Tuple tuple => (tuple, (Pattern.Variable?)null),
            AliasPattern { Left: Pattern.Variable binding, Right: Pattern.Tuple tuple } => (tuple, binding),
            AliasPattern { Left: Pattern.Tuple tuple, Right: Pattern.Variable binding } => (tuple, binding),
            _ => ((Pattern.Tuple?)null, (Pattern.Variable?)null)
        };
        if (record is not { Items: [Pattern.Literal { Value: Atom tag }, ..] }
            || computation < 0 && !record.Items.Skip(1).Any(field => field is Pattern.Literal { Value: Atom or Integer }))
        {
            return null;
        }

        string[] bindings = Semantics.Variables(outer).ToArray();
        if (alias is not null && (entry.Contains(alias.Name) || bindings.Count(name => name == alias.Name) != 1
            || alias.Name == VariableScopeNames.Wildcard))
        {
            return null;
        }

        // A match in a later clause can read a binding without an Expr.Variable.
        // Removing just the selector call lets the scope collector see those reads.
        HashSet<string> otherUses;
        try
        {
            otherUses = ClosureCaptures.ReadEntryReferences(
                new Clause([], arm.Guard, continuation with { Value = new Expr.Literal(Nil.Value) }),
                bindings
            );
        }
        catch (NotSupportedException)
        {
            return null;
        }

        var selected = new List<(int Field, int Argument)>();
        for (int field = 1; field < record.Items.Count; field++)
        {
            if (record.Items[field] is not Pattern.Variable { Name: not VariableScopeNames.Wildcard } variable
                || entry.Contains(variable.Name) || otherUses.Contains(variable.Name)
                || bindings.Count(name => name == variable.Name) != 1)
            {
                continue;
            }

            int[] uses = call.Arguments.Select((value, index) => (value, index))
                .Where(operand => operand.value is Expr.Variable input && input.Name == variable.Name
                    || operand.index == computation && operand.value is Expr.Call { Arguments: [Expr.Variable computed] }
                        && computed.Name == variable.Name)
                .Select(operand => operand.index).ToArray();
            if (uses is [int argument])
            {
                selected.Add((field, argument));
            }
        }

        if (computation < 0 ? selected.Count != 2
            : selected.Count != 3 || !selected.Any(read => read.Argument == computation)
                || !selected.Any(read => read.Argument == 0))
        {
            return null;
        }

        alias ??= new Pattern.Variable(FormattableString.Invariant($"$checked_argument_record_{nextAlias++}"));
        Pattern[] fields = record.Items.ToArray();
        Expr[] arguments = call.Arguments.ToArray();
        foreach (var (field, argument) in selected)
        {
            fields[field] = new Pattern.Variable(VariableScopeNames.Wildcard);
            var access = new Expr.RecordAccess(
                new Expr.Variable(alias.Name),
                tag.Name,
                fields.Length,
                field
            )
            {
                SourceLine = call.SourceLine,
                PreprocessorFile = call.PreprocessorFile
            };
            arguments[argument] = argument == computation && arguments[argument] is Expr.Call computed
                ? computed with { Arguments = [access] }
                : access;
        }

        Pattern.Tuple shape = record with { Items = fields };
        Pattern retained = payload switch
        {
            AliasPattern { Left: Pattern.Variable } original => original with { Right = shape },
            AliasPattern original => original with { Left = shape },
            _ => new AliasPattern(alias, shape)
        };

        // The original arm still checks the carrier. Argument lowering projects
        // its two single-use fields directly into the final call registers.
        return arm with
        {
            Patterns = [outer with { Items = [outer.Items[0], retained] }],
            Body = continuation with { Value = call with { Arguments = arguments } }
        };
    }
}
