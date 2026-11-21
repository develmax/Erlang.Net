using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitCountedMaskAccumulator(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled
                || function.Clauses is not
                [
                {
                    Guard: null,
                    Patterns: [Pattern.Literal { Value: Integer zero }, Pattern.Variable finalList, Pattern.Variable finalCount],
                    Body: Expr.Tuple { Items: [Expr.Variable returnedList, Expr.Variable returnedCount] }
                },
                {
                    Guard: null,
                    Patterns: [Pattern.Variable value, Pattern.Variable list, Pattern.Variable count],
                    Body: Expr.Call
                    {
                        Module: null,
                        DynamicRemote: false,
                        Arguments:
                            [
                                Expr.Binary
                            {
                                Operator: ErlangOperators.ShiftRight,
                                Left: Expr.Variable shifted, Right: Expr.Literal { Value: Integer width }
                            } shift,
                                Expr.List
                            {
                                Items:
                                    [
                                        Expr.Binary
                                    {
                                        Operator: ErlangOperators.BitwiseOr,
                                        Left: Expr.Binary
                                        {
                                            Operator: ErlangOperators.BitwiseAnd,
                                            Left: Expr.Variable masked, Right: Expr.Literal { Value: Integer mask }
                                        } maskOperation,
                                        Right: Expr.Literal { Value: Integer marker }
                                    } markerOperation
                                    ],
                                Tail: Expr.Variable rest
                            },
                                Expr.Binary
                            {
                                Operator: ErlangOperators.Plus,
                                Left: Expr.Variable incremented, Right: Expr.Literal { Value: Integer increment }
                            } add
                            ]
                    } recur
                }
                ]
                || !zero.Value.IsZero || !DistinctBindings(finalList, finalCount) || !DistinctBindings(value, list, count)
                || finalList.Name != returnedList.Name || finalCount.Name != returnedCount.Name
                || value.Name != shifted.Name || value.Name != masked.Name || list.Name != rest.Name
                || count.Name != incremented.Name || recur.Function != function.Name
                || width.Value <= 0 || width.Value > int.MaxValue || mask.Value < 0 || mask.Value > int.MaxValue
                || marker.Value < 0 || marker.Value > int.MaxValue || increment.Value <= 0 || increment.Value > int.MaxValue
                || !entries.TryGetValue((function.Name, 3), out int entry))
            {
                return false;
            }

            PairFacts argument = ReadJoinedNumericArgument(function, 0);
            PairFacts counter = ReadJoinedNumericArgument(function, 2);
            if ((argument & PairFacts.Natural) != PairFacts.Natural
                && (argument & (PairFacts.Integer | PairFacts.NonnegativeNumber)) != (PairFacts.Integer | PairFacts.NonnegativeNumber)
                || (counter & PairFacts.Positive) != PairFacts.Positive)
            {
                return false;
            }

            int recurse = Label();
            owner.Op(ExactEqualTest, LabelTag, recurse);
            owner.EmitTypedRegister(0, IntegerTypeBits, 0);
            owner.LiteralOperand(zero);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 3);
            owner.EmitTagMatchTuple(0, (XRegisterTag, 1), (XRegisterTag, 2));
            owner.instructions.WriteByte(Return);

            Mark(recurse);
            BeamSourceLocation previous = location;
            EmitOperation(
                shift,
                width,
                0,
                3,
                3,
                1
            );
            EmitOperation(
                maskOperation,
                mask,
                0,
                4,
                0,
                1
            );
            EmitOperation(
                markerOperation,
                marker,
                0,
                4,
                0,
                0,
                (long)mask.Value
            );
            EmitOperation(
                add,
                increment,
                2,
                4,
                2,
                1
            );

            owner.Op(TestHeap, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, 4);
            owner.Op(PutList, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            owner.Operand(XRegisterTag, 1);
            owner.MoveToX0(XRegisterTag, 3);
            owner.Op(TailLocalCall, BeamWire.UnsignedTag, 3);
            owner.Operand(LabelTag, entry);

            return true;

            void EmitOperation(
                Expr.Binary operation,
                Integer constant,
                int input,
                int live,
                int result,
                long minimum,
                long? maximum = null
            )
            {
                owner.EmitArithmeticLine(operation.SourceLine, ref previous);
                owner.Op(GcBifTwo, LabelTag, 0);
                owner.Operand(BeamWire.UnsignedTag, live);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, operation.Operator, 2));
                owner.EmitTypedRegister(
                    input,
                    IntegerTypeBits,
                    minimum,
                    maximum
                );
                owner.LiteralOperand(constant);
                owner.Operand(XRegisterTag, result);
            }
        }
    }
}
