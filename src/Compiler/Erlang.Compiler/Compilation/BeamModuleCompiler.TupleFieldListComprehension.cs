using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static (int Arity, Atom? Tag, int? Field)? ReadTupleListProjection(Pattern item, Expr body)
    {
        (Pattern, Pattern.Variable?) projection = item switch
        {
            AliasPattern { Left: Pattern.Tuple left, Right: Pattern.Variable variable } => (left, variable),
            AliasPattern { Left: Pattern.Variable variable, Right: Pattern.Tuple right } => (right, variable),
            _ => (item, null)
        };
        var (pattern, alias) = projection;
        if (pattern is not Pattern.Tuple tuple || body is not Expr.Variable { Name: not VariableScopeNames.Wildcard } value)
        {
            return null;
        }

        Atom? tag = tuple.Items.FirstOrDefault() is Pattern.Literal { Value: Atom atom } ? atom : null;
        if (alias is not null && (tag is null || alias.Name != value.Name))
        {
            return null;
        }

        int? field = null;
        var names = new HashSet<string>(StringComparer.Ordinal);
        if (alias is not null)
        {
            names.Add(alias.Name);
        }

        for (int index = tag is null ? 0 : 1; index < tuple.Items.Count; index++)
        {
            if (tuple.Items[index] is not Pattern.Variable binding
                || binding.Name != VariableScopeNames.Wildcard && !names.Add(binding.Name))
            {
                return null;
            }

            if (binding.Name == value.Name)
            {
                field = index;
            }
        }

        return alias is not null || field.HasValue ? (tuple.Items.Count, tag, field) : null;
    }

    private sealed partial class GeneralEmitter
    {
        private bool TryEmitTupleFieldListComprehension(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || !function.IsRecursiveListMapper
                || function.Clauses[0] is not
                {
                    Guard: null,
                    Patterns: [Pattern.List { Items: [var item] }],
                    Body: Expr.List { Items: [var mapped] }
                }
                || ReadTupleListProjection(item, mapped) is not { } projection
                || !entries.TryGetValue((function.Name, 1), out int entry))
            {
                return false;
            }

            int empty = Label();
            int skip = Label();
            owner.EmitNonemptyList(0, empty);
            owner.EmitListParts(
                0,
                XRegisterTag,
                1,
                XRegisterTag,
                0
            );
            if (projection.Tag is { } tag)
            {
                EmitDetailRecordTest(
                    1,
                    projection.Arity,
                    tag,
                    skip
                );
            }
            else
            {
                owner.EmitTupleCheck(1, projection.Arity, skip);
            }

            EmitFrameAllocation(2, 1);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                0
            );

            if (projection.Field is int field)
            {
                // The selected field's only use follows the recursive call.
                owner.EmitFieldCall(1, entry);
                owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
                owner.Operand(BeamWire.UnsignedTag, 1);
                owner.EmitTuplePart(
                    0,
                    field,
                    1,
                    inputTag: YRegisterTag
                );
                owner.Op(PutList, XRegisterTag, 1);
                owner.Operand(XRegisterTag, 0);
                owner.Operand(XRegisterTag, 0);
                owner.EmitFrameReturn(1);
            }
            else
            {
                owner.EmitRecursiveCons(entry, 1);
            }

            Mark(skip);
            EmitRecursiveListMappingRejection(function, entry, 1);
            EmitRecursiveListMappingEnd(function, empty);

            return true;
        }
    }
}
