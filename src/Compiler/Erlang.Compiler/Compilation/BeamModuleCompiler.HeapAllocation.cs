using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? CombineConstructionHeapChecks(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        using var output = new MemoryStream();
        int copied = 0;
        for (int start = 0; start + 2 < body.Count; start++)
        {
            BeamInstruction first = body[start];
            if (first.Definition.Opcode is not (TestHeap or AllocateHeap)
                || ReadHeapWords(code, first) is not { } allocation)
            {
                continue;
            }

            int total = allocation.Words;
            bool constructed = false;
            List<int>? checks = null;
            for (int index = start + 1; index + 1 < body.Count; index++)
            {
                BeamInstruction instruction = body[index];
                byte opcode = instruction.Definition.Opcode;
                if (opcode is PutTuple or PutList or UpdateRecord)
                {
                    constructed = true;
                }
                else if (opcode == TestHeap && constructed && ReadHeapWords(code, instruction) is { } next
                    && total <= int.MaxValue - next.Words)
                {
                    total += next.Words;
                    constructed = false;
                    (checks ??= []).Add(index);
                }
                else if (opcode is not (Move or Swap or Line or InitYRegisters))
                {
                    break;
                }
            }

            if (checks is null)
            {
                continue;
            }

            // No instruction can collect or enter this sequence between the checks.
            // The initial X roots still suffice, and the constructors define the later values.
            output.Write(code.Span[copied..allocation.Start]);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, total);
            copied = allocation.End;
            foreach (int redundant in checks)
            {
                output.Write(code.Span[copied..body[redundant].Offset]);
                copied = body[redundant + 1].Offset;
            }

            start = checks[^1];
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static (int Start, int End, int Words)? ReadHeapWords(ReadOnlyMemory<byte> code, BeamInstruction instruction)
    {
        int operandStart = instruction.Offset + 1;
        var cursor = new BeamCursor(code[operandStart..]);
        if (instruction.Definition.Opcode == AllocateHeap)
        {
            _ = BeamCompactReader.Read(cursor);
        }

        int start = operandStart + cursor.Position;
        BeamCompactValue operand = BeamCompactReader.Read(cursor);

        return operand.WireTag == BeamWire.UnsignedTag && operand.Value > 0 && operand.Value <= int.MaxValue
            ? (start, operandStart + cursor.Position, (int)operand.Value) : null;
    }

    private byte[]? CombineProjectedDictionaryHeapAllocation(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(BifOne)
            || !atoms.TryGetValue(ErlangModuleNames.Module, out int erlang)
            || !atoms.TryGetValue(AutoImportNames.Get, out int get)
            || !imports.TryGetValue((erlang, get, 1), out int import)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        return CombineStackAndHeapAllocationCore(
            code,
            decoded.Body,
            emittedImports?[import] ?? import,
            projectedDictionaryOnly: true
        );
    }

    private static byte[]? CombineStackAndHeapAllocation(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        int dictionaryLookupImport
    ) => CombineStackAndHeapAllocationCore(
        code,
        body,
        dictionaryLookupImport,
        projectedDictionaryOnly: false
    );

    private static byte[]? CombineStackAndHeapAllocationCore(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        int dictionaryLookupImport,
        bool projectedDictionaryOnly
    )
    {
        MemoryStream? output = null;
        BeamProjectionFlow? flow = null;
        int copied = 0;
        for (int start = 0; start + 2 < body.Count; start++)
        {
            if (body[start] is not { Definition.Opcode: Allocate, Operands: [var frame, var live] } allocation)
            {
                continue;
            }

            int end = start + 1;
            while (body[end].Definition.Opcode is Move or InitYRegisters)
            {
                end++;
            }

            if (end < body.Count && body[end].Definition.Opcode == GetTupleElement
                && ReadProjectedHeapCheck(
                    start,
                    end,
                    frame,
                    live
                ) is int projectedHeap)
            {
                // The source tuples stay reachable from the pre-allocation X
                // roots. The projections still initialize their Y homes in order.
                end = projectedHeap;
            }
            else if (projectedDictionaryOnly)
            {
                continue;
            }
            else if (dictionaryLookupImport >= 0 && frame.Value.Sign > 0 && end + 2 < body.Count
                && body[end] is
                {
                    Definition.Opcode: BifOne,
                    Operands:
                    [
                    { Kind: BeamOperandParameters.Failure, Value.IsZero: true },
                    { Kind: BeamOperandParameters.Unsigned } import,
                    { Kind: BeamOperandParameters.Atom },
                    { Kind: BeamOperandParameters.X, Value.IsZero: true, TypeIndex: null }
                    ]
                }
                && import.Value == dictionaryLookupImport
                && body[end + 1] is
                {
                    Definition.Opcode: TestHeap,
                    Operands: [_, { Kind: BeamOperandParameters.Unsigned, Value.IsOne: true }]
                }
                && body[end + 2].Definition.Opcode is PutTuple or PutList
                && HasSavedAllocationInputs(
                    start,
                    end,
                    frame,
                    live
                ))
            {
                // The lookup cannot collect. Its result stays reachable through
                // the process dictionary until the post-allocation X0 definition.
                end++;
            }

            if (end + 1 >= body.Count
                || body[end] is not { Definition.Opcode: TestHeap, Operands: [var heap, _] } heapCheck
                || heap.Kind != BeamOperandParameters.Unsigned || heap.Value.Sign <= 0)
            {
                continue;
            }

            output ??= new MemoryStream(code.Length);
            output.Write(code.Span[copied..allocation.Offset]);
            output.WriteByte(AllocateHeap);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, frame.Value);
            // The decoded total does not retain the split between words, floats and funs.
            int heapOperandEnd = body[end + 1].Offset - CompactRegisterCountWidth((int)heapCheck.Operands[^1].Value);
            output.Write(code.Span[(heapCheck.Offset + 1)..heapOperandEnd]);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, live.Value);
            output.Write(code.Span[body[start + 1].Offset..heapCheck.Offset]);
            copied = body[end + 1].Offset;
            start = end;
        }

        if (output is null)
        {
            return null;
        }

        using (output)
        {
            output.Write(code.Span[copied..]);

            return output.ToArray();
        }

        int? ReadProjectedHeapCheck(
            int allocation,
            int first,
            BeamOperand frame,
            BeamOperand live
        )
        {
            if (!HasSavedAllocationInputs(
                allocation,
                first,
                frame,
                live
            ))
            {
                return null;
            }

            var saved = new Dictionary<int, BeamOperand>();
            for (int index = allocation + 1; index < first; index++)
            {
                BeamInstruction instruction = body[index];
                if (instruction.Definition.Opcode == InitYRegisters)
                {
                    foreach (BeamOperand slot in instruction.Operands.Skip(1))
                    {
                        saved.Remove((int)slot.Value);
                    }
                }
                else
                {
                    saved[(int)instruction.Operands[1].Value] = instruction.Operands[0];
                }
            }

            var destinations = new HashSet<int>();
            int next = first;
            while (next < body.Count && body[next].Definition.Opcode == GetTupleElement)
            {
                if (body[next] is not
                    {
                        Operands:
                        [
                            var source,
                        { Kind: BeamOperandParameters.Unsigned } field,
                        { Kind: BeamOperandParameters.Y } destination
                        ]
                    }
                    || destination.Value >= frame.Value || !destinations.Add((int)destination.Value))
                {
                    return null;
                }

                BeamOperand? tuple = source.Kind == BeamOperandParameters.Y
                    ? saved.GetValueOrDefault((int)source.Value) : source;
                if (tuple is not { Kind: BeamOperandParameters.X } || tuple.Value >= live.Value
                    || !(flow ??= new BeamProjectionFlow(body)).HasCheckedTupleField(allocation, tuple, field))
                {
                    return null;
                }

                saved.Remove((int)destination.Value);
                next++;
            }

            bool lookup = dictionaryLookupImport >= 0 && allocation > 0 && next < body.Count
                && body[allocation - 1].Definition.Opcode == IsTaggedTuple
                && IsInfallibleDictionaryLookup(body[next], dictionaryLookupImport);
            if (projectedDictionaryOnly && !lookup)
            {
                return null;
            }

            // The checked fields and their source tuples are reachable from
            // the incoming roots. Infallible get/1 retrieves its result from
            // the process dictionary after allocation, so only its X0 is live
            // at the original heap check.
            int heap = lookup ? next + 1 : next;
            int liveAfterSetup = lookup ? 1 : 0;

            return heap + 1 < body.Count
                && body[heap] is { Definition.Opcode: TestHeap, Operands: [_, var roots] }
                && roots.Value == liveAfterSetup
                && body[heap + 1].Definition.Opcode is PutTuple or PutList ? heap : null;
        }

        bool HasSavedAllocationInputs(
            int first,
            int afterMoves,
            BeamOperand frame,
            BeamOperand live
        ) =>
            body.Skip(first + 1).Take(afterMoves - first - 1).All(instruction =>
                instruction.Definition.Opcode == InitYRegisters || instruction is
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.X } input, { Kind: BeamOperandParameters.Y } slot]
                } && input.Value < live.Value && slot.Value < frame.Value);
    }
}
