using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? FoldTerminalCalls(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body)
    {
        var returnBlocks = FindSharedCallReturns(body);
        using var output = new MemoryStream();
        int copied = 0;
        for (int index = 0; index + 2 < body.Count; index++)
        {
            BeamInstruction instruction = body[index];
            if (instruction.Definition.Opcode is not (LocalCall or ExternalCall or DynamicApply))
            {
                continue;
            }

            int continuation = index + 1;
            int consumed = continuation;
            if (body[continuation] is { Definition.Opcode: Jump, Operands: [var target] })
            {
                if (!returnBlocks.TryGetValue((int)target.Value, out continuation))
                {
                    continue;
                }
            }

            bool framed = body[continuation].Definition.Opcode == Deallocate;
            int terminal = continuation + (framed ? 1 : 0);
            if (terminal + 1 >= body.Count || body[terminal].Definition.Opcode != Return)
            {
                continue;
            }

            byte opcode = instruction.Definition.Opcode switch
            {
                LocalCall => framed ? TailLocalCallWithFrame : TailLocalCall,
                ExternalCall => framed ? TailExternalCallWithFrame : TailExternalCall,
                DynamicApply when framed => TailDynamicApply,
                _ => 0
            };
            if (opcode == 0)
            {
                continue;
            }

            output.Write(code.Span[copied..instruction.Offset]);
            output.WriteByte(opcode);
            output.Write(code.Span[(instruction.Offset + 1)..body[index + 1].Offset]);
            if (framed)
            {
                output.Write(code.Span[(body[continuation].Offset + 1)..body[terminal].Offset]);
            }

            index = continuation == consumed ? terminal : consumed;
            copied = body[index + 1].Offset;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static byte[]? ExpandShortReturnJumps(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body)
    {
        var returns = FindSharedCallReturns(body);
        using var output = new MemoryStream();
        int copied = 0;
        for (int index = 0; index + 1 < body.Count; index++)
        {
            if (body[index] is not { Definition.Opcode: Jump, Operands: [var target] }
                || !returns.TryGetValue((int)target.Value, out int start))
            {
                continue;
            }

            int end = start + (body[start].Definition.Opcode == Deallocate ? 2 : 1);
            output.Write(code.Span[copied..body[index].Offset]);
            output.Write(code.Span[body[start].Offset..body[end].Offset]);
            copied = body[index + 1].Offset;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static Dictionary<int, int> FindSharedCallReturns(IReadOnlyList<BeamInstruction> body)
    {
        var returns = new Dictionary<int, int>();
        int start = 0;
        bool protectedFunction = false;
        for (int end = 0; end < body.Count; end++)
        {
            byte opcode = body[end].Definition.Opcode;
            if (opcode is CatchEnter or TryEnter)
            {
                protectedFunction = true;
            }

            if (opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            if (!protectedFunction)
            {
                for (int index = start + 1; index + 1 < end; index++)
                {
                    if (body[index] is { Definition.Opcode: Label, Operands: [var entry] }
                        && (body[index + 1].Definition.Opcode == Return
                            || body[index + 1].Definition.Opcode == Deallocate
                                && body[index + 2].Definition.Opcode == Return))
                    {
                        returns.Add((int)entry.Value, index + 1);
                    }
                }
            }

            start = end + 1;
            protectedFunction = false;
        }

        return returns;
    }
}
