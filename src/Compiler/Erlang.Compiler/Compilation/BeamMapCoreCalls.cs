namespace Erlang.Compiler;

internal static class BeamMapCoreCalls
{
    internal static IReadOnlyList<FunctionDefinition> Rewrite(
        IReadOnlyList<FunctionDefinition> functions,
        SourceCallBindings bindings
    )
    {
        var definitions = new Dictionary<RecordDefinition, RecordDefinition>(ReferenceEqualityComparer.Instance);

        return functions.Select(function => function with
        {
            Clauses = function.Clauses.Select(clause => clause with
            {
                Guard = clause.Guard is null ? null : RewriteExpression(clause.Guard),
                Body = RewriteExpression(clause.Body)
            }).ToArray()
        }).ToArray();

        Expr RewriteExpression(Expr expression) => BeamTargetedInlining.Rewrite(
            expression,
            candidate => candidate switch
            {
                Expr.Call { DynamicRemote: false } call => RewriteCall(call, bindings),
                Expr.RecordConstruction record => record with { Definition = RewriteDefinition(record.Definition) },
                _ => candidate
            },
            rewriteComprehensions: true
        );

        RecordDefinition RewriteDefinition(RecordDefinition definition)
        {
            if (!definitions.TryGetValue(definition, out RecordDefinition? rewritten))
            {
                rewritten = definition with
                {
                    Fields = definition.Fields.Select(field => field with { Value = RewriteExpression(field.Value) }).ToArray()
                };
                definitions.Add(definition, rewritten);
            }

            return rewritten;
        }
    }

    private static Expr RewriteCall(Expr.Call call, SourceCallBindings bindings)
    {
        int arity = call.Arguments.Count;
        if ((call.Module ?? bindings.ImportedModule(call.Function, arity)) != MapModuleNames.Module)
        {
            return call;
        }

        if (call.Function == MapModuleNames.New && arity == 0)
        {
            return new Expr.Literal(new MapTerm([]));
        }

        string? builtin = (call.Function, arity) switch
        {
            (MapModuleNames.Get, 2) => ErlangModuleNames.MapGet,
            (MapModuleNames.IsKey, 2) => ErlangModuleNames.IsMapKey,
            (MapModuleNames.Size, 1) => ErlangModuleNames.MapSize,
            _ => null
        };

        return builtin is null ? call : call with { Module = ErlangModuleNames.Module, Function = builtin };
    }
}
