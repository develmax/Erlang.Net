using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ForwardTerminalCallProjections(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !code.Span.Contains(PutTuple)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        const int tupleArity = 3;
        const int tupleHeapWords = tupleArity + 1;
        const int resultHeapWords = 3;
        const int callArity = 4;
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var x1 = new BeamOperand(BeamOperandParameters.X, 1);
        var x2 = new BeamOperand(BeamOperandParameters.X, 2);
        var x3 = new BeamOperand(BeamOperandParameters.X, 3);
        var body = decoded.Body;
        BeamProjectionFlow? flow = null;
        using var output = new MemoryStream();
        int copied = 0;
        int entry = 0;
        int functionEnd = 0;
        (int[] Owners, Dictionary<int, bool[]> Uses)? ownership = null;
        for (int index = 0; index + 13 < body.Count; index++)
        {
            if (body[index].Definition.Opcode == FunctionInfo)
            {
                entry = index + 1;
                functionEnd = entry;
                while (body[functionEnd].Definition.Opcode is not (FunctionInfo or EndCode))
                {
                    functionEnd++;
                }

                ownership = null;
            }

            if (index + 13 >= functionEnd
                || body[index] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var source, _, { Kind: BeamOperandParameters.Y, TypeIndex: null } first]
                } || source != x0
                || body[index + 1] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } saved, _, var second]
                } || second != x0 || saved == first
                || !Moves(index + 2, x0, saved)
                || body[index + 3] is not
                {
                    Definition.Opcode: TestHeap,
                    Operands: [{ Value: var heapWords }, { Value.IsZero: true }]
                }
                || heapWords != tupleHeapWords || body[index + 4] is not
                {
                    Definition.Opcode: PutTuple,
                    Operands: [var tuple, _, { Kind: BeamOperandParameters.Atom } tag, var field,
                    { Kind: BeamOperandParameters.Atom } trailing]
                } || tuple != x0 || field != first
                || !Moves(index + 5, x0, x3)
                || !LoadsConstant(index + 6, x0) || !LoadsConstant(index + 7, x1)
                || !Moves(index + 8, saved, x2))
            {
                continue;
            }

            int call = index + 9;
            while (call < functionEnd && body[call].Definition.Opcode == Line)
            {
                call++;
            }

            if (call + 4 >= functionEnd
                || body[call] is not { Definition.Opcode: LocalCall or ExternalCall, Operands: [{ Value: var arity }, _] }
                || arity != callArity
                || body[call + 1] is not
                {
                    Definition.Opcode: TestHeap,
                    Operands: [{ Value: var resultWords }, { Value.IsOne: true }]
                }
                || resultWords != resultHeapWords || body[call + 2] is not
                {
                    Definition.Opcode: PutTuple,
                    Operands: [var result, _, { Kind: BeamOperandParameters.Atom }, var value]
                } || result != x0 || value != x0
                || body[call + 3] is not { Definition.Opcode: Deallocate, Operands: [{ Value: var frameWidth }] }
                || frameWidth <= 0
                || body[call + 4].Definition.Opcode != Return
                || (ownership ??= ReadDisjointFrameOwnership(body, entry, functionEnd)) is not { } frames
                || !frames.Uses.TryGetValue(frames.Owners[index - entry], out bool[]? slots) || frameWidth != slots.Length
                || !(flow ??= new BeamProjectionFlow(body)).IsInitialized(index, first))
            {
                continue;
            }

            // Only X0 is live at the reservation. The frame already roots the
            // saved tuple, and no stack value is needed after setting X2.
            output.Write(code.Span[copied..body[index].Offset]);
            output.WriteByte(TestHeap);
            Unsigned(tupleHeapWords);
            Unsigned(1);
            WriteProjection(index, first, x0);
            output.WriteByte(PutTuple);
            WriteRegister(output, x3);
            output.WriteByte(ExtendedListOperand);
            Unsigned(tupleArity);
            BeamCompactWriter.Write(output, AtomTag, tag.Value);
            WriteRegister(output, x0);
            BeamCompactWriter.Write(output, AtomTag, trailing.Value);
            WriteProjection(index + 1, x0, x2);
            output.Write(code.Span[body[index + 7].Offset..body[index + 8].Offset]);
            output.WriteByte(TrimFrame);
            Unsigned((int)frameWidth);
            Unsigned(0);
            output.Write(code.Span[body[index + 6].Offset..body[index + 7].Offset]);
            output.Write(code.Span[body[index + 9].Offset..body[call + 3].Offset]);
            output.WriteByte(Deallocate);
            Unsigned(0);
            copied = body[call + 4].Offset;
            index = call + 3;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        bool Moves(int index, BeamOperand source, BeamOperand destination) =>
            body[index] is { Definition.Opcode: Move, Operands: [var from, var to] }
            && from == source && to == destination;

        bool LoadsConstant(int index, BeamOperand destination) =>
            body[index] is
            {
                Definition.Opcode: Move,
                Operands: [
                {
                    Kind: BeamOperandParameters.Atom or BeamOperandParameters.Nil
                    or BeamOperandParameters.Integer or BeamOperandParameters.LiteralValue
                }, var to]
            } && to == destination;

        void WriteProjection(int position, BeamOperand original, BeamOperand replacement)
        {
            int after = body[position + 1].Offset;
            int width = CompactRegisterCountWidth((int)original.Value);
            output.Write(code.Span[body[position].Offset..(after - width)]);
            WriteRegister(output, replacement);
        }

        void Unsigned(int value) => BeamCompactWriter.Write(output, BeamWire.UnsignedTag, value);
    }
}
