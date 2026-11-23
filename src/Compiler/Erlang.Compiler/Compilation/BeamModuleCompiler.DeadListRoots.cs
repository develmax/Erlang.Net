using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ClearConsumedListHeadBeforeRedefinition(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(PutList)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var body = decoded.Body;
        var result = new BeamOperand(BeamOperandParameters.X, 0);
        using var output = new MemoryStream();
        int copied = 0;
        int start = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            bool protectedFrame = Enumerable.Range(start, end - start).Any(index =>
                body[index].Definition.Opcode is TryEnter or CatchEnter);
            if (!protectedFrame)
            {
                ClearConsumedRoots(start, end);
            }

            start = end + 1;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        void ClearConsumedRoots(int first, int last)
        {
            for (int index = first; index + 4 < last; index++)
            {
                if (body[index] is not
                    {
                        Definition.Opcode: PutList,
                        Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } consumed, _, var temporary]
                    } || temporary != result
                    || body[index + 1] is not
                    {
                        Definition.Opcode: PutList,
                        Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } head, var rest, var spine]
                    } || rest != result || spine != head || head == consumed)
                {
                    continue;
                }

                int call = index + 2;
                int argument = -1;
                while (call + 1 < last)
                {
                    if (body[call].Definition.Opcode == Line)
                    {
                        call++;
                        continue;
                    }

                    if (body[call] is not
                        {
                            Definition.Opcode: Move,
                            Operands: [var literal, { Kind: BeamOperandParameters.X, TypeIndex: null } destination]
                        } || !IsNonRegisterValue(literal))
                    {
                        break;
                    }

                    if (destination == result)
                    {
                        argument = call;
                    }

                    call++;
                }

                if (argument < 0 || call + 1 >= last
                    || body[call].Definition.Opcode is not (LocalCall or ExternalCall)
                    || body[call + 1] is not { Definition.Opcode: Move, Operands: [var value, var slot] }
                    || value != result || slot != consumed)
                {
                    continue;
                }

                // The new list owns the consumed value. No argument reload refers
                // to its old slot, and the next call result immediately redefines it.
                int position = body[argument].Offset;
                output.Write(code.Span[copied..position]);
                WriteFrameInitialization(output, [(int)consumed.Value]);
                copied = position;
                index = call;
            }
        }
    }
}
