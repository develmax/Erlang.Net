using System.Numerics;
using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitGuardedFloatRecursion(FunctionDefinition function, int failure)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || owner.IsDeclaredNative((function.Name, function.Arity))
                || function.Clauses is not
                [
                {
                    Patterns: [Pattern.Variable input],
                    Body: Expr.If { Clauses: [var shortArm, var recursiveArm] }
                } clause
                ]
                || !DistinctBindings(input)
                || ReadEntryTypeGuard(clause, clause.Guard) is not { Opcode: IsInteger, Register: 0 }
                || shortArm.Guard is not Expr.Binary
                {
                    Operator: ErlangOperators.Less,
                    Left: Expr.Variable checkedValue,
                    Right: Expr.Literal { Value: Integer threshold }
                }
                || checkedValue.Name != input.Name || threshold.Value <= 0 || threshold.Value > long.MaxValue
                || recursiveArm.Guard is not Expr.Literal { Value: Atom { Name: "true" } }
                || shortArm.Body is not Expr.Call
                {
                    Module: not null, DynamicRemote: false,
                    Arguments: [
                        Expr.Literal shortFormat,
                        Expr.List { Tail: null, Items: [Expr.Binary { Operator: ErlangOperators.Divide } fraction] }
                    ]
                } direct
                || recursiveArm.Body is not Expr.Call
                {
                    Module: not null, DynamicRemote: false,
                    Arguments: [
                        Expr.Literal recursiveFormat,
                        Expr.List
                    {
                        Tail: null,
                        Items: [
                                Expr.Binary { Operator: ErlangOperators.IntegerDivide } quotient,
                                Expr.Call
                        {
                            Module: null, DynamicRemote: false,
                            Arguments: [Expr.Binary { Operator: ErlangOperators.Remainder } remainder]
                        } recurse
                            ]
                    }
                    ]
                } recursive
                || direct.Module != recursive.Module || direct.Function != recursive.Function
                || !TryResolveOrdinaryCallTarget(direct, out string? module) || module is null
                || IsTrappingTailBif(module, direct.Function, direct.Arguments.Count)
                || recurse.Function != function.Name || !entries.TryGetValue((recurse.Function, 1), out int entry)
                || !ReadIntegerOperand(fraction, input, out Integer scale)
                || !TryReadExactFloatDivisor(scale, out double divisor)
                || !ReadIntegerOperand(quotient, input, out Integer divisorBound) || divisorBound.Value != threshold.Value
                || !ReadIntegerOperand(remainder, input, out Integer remainderBound) || remainderBound.Value != threshold.Value)
            {
                return false;
            }

            const int floatListWords = 2;
            const int recursiveListWords = 4;
            const int quotientSlot = 0;
            const int recursiveFrame = 1;
            const int arity = 1;
            const int formatArity = 2;
            int repeat = Label();
            int formatter = owner.Import(module, direct.Function, formatArity);
            long lower = (long)threshold.Value;
            long upper = lower - 1;

            owner.Op(IsInteger, LabelTag, failure);
            owner.Operand(XRegisterTag, 0);
            owner.Op(IsGreaterOrEqual, LabelTag, repeat);
            owner.LiteralOperand(new Integer(upper));
            owner.EmitTypedRegister(0, IntegerTypeBits);

            var previous = location;
            owner.EmitArithmeticLine(fraction.SourceLine, ref previous);
            owner.instructions.WriteByte(FloatConvert);
            owner.EmitTypedRegister(0, IntegerTypeBits, upper: upper);
            EmitFloatRegister(0);
            owner.instructions.WriteByte(FloatMove);
            owner.LiteralOperand(new FloatTerm(divisor));
            EmitFloatRegister(1);
            EmitFloatArithmeticResult(
                FloatDivide,
                live: 0,
                destination: 0,
                additionalHeapWords: floatListWords
            );
            owner.Op(PutList, XRegisterTag, 0);
            owner.LiteralOperand(Nil.Value);
            owner.Operand(XRegisterTag, 1);
            owner.EmitConstant(shortFormat.Value, 0);
            owner.EmitCallLine(direct, previous);
            owner.Op(TailExternalCall, BeamWire.UnsignedTag, formatArity);
            owner.Operand(BeamWire.UnsignedTag, formatter);

            Mark(repeat);
            EmitFrameAllocation(arity, recursiveFrame);
            EmitFrameInitialization(quotientSlot, recursiveFrame);
            previous = location;
            EmitIntegerOperation(quotient, YRegisterTag);
            EmitIntegerOperation(remainder, XRegisterTag);
            owner.EmitCallLine(recurse, previous);
            previous = CallLocation(recurse, previous);
            owner.Op(LocalCall, BeamWire.UnsignedTag, arity);
            owner.Operand(LabelTag, entry);
            owner.Op(TestHeap, BeamWire.UnsignedTag, recursiveListWords);
            owner.Operand(BeamWire.UnsignedTag, arity);
            owner.Op(PutList, XRegisterTag, 0);
            owner.LiteralOperand(Nil.Value);
            owner.Operand(XRegisterTag, 0);
            owner.Op(PutList, YRegisterTag, quotientSlot);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            owner.EmitConstant(recursiveFormat.Value, 0);
            owner.EmitCallLine(recursive, previous);
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, formatArity);
            owner.Operand(BeamWire.UnsignedTag, formatter);
            owner.Operand(BeamWire.UnsignedTag, recursiveFrame);

            return true;

            void EmitIntegerOperation(Expr.Binary operation, int destination)
            {
                owner.EmitArithmeticLine(operation.SourceLine, ref previous);
                owner.Op(GcBifTwo, LabelTag, 0);
                owner.Operand(BeamWire.UnsignedTag, arity);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, operation.Operator, 2));
                owner.EmitTypedRegister(0, IntegerTypeBits, lower: lower);
                owner.LiteralOperand(threshold);
                owner.Operand(destination, 0);
            }
        }

        private static bool ReadIntegerOperand(Expr.Binary operation, Pattern.Variable input, out Integer value)
        {
            if (operation is { Left: Expr.Variable source, Right: Expr.Literal { Value: Integer integer } }
                && source.Name == input.Name)
            {
                value = integer;

                return true;
            }

            value = default!;

            return false;
        }

        private static bool TryReadExactFloatDivisor(Integer scale, out double divisor)
        {
            divisor = (double)scale.Value;

            return divisor != 0 && double.IsFinite(divisor) && new BigInteger(divisor) == scale.Value;
        }
    }
}
