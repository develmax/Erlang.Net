using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitPositionedBinarySegmentList(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || recursiveClosure is not null
                || function.Arity != 2 || owner.OnLoadFunction is not null || owner.IsDeclaredNative((function.Name, 2))
                || function.Clauses is not
                [
                {
                    Guard: null, Patterns: [BitPattern { Segments.Count: 0 }, Pattern.Variable empty],
                    Body: Expr.Call
                    {
                        Module: ListModuleNames.Module, Function: ListModuleNames.Reverse, DynamicRemote: false,
                        Arguments: [Expr.Variable returned]
                    } reverse
                },
                {
                    Guard: null, Patterns: [BitPattern { Segments: [var integer, var tail] }, Pattern.Variable input],
                    Body: Expr.Call
                    {
                        Module: null, DynamicRemote: false,
                        Arguments:
                            [
                                Expr.Variable next,
                                Expr.List { Items: [Expr.Bits { Segments: [var head] } bits], Tail: Expr.Variable accumulator }
                            ]
                    } recur
                }
                ]
                || returned.Name != empty.Name || !DistinctBindings(empty)
                || integer.Value is not Pattern.Variable read || ReadBinaryTailBinding(tail) is not { } rest
                || !DistinctBindings(read, rest, input) || recur.Function != function.Name
                || next.Name != rest.Name || accumulator.Name != input.Name
                || head.Value is not Expr.Variable consumed || consumed.Name != read.Name
                || head is { IsStringLiteral: true } || !ReadUnsignedWidth(integer.Specification, out int inputWidth)
                || !ReadUnsignedWidth(head, out int outputWidth) || !entries.TryGetValue((function.Name, 2), out int entry))
            {
                return false;
            }

            bool hasSeed = false;
            if (!HasOnlyLocalCalls(
                function,
                2,
                (_, call) =>
                {
                    if (ReferenceEquals(recur, call))
                    {
                        return true;
                    }

                    if (!AttributeLiteral.TryNormalizeConstant(call.Arguments[1], out var seed) || seed is not Nil)
                    {
                        return false;
                    }

                    hasSeed = true;

                    return true;
                }
            ) || !hasSeed)
            {
                return false;
            }

            const int context = 2;
            int functionInfo = owner.nextLabel - 2;
            int nonempty = Label();
            int incomplete = Label();
            owner.Op(StartBinaryMatch, LabelTag, functionInfo);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Operand(XRegisterTag, context);
            owner.Op(GetBinaryPosition, XRegisterTag, context);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 3);
            EmitBitMatchHeader(
                context,
                nonempty,
                2,
                0,
                tailUnit: null
            );
            owner.MoveToX0(XRegisterTag, 1);
            EmitRegisterTailTarget(reverse, 1);

            Mark(nonempty);
            EmitBitMatchHeader(
                context,
                incomplete,
                9,
                inputWidth,
                BitSyntaxDefaults.BinaryUnit
            );
            EmitBitSegment(
                integer.Specification,
                inputWidth,
                3,
                XRegisterTag,
                0
            );
            var previous = location;
            owner.EmitArithmeticLine(bits.SourceLine, ref previous);
            owner.BeginBinaryConstruction(
                0,
                3,
                outputWidth,
                0,
                1,
                heapWords: ConsHeapWords
            );
            owner.EmitBinarySegmentPrefix(
                head.Type,
                1,
                head.Unit,
                head.Endian
            );
            owner.EmitTypedRegister(
                0,
                IntegerTypeBits,
                0,
                (1L << inputWidth) - 1
            );
            owner.LiteralOperand(Term.I(outputWidth));
            owner.Op(PutList, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            owner.Operand(XRegisterTag, 1);
            owner.MoveToX0(XRegisterTag, context);
            owner.Op(TailLocalCall, BeamWire.UnsignedTag, 2);
            owner.Operand(LabelTag, entry);

            Mark(incomplete);
            // Extraction writes X0 only when the match has succeeded.
            owner.Op(SetBinaryPosition, XRegisterTag, context);
            owner.Operand(XRegisterTag, 0);
            owner.Op(GetBinaryTail, XRegisterTag, context);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 3);
            owner.Op(Jump, LabelTag, functionInfo);

            return true;
        }

        private static bool ReadUnsignedWidth(BitSegment segment, out int width)
        {
            width = FixedSize(segment) ?? 0;

            return segment is
            {
                Type: BitSegmentTypes.Integer, Unit: BitSyntaxDefaults.NumericUnit, Endian: BitByteOrders.Big, Signed: false
            }
                && width is >= BitSyntaxDefaults.BinaryUnit and <= sizeof(uint) * BitSyntaxDefaults.BinaryUnit
                && width % BitSyntaxDefaults.BinaryUnit == 0;
        }
    }
}
