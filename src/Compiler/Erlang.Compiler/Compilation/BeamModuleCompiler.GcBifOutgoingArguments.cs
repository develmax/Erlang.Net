using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ForwardGcBifCallArguments(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled
            || !code.Span.Contains(GcBifOne) && !code.Span.Contains(GcBifTwo) && !code.Span.Contains(GcBifThree)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        using var output = new MemoryStream();
        int copied = 0;
        int start = -1;
        bool? unprotected = null;
        for (int index = 0; index + 2 < body.Count; index++)
        {
            BeamInstruction instruction = body[index];
            if (instruction.Definition.Opcode == FunctionInfo)
            {
                start = index + 1;
                unprotected = null;
            }

            if (start < 0 || instruction.Definition.Opcode is not (GcBifOne or GcBifTwo or GcBifThree)
                || instruction.Operands[0] is not { Kind: BeamOperandParameters.Failure, Value.IsZero: true }
                || instruction.Operands[^1] is not { Kind: BeamOperandParameters.X, TypeIndex: null } temporary
                || body[index + 1] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var copiedValue, { Kind: BeamOperandParameters.X, TypeIndex: null } destination]
                } || copiedValue != temporary || destination == temporary
                || !(unprotected ??= HasUnprotectedCallKillSchedule(body, start))
                || !ReachesGcBifCallArgument(
                    body,
                    index + 2,
                    temporary,
                    destination
                ))
            {
                continue;
            }

            // The collecting instruction observes the same incoming X/Y roots.
            // Its result overwrites only the old copy's destination.
            int target = body[index + 1].Offset - CompactRegisterCountWidth((int)temporary.Value);
            output.Write(code.Span[copied..target]);
            WriteRegister(output, destination);
            copied = body[index + 2].Offset;
            index++;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static bool ReachesGcBifCallArgument(
        IReadOnlyList<BeamInstruction> body,
        int start,
        BeamOperand temporary,
        BeamOperand destination
    )
    {
        bool overwritten = false;
        for (int index = start; index < body.Count; index++)
        {
            BeamInstruction instruction = body[index];
            if (instruction.Definition.Opcode is Line or InitYRegisters)
            {
                continue;
            }

            if (ReadFixedOutgoingCallArity(instruction) is { } arity)
            {
                return destination.Value < arity && (overwritten || temporary.Value >= arity);
            }

            if (instruction is not { Definition.Opcode: Move, Operands: [var source, var target] }
                || SameProjectionRegister(source, destination) || SameProjectionRegister(target, destination)
                || !overwritten && SameProjectionRegister(source, temporary))
            {
                return false;
            }

            overwritten |= SameProjectionRegister(target, temporary);
        }

        return false;
    }
}
