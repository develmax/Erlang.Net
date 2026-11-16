using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private const int FloatTypeBits = 1 << 3;

        private bool TryEmitRemoteResultTailCall(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null, Patterns: [],
                    Body: Expr.Call
                    {
                        Module: null, DynamicRemote: false, Arguments: [
                    Expr.Call { Module: not null, DynamicRemote: false, Arguments: [] } input
                ]
                    } output
                }] || !entries.TryGetValue((output.Function, 1), out int target))
            {
                return false;
            }

            owner.Op(Allocate, BeamWire.UnsignedTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.EmitCallLine(input, location);
            owner.EmitStaticRemoteCall(input);
            owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(LabelTag, target);
            owner.Operand(BeamWire.UnsignedTag, 0);

            return true;
        }

        private bool TryEmitSplitTupleFormat(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null, Patterns: [AliasPattern
                    {
                        Left: Pattern.Tuple { Items: [Pattern.Variable head1, Pattern.Variable head2, Pattern.Variable fraction] },
                        Right: Pattern.Variable input
                    }],
                    Body: Expr.Sequence
                    {
                        Items: [
                    Expr.Match
                        {
                            Pattern: Pattern.Tuple { Items: [Pattern.Variable left, Pattern.Variable right] },
                            Value: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [Expr.Variable source] } split
                        } pair,
                    Expr.Match
                        {
                            Pattern: Pattern.Tuple { Items: [Pattern.Variable a, Pattern.Variable b, Pattern.Variable c] },
                            Value: Expr.Variable leftValue
                        } first,
                    Expr.Match
                        {
                            Pattern: Pattern.Tuple { Items: [Pattern.Variable d, Pattern.Variable e, Pattern.Variable f] },
                            Value: Expr.Variable rightValue
                        } second,
                    Expr.Match
                        {
                            Pattern: Pattern.Variable result, Value: Expr.Call
                            {
                                Module: not null, DynamicRemote: false, Arguments: [
                            Expr.Literal format, Expr.List
                                {
                                    Tail: null, Items: [
                                Expr.Variable a1, Expr.Variable b1, Expr.Variable c1,
                                Expr.Variable d1, Expr.Variable e1, Expr.Variable f1,
                                Expr.Call
                                    {
                                        Module: null or ErlangModuleNames.Module, Function: AutoImportNames.Round,
                                        DynamicRemote: false, Arguments: [Expr.Binary
                                        {
                                            Operator: ErlangOperators.Divide, Left: Expr.Variable numerator,
                                            Right: Expr.Literal { Value: Integer divisor }
                                        } divide]
                                    } round
                            ]
                                }
                        ]
                            } write
                        },
                    Expr.Call { Module: not null, DynamicRemote: false, Arguments: [Expr.Variable returned] } finish
                ]
                    }
                }] || !DistinctBindings(
                    head1,
                    head2,
                    fraction,
                    input,
                    left,
                    right,
                    a,
                    b,
                    c,
                    d,
                    e,
                    f,
                    result
                )
                || source.Name != input.Name || left.Name != leftValue.Name || right.Name != rightValue.Name
                || a.Name != a1.Name || b.Name != b1.Name || c.Name != c1.Name
                || d.Name != d1.Name || e.Name != e1.Name || f.Name != f1.Name
                || fraction.Name != numerator.Name || result.Name != returned.Name
                || round.Module is null && entries.ContainsKey((AutoImportNames.Round, 1))
                || divisor.Value < int.MinValue || divisor.Value > int.MaxValue)
            {
                return false;
            }

            int info = owner.nextLabel - 2;
            int badSecond = Label();
            int badFirst = Label();
            int badPair = Label();
            owner.EmitTupleCheck(0, 3, info);
            owner.Op(Allocate, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                0
            );
            owner.EmitCallLine(split, location);
            owner.EmitStaticRemoteCall(split);
            owner.EmitTupleCheck(0, 2, badPair);
            owner.EmitTuplePart(0, 0, 1);
            owner.EmitTupleCheck(1, 3, badFirst);
            owner.EmitTuplePart(0, 1, 0);
            owner.EmitTupleCheck(0, 3, badSecond);
            owner.Op(GetTupleElement, YRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Operand(XRegisterTag, 2);
            var previous = CallLocation(split, location);
            owner.EmitArithmeticLine(divide.SourceLine, ref previous);
            EmitRoundedDivision((int)divisor.Value);

            owner.Op(TestHeap, BeamWire.UnsignedTag, 7 * ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 3);
            owner.EmitTuplePart(0, 0, 3);
            owner.EmitTuplePart(0, 1, 4);
            owner.EmitTuplePart(0, 2, 0);
            owner.EmitTuplePart(1, 0, 5);
            owner.EmitTuplePart(1, 1, 6);
            owner.EmitTuplePart(1, 2, 1);
            EmitFormatList();
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.EmitConstant(format.Value, 0);
            owner.EmitCallLine(write, previous);
            owner.EmitStaticRemoteCall(write);
            owner.EmitCallLine(finish, CallLocation(write, previous));
            owner.EmitTailRemote(finish.Module, finish.Function, framed: true);

            EmitTupleFormatFailure(badSecond, second, 0);
            EmitTupleFormatFailure(badFirst, first, 1);
            EmitTupleFormatFailure(badPair, pair, 0);

            return true;
        }

        private void EmitRoundedDivision(int divisor)
        {
            owner.Op(FloatConvert, XRegisterTag, 2);
            EmitFloatRegister(0);
            owner.instructions.WriteByte(FloatMove);
            owner.LiteralOperand(new FloatTerm(divisor));
            EmitFloatRegister(1);
            EmitFloatDivisionResult(live: 2, destination: 2);
            owner.Op(GcBifOne, LabelTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, AutoImportNames.Round, 1));
            owner.EmitTypedRegister(2, FloatTypeBits);
            owner.Operand(XRegisterTag, 2);
        }

        private void EmitFloatDivisionResult(int live, int destination)
        {
            owner.Op(FloatDivide, LabelTag, 0);
            EmitFloatRegister(0);
            EmitFloatRegister(1);
            EmitFloatRegister(0);
            owner.Op(TestHeap, ExtendedTag, AllocationList);
            owner.Operand(BeamWire.UnsignedTag, AllocationKinds);
            owner.Operand(BeamWire.UnsignedTag, AllocationWords);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.Operand(BeamWire.UnsignedTag, AllocationFloats);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, AllocationFuns);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.Operand(BeamWire.UnsignedTag, live);
            owner.instructions.WriteByte(FloatMove);
            EmitFloatRegister(0);
            owner.Operand(XRegisterTag, destination);
        }

        private void EmitFloatRegister(int register)
        {
            owner.instructions.WriteByte(ExtendedFloatRegisterOperand);
            owner.Operand(BeamWire.UnsignedTag, register);
        }

        private void EmitFormatList()
        {
            owner.Op(PutList, XRegisterTag, 2);
            owner.LiteralOperand(Nil.Value);
            owner.Operand(XRegisterTag, 2);
            owner.Op(PutList, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 2);
            owner.Operand(XRegisterTag, 0);
            foreach (int register in new[] { 4, 3, 1, 6, 5 })
            {
                owner.Op(PutList, XRegisterTag, register);
                owner.Operand(XRegisterTag, 0);
                owner.Operand(XRegisterTag, register == 5 ? 1 : 0);
            }
        }

        private void EmitTupleFormatFailure(int label, Expr.Match match, int register)
        {
            Mark(label);
            owner.EmitLine(match.SourceLine == 0 ? location : new(match.PreprocessorFile, match.SourceLine));
            owner.Op(BadMatch, XRegisterTag, register);
        }
    }
}
