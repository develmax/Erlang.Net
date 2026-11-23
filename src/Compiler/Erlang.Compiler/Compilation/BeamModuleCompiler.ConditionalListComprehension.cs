using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static (Expr LowerGuard, long Lower, long Upper, Integer Delta, int Line)? ReadConditionalListElement(
        Expr expression,
        string item
    )
    {
        if (item == VariableScopeNames.Wildcard || expression is not Expr.If
            {
                Clauses:
                [
                {
                    Guard: Expr.Binary
                    {
                        Operator: ErlangOperators.AndAlso,
                        Left: Expr.Binary { Operator: ErlangOperators.GreaterOrEqual, Left: Expr.Variable lowerRead } lowerGuard,
                        Right: Expr.Binary { Operator: ErlangOperators.LessOrEqual, Left: Expr.Variable upperRead } upperGuard
                    },
                    Body: Expr.Binary { Operator: ErlangOperators.Minus, Left: Expr.Variable changed } subtract
                },
                { Guard: Expr.Literal { Value: Atom { Name: "true" } }, Body: Expr.Variable fallback }
                ]
            }
            || lowerRead.Name != item || upperRead.Name != item || changed.Name != item || fallback.Name != item
            || !AttributeLiteral.TryNormalizeConstant(lowerGuard.Right, out Term lowerValue) || lowerValue is not Integer lower
            || !AttributeLiteral.TryNormalizeConstant(upperGuard.Right, out Term upperValue) || upperValue is not Integer upper
            || lower.Value < long.MinValue || upper.Value > long.MaxValue || lower.Value > upper.Value
            || !AttributeLiteral.TryNormalizeConstant(subtract.Right, out Term deltaValue) || deltaValue is not Integer delta)
        {
            return null;
        }

        return (lowerGuard, (long)lower.Value, (long)upper.Value, delta, subtract.SourceLine);
    }

    private sealed partial class GeneralEmitter
    {
        private bool TryEmitConditionalListComprehension(FunctionDefinition function)
        {
            if (!function.IsRecursiveListMapper || function.Clauses[0] is not
                {
                    Guard: null,
                    Patterns: [Pattern.List { Items: [Pattern.Variable item] }],
                    Body: Expr.List { Items: [var expression] }
                }
                || ReadConditionalListElement(expression, item.Name) is not { } update
                || !entries.TryGetValue((function.Name, 1), out int entry))
            {
                return false;
            }

            int empty = Label();
            int unchanged = Label();
            int recurse = Label();
            owner.EmitNonemptyList(0, empty);
            EmitFrameAllocation(1, 1);
            EmitFrameInitialization(0, 1);
            owner.EmitListParts(
                0,
                XRegisterTag,
                1,
                XRegisterTag,
                0
            );

            // Compare whole Erlang terms. Only the two successful bounds
            // taken together establish a numeric range for the subtraction.
            var guardInputs = new Clause([new Pattern.Variable(VariableScopeNames.Wildcard), item], null, expression);
            EmitEntryGuardPrefix(guardInputs, update.LowerGuard, unchanged);
            owner.Op(IsGreaterOrEqual, LabelTag, unchanged);
            owner.LiteralOperand(new Integer(update.Upper));
            owner.EmitTypedRegister(1, AnyTypeBits);
            BeamSourceLocation previous = location;
            owner.EmitArithmeticLine(update.Line, ref previous);
            owner.Op(GcBifTwo, LabelTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, ErlangOperators.Minus, 2));
            owner.EmitTypedRegister(
                1,
                NumberTypeBits,
                update.Lower,
                update.Upper
            );
            owner.LiteralOperand(update.Delta);
            owner.Operand(XRegisterTag, 1);
            SaveElement();
            JumpTo(recurse);

            Mark(unchanged);
            SaveElement();
            Mark(recurse);
            owner.EmitRecursiveCons(entry, 1);
            EmitRecursiveListMappingEnd(function, empty);

            return true;

            void SaveElement() => owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                0
            );
        }
    }
}
