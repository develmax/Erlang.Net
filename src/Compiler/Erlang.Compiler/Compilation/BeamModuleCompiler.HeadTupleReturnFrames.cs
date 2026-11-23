using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? FoldHeadTupleReturnFrames(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(GetHead)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var body = decoded.Body;
        using var output = new MemoryStream();
        int copied = 0;
        for (int info = 0; info + 10 < body.Count; info++)
        {
            if (body[info] is not { Definition.Opcode: FunctionInfo, Operands: [_, _, var arity] }
                || body[info + 1].Definition.Opcode != Label)
            {
                continue;
            }

            int entry = info + 2;
            bool framed = body[entry].Definition.Opcode == Allocate;
            var live = arity.Value;
            if (framed)
            {
                if (body[entry].Operands is not [{ Value.IsZero: true }, var entryLive]
                    || entryLive.Value > live)
                {
                    continue;
                }

                live = entryLive.Value;
            }

            int test = framed ? entry + 1 : entry;
            if (body[test] is not
                {
                    Definition.Opcode: ExactEqualTest,
                    Operands: [var failure, { Kind: BeamOperandParameters.X, TypeIndex: null } tested,
                    { Kind: BeamOperandParameters.Atom }]
                }
                || tested.Value >= live)
            {
                continue;
            }

            int position = SkipLines(test + 1);
            int returnIndex = position + (framed ? 4 : 3);
            if (returnIndex + 2 >= body.Count
                || body[position] is not
                {
                    Definition.Opcode: GetHead,
                    Operands: [{ Kind: BeamOperandParameters.X, TypeIndex: null } source,
                    { Kind: BeamOperandParameters.X, Value.IsZero: true, TypeIndex: null } head]
                }
                || source.Value >= live
                || body[position + 1] is not { Definition.Opcode: TestHeap, Operands: [{ Value: var words }, { Value.IsOne: true }] }
                || body[position + 2] is not
                {
                    Definition.Opcode: PutTuple,
                    Operands: [var result, { Value: var fields }, { Kind: BeamOperandParameters.Atom }, var value]
                }
                || fields != 2 || words != fields + 1 || result != head || value != head
                || framed && body[returnIndex - 1] is not { Definition.Opcode: Deallocate, Operands: [{ Value.IsZero: true }] }
                || body[returnIndex].Definition.Opcode != Return
                || body[returnIndex + 1] is not { Definition.Opcode: Label, Operands: [var fallback] }
                || fallback.Value != failure.Value)
            {
                continue;
            }

            int tail = SkipLines(returnIndex + 2);
            if (tail < body.Count && body[tail] is
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.X, TypeIndex: null } arg, var argument]
                }
                && arg.Value < live && argument == head)
            {
                tail = SkipLines(tail + 1);
            }

            if (tail + 1 >= body.Count || !EndsFunction(tail + 1))
            {
                continue;
            }

            BeamInstruction tailCall = body[tail];
            bool matchingTail = framed
                ? tailCall is { Definition.Opcode: TailLocalCallWithFrame, Operands: [var outgoing, _, { Value.IsZero: true }] }
                    && outgoing.Value <= live
                : tailCall is { Definition.Opcode: TailLocalCall, Operands: [var outgoingFrameless, _] }
                    && outgoingFrameless.Value <= live;
            if (!matchingTail)
            {
                continue;
            }

            int next = body[tail + 1].Offset;
            int headOffset = body[position].Offset;
            if (framed)
            {
                output.Write(code.Span[copied..body[entry].Offset]);
                output.Write(code.Span[body[test].Offset..headOffset]);
            }
            else
            {
                // The earlier leaf allocator may already have removed the zero-slot frame.
                output.Write(code.Span[copied..headOffset]);
            }

            output.WriteByte(TestHeap);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, words);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, source.Value + 1);
            output.Write(code.Span[headOffset..body[position + 1].Offset]);
            if (framed)
            {
                output.Write(code.Span[body[position + 2].Offset..body[returnIndex - 1].Offset]);
                output.Write(code.Span[body[returnIndex].Offset..tailCall.Offset]);
                output.WriteByte(TailLocalCall);
                output.Write(code.Span[(tailCall.Offset + 1)..(next - CompactRegisterCountWidth(0))]);
            }
            else
            {
                output.Write(code.Span[body[position + 2].Offset..next]);
            }

            copied = next;
            info = tail;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        int SkipLines(int index)
        {
            while (index < body.Count && body[index].Definition.Opcode == Line)
            {
                index++;
            }

            return index;
        }

        bool EndsFunction(int index)
        {
            if (body[index].Definition.Opcode == EndCode)
            {
                return true;
            }

            if (body[index].Definition.Opcode != Label)
            {
                return false;
            }

            index = SkipLines(index + 1);

            return index < body.Count && body[index].Definition.Opcode == FunctionInfo;
        }
    }
}
