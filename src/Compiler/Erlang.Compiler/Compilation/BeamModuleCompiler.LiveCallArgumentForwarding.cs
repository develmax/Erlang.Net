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
                })
            {
                continue;
            }

            if (ReadRetainedX0Reload(index + 1, saved, resultRegister) is int reload)
            {
                // Preparing the other arguments leaves X0 untouched. Keep
                // the Y copy for later uses and discard only this reload.
                result.Write(code.Span[copied..body[reload].Offset]);
                copied = body[reload + 1].Offset;
                index = reload;
                continue;
            }

            if (body[index + 1] is not
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

        int? ReadRetainedX0Reload(int first, BeamOperand saved, BeamOperand resultRegister)
        {
            int highestArgument = 0;
            int position = first;
            for (; position < body.Count; position++)
            {
                if (body[position] is
                    {
                        Definition.Opcode: Move,
                        Operands: [
                        {
                            Kind: BeamOperandParameters.Atom or BeamOperandParameters.Integer
                        or BeamOperandParameters.Nil
                        },
                        { Kind: BeamOperandParameters.X, Value.Sign: > 0, TypeIndex: null } argument]
                    })
                {
                    highestArgument = Math.Max(highestArgument, (int)argument.Value);
                }
                else if (body[position].Definition.Opcode != Line)
                {
                    break;
                }
            }

            if (highestArgument == 0 || position + 1 >= body.Count
                || !IsMove(body[position], saved, resultRegister))
            {
                return null;
            }

            int call = position + 1;
            while (call < body.Count && body[call].Definition.Opcode == Line)
            {
                call++;
            }

            return call < body.Count && body[call].Definition.Opcode is LocalCall or ExternalCall
                && body[call].Operands[0].Value > highestArgument ? position : null;
        }

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
