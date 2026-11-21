using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? SinkIndependentTupleReadsPastHeap(ReadOnlyMemory<byte> code, int labelCount) =>
        !ModuleOptimizationDisabled && SsaTypeInferenceEnabled && code.Span.Contains(TestHeap)
            && DecodeLateAtomicProjectionSlots(code, labelCount) is { } decoded
            ? MoveTerminalTupleReadsPastHeap(code, decoded.Body)
            : null;

    private static byte[]? MoveTerminalTupleReadsPastHeap(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        for (int heap = 1; heap + 2 < body.Count; heap++)
        {
            if (body[heap] is not { Definition.Opcode: TestHeap, Operands: [_, var live] }
                || body[heap - 1].Definition.Opcode != GetTupleElement)
            {
                continue;
            }

            int end = heap + 1;
            while (end < body.Count && body[end].Definition.Opcode is PutTuple or PutList)
            {
                end++;
            }

            if (end == heap + 1 || end == body.Count || body[end].Definition.Opcode != Return
                || !HasUnprotectedCheckedFailureRegion(body, heap, end))
            {
                continue;
            }

            int start = heap;
            while (start > 0 && IsHeapProjectionPrefix(body[start - 1].Definition.Opcode))
            {
                start--;
            }

            var reads = new List<int>();
            for (int index = start; index < heap; index++)
            {
                if (body[index] is not
                    {
                        Definition.Opcode: GetTupleElement,
                        Operands: [
                        { Kind: BeamOperandParameters.X, TypeIndex: null } source,
                            _,
                        { Kind: BeamOperandParameters.X, TypeIndex: null } destination
                        ]
                    }
                    || source.Value >= live.Value || destination.Value >= live.Value
                    || source != destination && !PreviousInstructionDefinesHeapRoot(
                        body,
                        start,
                        index,
                        destination
                    ))
                {
                    continue;
                }

                bool independent = true;
                for (int crossed = index + 1; crossed < heap; crossed++)
                {
                    var instruction = body[crossed];
                    if (instruction.Operands.Any(operand =>
                        SameProjectionRegister(operand, source) || SameProjectionRegister(operand, destination))
                        || IsHeapProjectionGuard(instruction.Definition.Opcode)
                            && !(flow ??= new BeamProjectionFlow(body)).IsGuardTemporaryDeadOnFailure(
                                crossed,
                                destination,
                                preserveHeapRoots: true
                            ))
                    {
                        independent = false;
                        break;
                    }
                }

                if (independent)
                {
                    reads.Add(index);
                }
            }

            if (reads.Count < 2 || reads[^1] != heap - 1)
            {
                continue;
            }

            foreach (int read in reads)
            {
                output.Write(code.Span[copied..body[read].Offset]);
                copied = body[read + 1].Offset;
            }

            int afterHeap = body[heap + 1].Offset;
            output.Write(code.Span[copied..afterHeap]);
            foreach (int read in reads)
            {
                output.Write(code.Span[body[read].Offset..body[read + 1].Offset]);
            }

            copied = afterHeap;
            heap = end;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static bool PreviousInstructionDefinesHeapRoot(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int projection,
        BeamOperand root
    )
    {
        // Every instruction in the block is a pure term read or writes a term.
        // A mention in its straight-line prefix proves the old root exists.
        for (int index = projection - 1; index >= start; index--)
        {
            if (body[index].Operands.Any(operand => SameProjectionRegister(operand, root)))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsHeapProjectionPrefix(byte opcode) =>
        opcode is GetTupleElement or GetList or GetHead or GetTail || IsHeapProjectionGuard(opcode);

    private static bool IsHeapProjectionGuard(byte opcode) =>
        opcode is IsTuple or TestArity or IsTaggedTuple or IsNonemptyList or IsNil or ExactEqualTest;
}
