namespace Erlang.Compiler;

internal static class ComprehensionSourceBindings
{
    internal static void Bind(ModuleDefinition module)
    {
        foreach (var function in module.Functions)
        {
            int coreOrdinal = 0;
            int liftedOrdinal = 0;
            var ordinals = new Dictionary<Expr.ListComprehension, List<(int Index, string Kind, int Arity, int Core)>>(ReferenceEqualityComparer.Instance);
            foreach (var expression in LocalFunctionEntries.ClauseExpressions(function.Clauses))
            {
                Core(expression);
            }
            foreach (var expression in LocalFunctionEntries.ClauseExpressions(function.Clauses))
            {
                Lift(expression);
            }

            void Core(Expr expression)
            {
                if (expression is Expr.ListComprehension list)
                {
                    foreach (var child in QualifierExpressions(list.Qualifiers))
                    {
                        Core(child);
                    }
                    var generated = new List<(int, string, int, int)>();
                    for (int index = 0; index < list.Qualifiers.Count; index++)
                    {
                        var qualifier = list.Qualifiers[index];
                        if (qualifier is ComprehensionQualifier.Filter)
                        {
                            continue;
                        }
                        bool zip = qualifier is ComprehensionQualifier.Zip;
                        int arity = zip ? ComprehensionGenerator.Flatten(((ComprehensionQualifier.Zip)qualifier).Generators).Count() : 1;
                        generated.Add((index, zip ? GeneratedComprehensionNames.ZipList : GeneratedComprehensionNames.List, arity, coreOrdinal++));
                    }
                    ordinals.Add(list, generated);
                    foreach (var child in list.Items)
                    {
                        Core(child);
                    }

                    return;
                }
                if (expression is Expr.MapComprehension
                    {
                        Fields: [{ Key: Expr.Variable, Value: Expr.Variable }],
                        Qualifiers: [var mapQualifier]
                    } && ComprehensionGenerator.From(mapQualifier) is
                    {
                        Binary: false,
                        Pattern: Pattern.Tuple { Items: [Pattern.Variable, Pattern.Variable] },
                        Source: Expr.Variable
                    })
                {
                    // v3_core lowers this map generator through lc_tq1 before translating subsequent funs.
                    coreOrdinal++;

                    return;
                }
                if (expression is Expr.Fun anonymous)
                {
                    foreach (var child in LocalFunctionEntries.Children(expression))
                    {
                        Core(child);
                    }
                    anonymous.CoreOrdinal = coreOrdinal++;

                    return;
                }
                if (expression is Expr.Apply application)
                {
                    Core(application.Function);
                    foreach (var argument in application.Arguments.Reverse())
                    {
                        Core(argument);
                    }

                    return;
                }

                if (expression is Expr.Bits bits)
                {
                    // expr_bin_1 visits segments in reverse constructor order.
                    // Within a segment bitstr still visits the value before its size.
                    foreach (var segment in bits.Segments.Reverse())
                    {
                        Core(segment.Value);
                        if (segment.Size is { } size)
                        {
                            Core(size);
                        }
                    }

                    return;
                }

                var children = LocalFunctionEntries.Children(expression);
                // v3_core's safe_list reserves identities right to left.
                // Evaluation and lifted function emission have their own order.
                bool usesSafeList = expression is Expr.List or Expr.Tuple or Expr.Call
                    or Expr.Binary
                {
                    Operator: not (ErlangOperators.AndAlso or ErlangOperators.OrElse
                            or ErlangOperators.And or ErlangOperators.Or or ErlangOperators.Xor)
                };
                foreach (var child in usesSafeList ? children.Reverse() : children)
                {
                    Core(child);
                }
            }

            void Lift(Expr expression)
            {
                if (expression is Expr.ListComprehension list)
                {
                    var generated = new List<GeneratedComprehensionFunction>();
                    foreach (var definition in ordinals[list])
                    {
                        if (ComprehensionLowering.EliminatesHelper(list.Qualifiers[definition.Index]))
                        {
                            continue;
                        }
                        int lifted = liftedOrdinal++;
                        generated.Add(new(
                            definition.Index,
                            GeneratedComprehensionNames.Function(
                                function.Name,
                                function.Arity,
                                definition.Kind,
                                definition.Core,
                                definition.Arity,
                                lifted
                            ),
                            definition.Arity + ComprehensionLowering.CapturedArity(list, definition.Index),
                            definition.Core,
                            lifted,
                            list.Qualifiers[definition.Index].SourceLine
                        )
                        {
                            DiscardConsContinuation = ComprehensionReturnAnalysis.KnownImproperSource(list.Qualifiers[definition.Index])
                        });
                    }
                    list.Source?.Bind(
                        function.Name,
                        function.Arity,
                        generated,
                        ComprehensionReturnAnalysis.TerminalBody(list, module, function)
                    );
                    foreach (var child in list.Items)
                    {
                        Lift(child);
                    }
                    foreach (var child in QualifierExpressions(list.Qualifiers))
                    {
                        Lift(child);
                    }

                    return;
                }
                foreach (var child in LocalFunctionEntries.Children(expression))
                {
                    Lift(child);
                }
            }
        }
    }

    private static IEnumerable<Expr> QualifierExpressions(IEnumerable<ComprehensionQualifier> qualifiers)
    {
        foreach (var qualifier in qualifiers)
        {
            if (qualifier is ComprehensionQualifier.Filter filter)
            {
                yield return filter.Expression;
            }
            else if (qualifier is ComprehensionQualifier.Zip zip)
            {
                foreach (var child in QualifierExpressions(zip.Generators))
                {
                    yield return child;
                }
            }
            else if (ComprehensionGenerator.From(qualifier) is { } generator)
            {
                yield return generator.Source;
            }
        }
    }
}
