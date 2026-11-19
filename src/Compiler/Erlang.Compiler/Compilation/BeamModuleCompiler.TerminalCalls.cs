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

            int continuation = FindTerminalCallReturn(body, index, returnBlocks);
            if (continuation < 0)
            {
                continue;
            }

            int consumed = index + 1;
            bool framed = body[continuation].Definition.Opcode == Deallocate;
            int terminal = continuation + (framed ? 1 : 0);

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

    private static int FindTerminalCallReturn(
        IReadOnlyList<BeamInstruction> body,
        int call,
        Dictionary<int, int> returnBlocks
    )
    {
        int continuation = call + 1;
        if (continuation >= body.Count
            || body[continuation] is { Definition.Opcode: Jump, Operands: [var target] }
                && !returnBlocks.TryGetValue((int)target.Value, out continuation))
        {
            return -1;
        }

        int terminal = continuation + (body[continuation].Definition.Opcode == Deallocate ? 1 : 0);

        return terminal + 1 < body.Count && body[terminal].Definition.Opcode == Return ? continuation : -1;
    }

    private static byte[]? NormalizeTailCallFrames(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        string[] atomNames,
        (int Module, int Function, int Arity)[] importsByIndex
    )
    {
        using var output = new MemoryStream();
        int copied = 0;
        for (int frame = 0; frame + 1 < body.Count; frame++)
        {
            var allocation = body[frame];
            if (allocation.Definition.Opcode == TailExternalCall
                && IsTrappingTailBif(importsByIndex[(int)allocation.Operands[1].Value], atomNames))
            {
                int line = frame;
                while (line > 0 && body[line - 1].Offset >= copied && body[line - 1].Definition.Opcode == Line)
                {
                    line--;
                }

                int heap = line - 1;
                while (heap >= 0 && body[heap].Offset >= copied && body[heap].Definition.Opcode != TestHeap
                    && IsTailArgumentSetup(body[heap]))
                {
                    heap--;
                }

                if (heap >= 0 && body[heap].Offset >= copied && body[heap].Definition.Opcode == TestHeap)
                {
                    int heapOffset = body[heap].Offset;
                    output.Write(code.Span[copied..heapOffset]);
                    output.WriteByte(AllocateHeap);
                    BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 0);
                    output.Write(code.Span[(heapOffset + 1)..allocation.Offset]);
                }
                else
                {
                    int start = body[line].Offset;
                    output.Write(code.Span[copied..start]);
                    output.WriteByte(Allocate);
                    BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 0);
                    BeamCompactWriter.Write(output, BeamWire.UnsignedTag, allocation.Operands[0].Value);
                    output.Write(code.Span[start..allocation.Offset]);
                }

                output.WriteByte(TailExternalCallWithFrame);
                copied = body[frame + 1].Offset;
                output.Write(code.Span[(allocation.Offset + 1)..copied]);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 0);
                continue;
            }

            if (allocation.Definition.Opcode is not (Allocate or AllocateHeap) || !allocation.Operands[0].Value.IsZero)
            {
                continue;
            }

            int tail = frame + 1;
            while (tail + 1 < body.Count && IsTailArgumentSetup(body[tail]))
            {
                tail++;
            }

            if (tail + 1 == body.Count)
            {
                continue;
            }

            var call = body[tail];
            byte opcode = call.Definition.Opcode switch
            {
                TailLocalCallWithFrame => TailLocalCall,
                TailExternalCallWithFrame => TailExternalCall,
                _ => 0
            };
            if (opcode == 0 || !call.Operands[^1].Value.IsZero)
            {
                continue;
            }

            if (opcode == TailExternalCall
                && IsTrappingTailBif(importsByIndex[(int)call.Operands[1].Value], atomNames))
            {
                continue;
            }

            int setup = body[frame + 1].Offset;
            output.Write(code.Span[copied..allocation.Offset]);
            if (allocation.Definition.Opcode == AllocateHeap)
            {
                output.WriteByte(TestHeap);
                // Skip the opcode and the compact unsigned zero stack size.
                output.Write(code.Span[(allocation.Offset + 2)..setup]);
            }

            output.Write(code.Span[setup..call.Offset]);
            output.WriteByte(opcode);
            copied = body[tail + 1].Offset;
            output.Write(code.Span[(call.Offset + 1)..(copied - 1)]);
            frame = tail;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        static bool IsTailArgumentSetup(BeamInstruction instruction) =>
            (instruction.Definition.Opcode is Line or TestHeap or Move or Swap or PutTuple or PutList
                or GetTupleElement or GetList or GetHead or GetTail)
            && instruction.Operands.All(operand => operand.Kind != BeamOperandParameters.Y);
    }

    private static bool IsTrappingTailBif((int Module, int Function, int Arity) import, string[] atomNames) =>
        IsTrappingTailBif(atomNames[import.Module - 1], atomNames[import.Function - 1], import.Arity);

    private static bool IsTrappingTailBif(string module, string function, int arity) =>
        module == ErlangModuleNames.Module && (function, arity) is
            (ErlangModuleNames.Link or ErlangModuleNames.Unlink, 1)
            or (ErlangOperators.Send or AutoImportNames.MonitorNode or AutoImportNames.GroupLeader or ErlangModuleNames.Exit, 2);

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
