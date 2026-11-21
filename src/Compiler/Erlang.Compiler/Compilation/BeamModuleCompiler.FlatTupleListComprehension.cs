using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static Term?[]? ReadFlatTupleListElement(Expr expression, string item)
    {
        if (item == VariableScopeNames.Wildcard || expression is not Expr.Tuple tuple)
        {
            return null;
        }

        var fields = new Term?[tuple.Items.Count];
        bool hasElement = false;
        for (int index = 0; index < fields.Length; index++)
        {
            Expr field = tuple.Items[index];
            if (field is Expr.Variable variable && variable.Name == item)
            {
                hasElement = true;
            }
            else if (AttributeLiteral.TryNormalizeConstant(field, out Term literal))
            {
                fields[index] = literal;
            }
            else
            {
                return null;
            }
        }

        return hasElement ? fields : null;
    }

    private sealed partial class GeneralEmitter
    {
        private bool TryEmitFlatTupleListComprehension(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || !function.IsRecursiveListMapper
                || function.Clauses[0] is not
                {
                    Guard: null,
                    Patterns: [Pattern.List { Items: [Pattern.Variable item] }],
                    Body: Expr.List { Items: [var expression] }
                }
                || !entries.TryGetValue((function.Name, 1), out int entry))
            {
                return false;
            }

            int singletonWrappers = 0;
            while (expression is Expr.List { Items: [var wrapped], Tail: null or Expr.Literal { Value: Nil } })
            {
                singletonWrappers++;
                expression = wrapped;
            }

            if (ReadFlatTupleListElement(expression, item.Name) is not { } fields)
            {
                return false;
            }

            int empty = Label();
            owner.EmitNonemptyList(0, empty);
            EmitFrameAllocation(1, 1);
            owner.EmitListParts(
                0,
                YRegisterTag,
                0,
                XRegisterTag,
                0
            );

            // All head tuple fields are existing terms. Keep the generator
            // value alive and construct the tuple together with the cons.
            owner.EmitRecursiveCons(
                entry,
                1,
                fields,
                singletonWrappers
            );
            EmitRecursiveListMappingEnd(function, empty);

            return true;
        }
    }
}
