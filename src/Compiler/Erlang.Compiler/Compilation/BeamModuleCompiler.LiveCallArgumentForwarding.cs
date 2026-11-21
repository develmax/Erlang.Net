using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ForwardLiveCallResultToArgument(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled)
        {
            return null;
        }

        IReadOnlyList<BeamInstruction> body;
        try
        {
            body = BeamInstructionReader.Read(
                new BeamCodeHeader(
                    BeamWire.FormatVersion,
                    highestOpcode,
                    labelCount,
                    exports.Count + locals.Count,
                    code
                ),
                atoms.Count + 1,
                literalBytes.Count
            );
        }
        catch (Exception failure) when (failure is InvalidDataException or NotSupportedException)
        {
            return null;
        }

        using var result = new MemoryStream();
        int copied = 0;
        for (int index = 1; index + 3 < body.Count; index++)
        {
            if (body[index - 1].Definition.Opcode is not (LocalCall or ExternalCall)
                || body[index] is not
                {
                    Definition.Opcode: Move, Operands:
                    [
                    { Kind: BeamOperandParameters.X, Value.IsZero: true, TypeIndex: null } resultRegister,
                    { Kind: BeamOperandParameters.Y, TypeIndex: null } saved
                    ]
                } || body[index + 1] is not
                {
                    Definition.Opcode: Move, Operands:
                    [
                        var source,
                    { Kind: BeamOperandParameters.X, Value.Sign: > 0, TypeIndex: null } argument
                    ]
                } || source != saved || !IsOutgoingArgument(index + 2, argument.Value))
            {
                continue;
            }

            // The preceding call's result is still in X0. The copy to the owned frame
            // remains live, but argument setup can also use its original definition.
            result.Write(code.Span[copied..body[index + 1].Offset]);
            result.WriteByte(Move);
            WriteRegister(result, resultRegister);
            WriteRegister(result, argument);
            copied = body[index + 2].Offset;
            index++;
        }

        if (copied == 0)
        {
            return null;
        }

        result.Write(code.Span[copied..]);

        return result.ToArray();

        bool IsOutgoingArgument(int next, System.Numerics.BigInteger argument)
        {
            while (next < body.Count && (body[next].Definition.Opcode == Line
                || body[next] is { Definition.Opcode: Move, Operands: [_, { Kind: BeamOperandParameters.X } target] }
                    && target.Value != argument))
            {
                next++;
            }

            return next < body.Count && body[next].Definition.Opcode is LocalCall or ExternalCall
                && body[next].Operands[0].Value > argument;
        }
    }
}
