using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? RetainPostCallConstructorInputs(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(TestHeap)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        var edits = new List<CodeEdit>();
        var entries = new HashSet<int>();
        int first = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int entry = first;
            first = end + 1;
            if (entry >= end || body[entry] is not { Definition.Opcode: Label, Operands: [var label] }
                || ReadPostCallConstructionInput(body, entry, end) is not { } plan
                || ReadDisjointFrameOwnership(body, entry, end) is not { } frame || frame.Uses.Count != 1)
            {
                continue;
            }

            int allocation = frame.Uses.Keys.Single();
            if (frame.Owners[plan.Spill - entry] != allocation || frame.Owners[plan.LastUse - entry] != allocation)
            {
                continue;
            }

            int heap = plan.Spill + 1;
            edits.Add(new(
                body[plan.Spill].Offset,
                body[heap].Offset,
                0,
                0,
                IsRemoval: true
            ));
            int heapEnd = body[heap + 1].Offset;
            edits.Add(new(
                heapEnd - CompactRegisterCountWidth(0),
                heapEnd,
                BeamWire.UnsignedTag,
                1
            ));

            for (int at = heap + 1; at <= plan.LastUse; at++)
            {
                AddPostCallConstructionOperandEdits(
                    code,
                    body,
                    at,
                    plan.LastUse,
                    plan.Slot,
                    edits
                );
            }

            entries.Add((int)label.Value);
        }

        if (entries.Count == 0 || ApplyDuplicatedFrameReadEdits(code, edits) is not { } rewritten
            || DecodeLateAtomicProjectionSlots(rewritten, labelCount) is not { } updated)
        {
            return null;
        }

        return RewriteDisjointFrames(
            rewritten,
            updated.Body,
            updated.StackOperands,
            atomicOnly: false,
            entriesToCompact: entries
        );
    }

    private static (int Spill, int LastUse, BeamOperand Slot)? ReadPostCallConstructionInput(
        IReadOnlyList<BeamInstruction> body,
        int entry,
        int end
    )
    {
        for (int spill = entry + 1; spill + 2 < end; spill++)
        {
            if (body[spill] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var incoming, { Kind: BeamOperandParameters.Y, TypeIndex: null } slot]
                } || !IsBareX0(incoming) || body[spill + 1] is not
                {
                    Definition.Opcode: TestHeap,
                    Operands: [_, { Value.IsZero: true }]
                })
            {
                continue;
            }

            int producer = spill - 1;
            while (producer > entry && body[producer].Definition.Opcode == Line)
            {
                producer--;
            }

            if (body[producer].Definition.Opcode is not (LocalCall or ExternalCall))
            {
                continue;
            }

            int from = spill + 2;
            int constructionEnd = from;
            while (constructionEnd < end && body[constructionEnd].Definition.Opcode is (Line or Move or PutList or PutTuple))
            {
                constructionEnd++;
            }

            int last = -1;
            bool readsOutsideConstruction = false;
            for (int at = entry; at < end; at++)
            {
                var instruction = body[at];
                if (at == spill || instruction.Definition.Opcode == InitYRegisters
                    || !instruction.Operands.Any(operand => SameProjectionRegister(operand, slot)))
                {
                    continue;
                }

                if (at < from || at >= constructionEnd)
                {
                    readsOutsideConstruction = true;
                    break;
                }

                last = at;
            }

            if (readsOutsideConstruction || last <= from)
            {
                continue;
            }

            bool clobbersX0 = false;
            bool blocked = false;
            for (int at = from; at <= last; at++)
            {
                var operation = body[at];
                int write = ConstructionDestination(operation.Definition.Opcode);
                for (int position = 0; position < operation.Operands.Count; position++)
                {
                    BeamOperand operand = operation.Operands[position];
                    bool readsResult = SameProjectionRegister(operand, slot);
                    bool x0 = IsBareX0(operand);
                    // A constructor reads its inputs before replacing its destination.
                    bool writesFinalDestination = at == last && position == write;
                    if (operand.Kind == BeamOperandParameters.X
                        && (operand.Value == 1 && !writesFinalDestination || operand.Value.IsZero && operand.TypeIndex is not null)
                        || readsResult && (position == write || operand.TypeIndex is not null)
                        || x0 && position != write && !clobbersX0)
                    {
                        blocked = true;
                        break;
                    }
                }

                if (blocked)
                {
                    break;
                }

                if (at < last && write >= 0 && IsBareX0(operation.Operands[write]))
                {
                    clobbersX0 = true;
                }
            }

            if (blocked || !clobbersX0)
            {
                continue;
            }

            int finalWrite = ConstructionDestination(body[last].Definition.Opcode);
            if (finalWrite >= 0 && IsBareX0(body[last].Operands[finalWrite])
                || RestoresConstructorResultRegister(body, last + 1, constructionEnd))
            {
                return (spill, last, slot);
            }
        }

        return null;
    }

    private static bool RestoresConstructorResultRegister(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end
    )
    {
        for (int at = start; at < end; at++)
        {
            var instruction = body[at];
            int write = ConstructionDestination(instruction.Definition.Opcode);
            if (instruction.Operands.Where((_, position) => position != write)
                .Any(operand => operand.Kind == BeamOperandParameters.X && operand.Value.IsZero))
            {
                return false;
            }

            if (write >= 0 && IsBareX0(instruction.Operands[write]))
            {
                return true;
            }
        }

        return false;
    }

    private static int ConstructionDestination(byte opcode) => opcode switch
    {
        Move => 1,
        PutList => 2,
        PutTuple => 0,
        _ => -1
    };

    private static void AddPostCallConstructionOperandEdits(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        int at,
        int last,
        BeamOperand slot,
        List<CodeEdit> edits
    )
    {
        BeamInstruction operation = body[at];
        int first = operation.Offset + 1;
        var cursor = new BeamCursor(code[first..body[at + 1].Offset]);
        int write = ConstructionDestination(operation.Definition.Opcode);
        for (int position = 0; position < operation.Operands.Count; position++)
        {
            int start = first + cursor.Position;
            BeamOperand operand = operation.Operands[position];
            _ = BeamCompactReader.Read(cursor);
            if (operand.Kind is BeamOperandParameters.Count or BeamOperandParameters.LiteralValue)
            {
                _ = BeamCompactReader.Read(cursor);
            }
            else if (operand.TypeIndex is not null)
            {
                _ = BeamCompactReader.Read(cursor);
                _ = BeamCompactReader.Read(cursor);
            }

            if (SameProjectionRegister(operand, slot))
            {
                edits.Add(new(
                    start,
                    first + cursor.Position,
                    XRegisterTag,
                    0
                ));
            }
            else if (IsBareX0(operand) && (at != last || position != write))
            {
                edits.Add(new(
                    start,
                    first + cursor.Position,
                    XRegisterTag,
                    1
                ));
            }
        }
    }
}
