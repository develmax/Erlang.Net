using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ForwardProjectedPairCallArguments(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(GetTupleElement)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        return RewriteProjectedPairCallSetup(code, body);
    }

    private static byte[]? RewriteProjectedPairCallSetup(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body)
    {
        using var output = new MemoryStream();
        var result = new BeamOperand(BeamOperandParameters.X, 0);
        bool unprotected = false;
        int copied = 0;
        for (int call = 0; call + 6 < body.Count; call++)
        {
            if (body[call].Definition.Opcode == FunctionInfo)
            {
                unprotected = HasUnprotectedCallKillSchedule(body, call + 1);
                continue;
            }

            if (!unprotected || body[call].Definition.Opcode is not (LocalCall or ExternalCall)
                || body[call + 1] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [
                        var firstInput,
                    { Kind: BeamOperandParameters.Unsigned } firstField,
                    { Kind: BeamOperandParameters.X, TypeIndex: null } argument
                    ]
                } || firstInput != result || argument == result
                || body[call + 2] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var secondInput, { Kind: BeamOperandParameters.Unsigned } secondField, var secondOutput]
                } || secondInput != result || secondOutput != result || firstField == secondField
                || body[call + 3] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var secondValue, { Kind: BeamOperandParameters.Y, TypeIndex: null } transit]
                } || secondValue != result
                || body[call + 4] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var firstValue, { Kind: BeamOperandParameters.Y, TypeIndex: null } retained]
                } || firstValue != argument || retained == transit
                || body[call + 5] is not { Definition.Opcode: Move, Operands: [var loaded, var into] }
                || loaded != transit || into != argument
                || !ReachesProjectedPairArgumentCall(
                    body,
                    call + 6,
                    result,
                    argument,
                    transit
                ))
            {
                continue;
            }

            // Both reads still see the exact returning tuple. The first field
            // survives the next call and the second goes into its argument.
            output.Write(code.Span[copied..body[call + 1].Offset]);
            WriteProjection(firstField, retained);
            WriteProjection(secondField, argument);
            copied = body[call + 6].Offset;
            call += 5;

            void WriteProjection(BeamOperand index, BeamOperand destination)
            {
                output.WriteByte(GetTupleElement);
                WriteRegister(output, result);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, index.Value);
                WriteRegister(output, destination);
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static bool ReachesProjectedPairArgumentCall(
        IReadOnlyList<BeamInstruction> body,
        int start,
        BeamOperand result,
        BeamOperand argument,
        BeamOperand transit
    )
    {
        bool overwritten = false;
        bool cleared = false;
        for (int index = start; index < body.Count; index++)
        {
            BeamInstruction instruction = body[index];
            switch (instruction.Definition.Opcode)
            {
                case Line:
                    break;
                case InitYRegisters:
                    cleared |= instruction.Operands.Any(item => SameProjectionRegister(item, transit));
                    break;
                case Move:
                    var source = instruction.Operands[0];
                    var destination = instruction.Operands[1];
                    if (!overwritten && SameProjectionRegister(source, result)
                        || SameProjectionRegister(source, transit) || SameProjectionRegister(destination, transit)
                        || SameProjectionRegister(destination, argument))
                    {
                        return false;
                    }

                    overwritten |= SameProjectionRegister(destination, result);
                    break;
                case LocalCall or ExternalCall:
                    return overwritten && cleared && instruction.Operands[0].Value > argument.Value;
                default:
                    return false;
            }
        }

        return false;
    }
}
