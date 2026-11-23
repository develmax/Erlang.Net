using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ReuseRetiredProjectionForStatusCleanup(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(TrimFrame)
            || !code.Span.Contains(IsTaggedTuple)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        return RewriteRetiredProjectionForStatusCleanup(code, body);
    }

    private static byte[]? RewriteRetiredProjectionForStatusCleanup(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
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
            (int[] Owners, Dictionary<int, bool[]> Uses)? frames = null;
            for (int open = start + 1; open + 15 < end; open++)
            {
                if (body[open].Definition.Opcode != ExternalCall
                    || body[open + 1] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [var raw, { Kind: BeamOperandParameters.Y, TypeIndex: null } retained]
                    } || raw != x0
                    || body[open + 2] is not
                    {
                        Definition.Opcode: IsTaggedTuple,
                        Operands: [_, var checkedTuple, var tupleArity, _]
                    } || checkedTuple != x0
                    || body[open + 3] is not
                    {
                        Definition.Opcode: GetTupleElement,
                        Operands: [var tuple, var field, { Kind: BeamOperandParameters.Y, TypeIndex: null } resource]
                    } || tuple != x0 || field.Value >= tupleArity.Value || resource == retained
                    || body[open + 4] is not
                    {
                        Definition.Opcode: GetTupleElement,
                        Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } carrier, _,
                        { Kind: BeamOperandParameters.X, TypeIndex: null, Value.IsOne: true }]
                    } || carrier == retained || carrier == resource
                    || !IsMove(open + 5, resource, x0))
                {
                    continue;
                }

                int action = AfterSourceLines(open + 6, end);
                if (action + 8 >= end || body[action] is not
                    { Definition.Opcode: LocalCall or ExternalCall, Operands: [{ Value: var arity }, _] }
                    || arity != 2 || !IsMove(action + 1, x0, carrier)
                    || body[action + 2] is not
                    {
                        Definition.Opcode: ExactEqualTest,
                        Operands: [var cold, var status, { Kind: BeamOperandParameters.Atom }]
                    } || status != x0 || !IsMove(action + 3, retained, x0)
                    || body[action + 4] is not
                    {
                        Definition.Opcode: Deallocate,
                        Operands: [var width]
                    } || width.Value != carrier.Value + 1
                    || body[action + 5].Definition.Opcode != Return
                    || body[action + 6] is not { Definition.Opcode: Label, Operands: [var target] }
                    || target.Value != cold.Value || !IsMove(action + 7, resource, x0)
                    || body[action + 8] is not
                    {
                        Definition.Opcode: TrimFrame,
                        Operands: [var discarded, { Value.IsOne: true }]
                    } || discarded.Value != carrier.Value)
                {
                    continue;
                }

                int cleanup = AfterSourceLines(action + 9, end);
                if (cleanup + 3 >= end || body[cleanup] is not
                    { Definition.Opcode: ExternalCall, Operands: [{ Value.IsOne: true }, _] }
                    || !IsMove(cleanup + 1, new BeamOperand(BeamOperandParameters.Y, 0), x0)
                    || body[cleanup + 2] is not { Definition.Opcode: Deallocate, Operands: [{ Value.IsOne: true }] }
                    || body[cleanup + 3].Definition.Opcode != Return)
                {
                    continue;
                }

                var branches = flow ??= new BeamProjectionFlow(body);
                if (!branches.TryGetSingleForwardTarget(action + 2, (int)cold.Value, out int failure)
                    || failure != action + 6
                    || (frames ??= ReadDisjointFrameOwnership(
                        body,
                        start,
                        end,
                        trackSiblingTrimEpochs: true
                    )) is not { } owned)
                {
                    continue;
                }

                int allocation = owned.Owners[open - start];
                if (allocation < 0 || owned.Owners[action - start] != allocation
                    || owned.Owners[action + 8 - start] != allocation
                    || owned.Owners[cleanup - start] != action + 8)
                {
                    continue;
                }

                // The second projection is the last read of the old carrier.
                // Reuse that slot for the resource, then preserve status in the
                // kept trim slot only after the resource leaves for cleanup.
                output.Write(code.Span[copied..body[open + 3].Offset]);
                output.Write(code.Span[body[open + 4].Offset..body[open + 5].Offset]);
                output.WriteByte(GetTupleElement);
                WriteRegister(output, x0);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, field.Value);
                WriteRegister(output, carrier);
                WriteMove(carrier, x0);
                output.Write(code.Span[body[open + 6].Offset..body[action + 1].Offset]);
                WriteMove(x0, resource);
                output.Write(code.Span[body[action + 2].Offset..body[action + 7].Offset]);
                WriteMove(carrier, x0);
                WriteMove(resource, carrier);
                copied = body[action + 8].Offset;
                open = cleanup + 3;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        bool IsMove(int index, BeamOperand source, BeamOperand destination) => body[index] is
        {
            Definition.Opcode: Move,
            Operands: [var read, var write]
        } && read == source && write == destination;

        int AfterSourceLines(int index, int end)
        {
            while (index < end && body[index].Definition.Opcode == Line)
            {
                index++;
            }

            return index;
        }

        void WriteMove(BeamOperand source, BeamOperand destination)
        {
            output.WriteByte(Move);
            WriteRegister(output, source);
            WriteRegister(output, destination);
        }
    }
}
