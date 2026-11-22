using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ForwardOutgoingClosureRegisters(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(MakeFun)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        using var output = new MemoryStream();
        int copied = 0;
        for (int make = 0; make + 2 < body.Count; make++)
        {
            if (body[make] is not
                {
                    Definition.Opcode: MakeFun,
                    Operands: [_, { Kind: BeamOperandParameters.X, TypeIndex: null } temporary, ..]
                }
                || ReadOutgoingClosureCopy(body, make, temporary) is not { } copy)
            {
                continue;
            }

            // make_fun3 reads the complete capture vector before writing its
            // destination. The call's register can therefore also be a capture.
            int firstOperand = body[make].Offset + 1;
            var cursor = new BeamCursor(code[firstOperand..]);
            _ = BeamCompactReader.Read(cursor);
            int destination = firstOperand + cursor.Position;
            _ = BeamCompactReader.Read(cursor);
            output.Write(code.Span[copied..destination]);
            WriteRegister(output, body[copy].Operands[1]);
            output.Write(code.Span[(firstOperand + cursor.Position)..body[copy].Offset]);
            copied = body[copy + 1].Offset;
            make = copy;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static int? ReadOutgoingClosureCopy(
        IReadOnlyList<BeamInstruction> body,
        int make,
        BeamOperand temporary
    )
    {
        for (int copy = make + 1; copy < body.Count; copy++)
        {
            if (body[copy] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var source, { Kind: BeamOperandParameters.X, TypeIndex: null } outgoing]
                } || source != temporary)
            {
                if (!IsClosureArgumentSetup(body[copy]) || ReadsOrWrites(body[copy], temporary))
                {
                    return null;
                }

                continue;
            }

            if (outgoing == temporary)
            {
                return null;
            }

            for (int index = make + 1; index < copy; index++)
            {
                if (ReadsOrWrites(body[index], outgoing))
                {
                    return null;
                }
            }

            return IsClosureTemporaryDeadBeforeCall(
                body,
                copy + 1,
                temporary,
                outgoing
            ) ? copy : null;
        }

        return null;
    }

    private static bool IsClosureTemporaryDeadBeforeCall(
        IReadOnlyList<BeamInstruction> body,
        int start,
        BeamOperand temporary,
        BeamOperand outgoing
    )
    {
        bool overwritten = false;
        for (int index = start; index < body.Count; index++)
        {
            var instruction = body[index];
            if (instruction.Definition.Opcode is LocalCall or ExternalCall
                or TailLocalCall or TailExternalCall or TailLocalCallWithFrame or TailExternalCallWithFrame)
            {
                var arity = instruction.Operands[0].Value;

                return outgoing.Value < arity && (overwritten || temporary.Value >= arity);
            }

            if (!IsClosureArgumentSetup(instruction))
            {
                return false;
            }

            if (instruction.Definition.Opcode == Move)
            {
                var source = instruction.Operands[0];
                var destination = instruction.Operands[1];
                if (SameProjectionRegister(destination, outgoing)
                    || !overwritten && SameProjectionRegister(source, temporary))
                {
                    return false;
                }

                overwritten |= SameProjectionRegister(destination, temporary);
            }
        }

        return false;
    }

    private static bool ReadsOrWrites(BeamInstruction instruction, BeamOperand register) =>
        instruction.Definition.Opcode == Move
        && instruction.Operands.Any(operand => SameProjectionRegister(operand, register));

    private static bool IsClosureArgumentSetup(BeamInstruction instruction) =>
        instruction.Definition.Opcode is Line or InitYRegisters or TrimFrame
        || instruction is
        {
            Definition.Opcode: Move,
            Operands: [_, { Kind: BeamOperandParameters.X, TypeIndex: null }]
        };
}
