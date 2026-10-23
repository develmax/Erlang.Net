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
                if (expression is Expr.Fun anonymous)
                {
                    foreach (var child in LocalFunctionEntries.Children(expression))
                    {
                        Core(child);
                    }
                    anonymous.CoreOrdinal = coreOrdinal++;

                    return;
                }
                foreach (var child in LocalFunctionEntries.Children(expression))
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
