using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? TrimTerminalCallbackCons(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(PutList)
            || !code.Span.Contains(MakeFun)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var x1 = new BeamOperand(BeamOperandParameters.X, 1);
        var low = new BeamOperand(BeamOperandParameters.Y, 0);
        var high = new BeamOperand(BeamOperandParameters.Y, 1);
        using var output = new MemoryStream(code.Length);
        int copied = 0;
        int entry = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = end + 1;
            if (start + 12 >= end || body[start] is not { Definition.Opcode: Label, Operands: [var function] })
            {
                continue;
            }

            (int[] Owners, Dictionary<int, bool[]> Uses)? ownership = null;
            for (int call = start + 5; call + 4 < end; call++)
            {
                if (body[call] is not
                    {
                        Definition.Opcode: LocalCall,
                        Operands: [{ Value: var arity }, var target]
                    } || arity != 2 || target.Value != function.Value
                    || body[call + 1] is not
                    {
                        Definition.Opcode: TestHeap, Operands: [{ Value: var words }, { Value.IsOne: true }]
                    } || words != ConsHeapWords
                    || body[call + 2] is not { Definition.Opcode: PutList, Operands: [var kept, var tail, var result] }
                    || kept != low && kept != high || tail != x0 || result != x0
                    || body[call + 3] is not { Definition.Opcode: Deallocate, Operands: [{ Value: var width }] }
                    || width != 2 || body[call + 4].Definition.Opcode != Return)
                {
                    continue;
                }

                int site = call;
                while (site > start + 3 && body[site - 1].Definition.Opcode == Line)
                {
                    site--;
                }

                BeamOperand outgoing = kept == low ? high : low;
                if (!IsMove(body[site - 1], outgoing, x0) || !IsMove(body[site - 2], x0, x1)
                    || body[site - 3] is not
                    { Definition.Opcode: LocalCall or ExternalCall, Operands: [{ Value: var priorArity }, _] }
                    || priorArity != 2
                    || (ownership ??= ReadDisjointFrameOwnership(
                        body,
                        start,
                        end,
                        trackSiblingTrimEpochs: true
                    )) is not { Owners: var owners, Uses: var frames } || frames.Count != 1)
                {
                    continue;
                }

                int allocation = owners[site - 1 - start];
                if (allocation < 0 || frames[allocation].Length != 2
                    || Enumerable.Range(site - 3, call + 4 - (site - 3))
                        .Any(index => owners[index - start] != allocation)
                    || !Enumerable.Range(allocation + 1, site - 3 - allocation - 1)
                        .Any(index => body[index] is { Definition.Opcode: MakeFun, Operands: [_, _, { Value.IsZero: true }] }))
                {
                    continue;
                }

                // The outgoing root is already in X0 and the cons needs
                // precisely the other Y. Release the dead prefix before the call.
                output.Write(code.Span[copied..body[site].Offset]);
                if (kept == low)
                {
                    output.WriteByte(Move);
                    WriteRegister(output, low);
                    WriteRegister(output, high);
                }

                output.WriteByte(TrimFrame);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
                output.Write(code.Span[body[site].Offset..body[call + 2].Offset]);
                output.WriteByte(PutList);
                WriteRegister(output, low);
                WriteRegister(output, x0);
                WriteRegister(output, x0);
                output.WriteByte(Deallocate);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
                copied = body[call + 4].Offset;
                break;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        highestOpcode = Math.Max(highestOpcode, TrimFrame);
        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
