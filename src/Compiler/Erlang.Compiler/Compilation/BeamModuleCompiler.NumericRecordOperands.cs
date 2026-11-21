using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private const int FirstRecordUpdateValueOperand = 6;
    private const int RecordUpdateFieldPairWidth = 2;

    private int AnnotateNumericRecordFields(
        ReadOnlyMemory<byte> code,
        Stream output,
        int copied,
        BeamInstruction instruction,
        HashSet<BeamOperand> values,
        (BeamOperand Slot, long Minimum)? refinement,
        BeamOperand? integerResult
    )
    {
        for (int index = FirstRecordUpdateValueOperand; index < instruction.Operands.Count; index += RecordUpdateFieldPairWidth)
        {
            BeamOperand operand = instruction.Operands[index];
            long? minimum = refinement is { } known && known.Slot == operand ? known.Minimum : null;
            bool integer = integerResult is not null && SameProjectionRegister(operand, integerResult);
            if (integer || minimum.HasValue || values.Contains(operand))
            {
                copied = AnnotateRegisterInput(
                    code,
                    output,
                    copied,
                    instruction,
                    index,
                    integer ? IntegerTypeBits : NumberTypeBits,
                    minimum: minimum
                );
            }
        }

        return copied;
    }

    private static int? ReadBoundedRecordUpdate(
        IReadOnlyList<BeamInstruction> body,
        int target,
        BeamOperand slot
    )
    {
        // The unique successor of the failed numerical comparison carries a
        // lower bound until its saved value is overwritten or control splits.
        for (int index = target + 1; index < body.Count; index++)
        {
            BeamInstruction instruction = body[index];
            switch (instruction.Definition.Opcode)
            {
                case Line:
                case TestHeap:
                    continue;
                case Move when !SameProjectionRegister(instruction.Operands[^1], slot):
                    continue;
                case GcBifTwo when instruction.Operands[0].Value.IsZero
                    && !SameProjectionRegister(instruction.Operands[^1], slot):
                    continue;
                case UpdateRecord:
                    for (int operand = FirstRecordUpdateValueOperand; operand < instruction.Operands.Count;
                        operand += RecordUpdateFieldPairWidth)
                    {
                        if (instruction.Operands[operand] == slot)
                        {
                            return index;
                        }
                    }

                    return null;
                default:
                    return null;
            }
        }

        return null;
    }

    private static void AdvanceNumericRecordValues(
        BeamInstruction instruction,
        HashSet<BeamOperand> values,
        HashSet<int> producers,
        bool inputWasTyped
    )
    {
        var operands = instruction.Operands;
        switch (instruction.Definition.Opcode)
        {
            case Move:
                bool fromNumber = values.Contains(operands[0]);
                BeamOperand destination = operands[1];
                values.Remove(destination with { TypeIndex = null });
                if (fromNumber && IsBareRecordValueRegister(destination))
                {
                    values.Add(destination);
                }
                break;
            case Allocate:
            case AllocateHeap:
                // A new frame has no saved Y values. GC preserves the live X prefix.
                int liveIndex = instruction.Definition.Opcode == Allocate ? 1 : 2;
                values.RemoveWhere(operand => operand.Kind != BeamOperandParameters.X
                    || operand.Value >= operands[liveIndex].Value);
                break;
            case TestHeap:
                RemoveUnrootedRecordValues(values, operands[1]);
                break;
            case InitYRegisters:
                foreach (BeamOperand slot in operands.Skip(1))
                {
                    values.Remove(slot with { TypeIndex = null });
                }
                break;
            case GcBifTwo:
                RemoveUnrootedRecordValues(values, operands[1]);
                BeamOperand result = operands[^1];
                values.Remove(result with { TypeIndex = null });
                if (!inputWasTyped && IsOrdinaryBinaryBif(instruction, producers)
                    && IsBareRecordValueRegister(result)
                    && IsPlainRecordArithmeticInput(operands[3]) && IsPlainRecordArithmeticInput(operands[4])
                    && (operands[3].Kind != BeamOperandParameters.Integer || operands[4].Kind != BeamOperandParameters.Integer))
                {
                    values.Add(result);
                }
                break;
            default:
                // No inference at joins, calls, comparisons or other unmodeled instructions.
                values.Clear();
                break;
        }
    }

    private static bool IsBareRecordValueRegister(BeamOperand operand) =>
        operand is { Kind: BeamOperandParameters.X or BeamOperandParameters.Y, TypeIndex: null };

    private static bool IsPlainRecordArithmeticInput(BeamOperand operand) =>
        operand.Kind == BeamOperandParameters.Integer || IsBareRecordValueRegister(operand);

    private static void RemoveUnrootedRecordValues(HashSet<BeamOperand> values, BeamOperand live) =>
        values.RemoveWhere(operand => operand.Kind == BeamOperandParameters.X && operand.Value >= live.Value);
}
