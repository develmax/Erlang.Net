using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? DirectTerminalCallResultArguments(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(PutList)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var x1 = new BeamOperand(BeamOperandParameters.X, 1);
        var x2 = new BeamOperand(BeamOperandParameters.X, 2);
        var body = decoded.Body;
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        int activeEnd = 0;
        int closedAt = 0;
        int catchEnter = -1;
        int catchEnd = -1;
        for (int index = 1; index + 14 < body.Count; index++)
        {
            if (body[index].Definition.Opcode == FunctionInfo)
            {
                activeEnd = index + 1;
                while (body[activeEnd].Definition.Opcode is not (FunctionInfo or EndCode))
                {
                    activeEnd++;
                }

                (closedAt, catchEnter, catchEnd) = ReadClosedCatch(index + 1, activeEnd);
            }

            if (index < closedAt || index + 13 >= activeEnd
                || body[index - 1].Definition.Opcode is not (LocalCall or ExternalCall)
                || body[index] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var incoming, { Kind: BeamOperandParameters.Y, TypeIndex: null } temporary]
                } || incoming != x0
                || !LoadsStack(index + 1, x1, temporary)
                || !Moves(index + 2, temporary, x2)
                || !LoadsStack(index + 3, x0, temporary))
            {
                continue;
            }

            int call = SkipLines(index + 4);
            if (call + 7 >= activeEnd
                || body[call] is not { Definition.Opcode: LocalCall or ExternalCall, Operands: [var arity, _] }
                || arity.Value != 3 || !Moves(call + 1, x0, temporary)
                || body[call + 2] is not { Definition.Opcode: TestHeap, Operands: [var heap, { Value.IsZero: true }] }
                || body[call + 3] is not
                {
                    Definition.Opcode: PutTuple,
                    Operands: [var tagged, _, { Kind: BeamOperandParameters.Atom } tag, var wrapped]
                } || tagged != x0 || wrapped != temporary || !Moves(call + 4, x0, temporary))
            {
                continue;
            }

            int firstList = call + 5;
            int list = firstList;
            while (list < activeEnd && body[list] is { Definition.Opcode: PutList, Operands: [var head, var tail, var to] }
                && head != temporary && head.Kind == BeamOperandParameters.Y && head.TypeIndex is null
                && tail == (list == firstList ? new BeamOperand(BeamOperandParameters.Nil, NilAtomIndex) : x0)
                && to == x0)
            {
                list++;
            }

            if (list == firstList || list + 3 >= activeEnd
                || body[list] is not
                {
                    Definition.Opcode: PutList,
                    Operands: [{ Kind: BeamOperandParameters.Atom } prefix, var previous, var listTarget]
                } || previous != x0 || listTarget != x2
                || body[list + 1] is not
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.Atom }, var moduleTarget]
                } || moduleTarget != x1 || !Moves(list + 2, temporary, x0))
            {
                continue;
            }

            int tailCall = SkipLines(list + 3);
            if (tailCall >= activeEnd || body[tailCall] is not
                {
                    Definition.Opcode: TailExternalCallWithFrame or TailLocalCallWithFrame,
                    Operands: [var outgoingArity, _, _]
                } || outgoingArity.Value != 3
                || !(flow ??= new BeamProjectionFlow(body)).IsInitialized(
                    index,
                    temporary,
                    catchEnter,
                    catchEnd
                ))
            {
                continue;
            }

            // The scalar results do not outlive the next call or the tail
            // arguments. Preserve the tuple in X0 while building the list in X1.
            output.Write(code.Span[copied..body[index].Offset]);
            Copy(index + 1, index + 2);
            output.WriteByte(Move);
            WriteRegister(output, x0);
            WriteRegister(output, x2);
            Copy(index + 3, call + 1);
            output.WriteByte(TestHeap);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, heap.Value);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
            output.WriteByte(PutTuple);
            WriteRegister(output, x0);
            output.WriteByte(ExtendedListOperand);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 2);
            BeamCompactWriter.Write(output, AtomTag, tag.Value);
            WriteRegister(output, x0);
            for (int cons = firstList; cons < list; cons++)
            {
                output.WriteByte(PutList);
                WriteRegister(output, body[cons].Operands[0]);
                if (cons == firstList)
                {
                    BeamCompactWriter.Write(output, AtomTag, NilAtomIndex);
                }
                else
                {
                    WriteRegister(output, x1);
                }

                WriteRegister(output, x1);
            }

            output.WriteByte(PutList);
            BeamCompactWriter.Write(output, AtomTag, prefix.Value);
            WriteRegister(output, x1);
            WriteRegister(output, x2);
            Copy(list + 1, list + 2);
            copied = body[list + 3].Offset;
            index = tailCall;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        int SkipLines(int next)
        {
            while (next < activeEnd && body[next].Definition.Opcode == Line)
            {
                next++;
            }

            return next;
        }

        bool Moves(int at, BeamOperand source, BeamOperand dest) => body[at] is
        { Definition.Opcode: Move, Operands: [var from, var to] } && from == source && to == dest;

        bool LoadsStack(int at, BeamOperand dest, BeamOperand temporary) => body[at] is
        { Definition.Opcode: Move, Operands: [{ Kind: BeamOperandParameters.Y } from, var to] }
            && !SameProjectionRegister(from, temporary) && to == dest;

        void Copy(int start, int end) => output.Write(code.Span[body[start].Offset..body[end].Offset]);

        (int ClosedAt, int Enter, int End) ReadClosedCatch(int start, int end)
        {
            int next = start;
            int enter = -1;
            for (int at = start; at < end; at++)
            {
                if (body[at].Definition.Opcode is TryEnter or DebugLine or ExecutableLine)
                {
                    return (end, -1, -1);
                }

                if (body[at] is not { Definition.Opcode: CatchEnter, Operands: [var handler, var target] })
                {
                    continue;
                }

                int call = at + 1;
                while (call < end && body[call].Definition.Opcode == Line)
                {
                    call++;
                }

                if (next != start || call + 2 >= end
                    || body[call].Definition.Opcode is not (LocalCall or ExternalCall)
                    || body[call + 1] is not { Definition.Opcode: Label, Operands: [var landing] }
                    || landing.Value != target.Value
                    || body[call + 2] is not { Definition.Opcode: CatchEnd, Operands: [var released] }
                    || released != handler)
                {
                    return (end, -1, -1);
                }

                enter = at;
                next = call + 3;
            }

            return (next, enter, enter == -1 ? -1 : next - 1);
        }
    }
}
