using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? RefineListFallbacksAfterNilTest(ReadOnlyMemory<byte> code, int labelCount) =>
        !ModuleOptimizationDisabled && SsaTypeInferenceEnabled
        && code.Span.Contains(IsNil) && code.Span.Contains(IsList)
        && DecodeLateAtomicProjectionSlots(code, labelCount) is { Body: var body }
            ? RewriteListFallbacksAfterNilTest(code, body)
            : null;

    private static byte[]? RewriteListFallbacksAfterNilTest(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        BeamProjectionFlow? flow = null;
        byte[]? rewritten = null;
        for (int index = 0; index < body.Count; index++)
        {
            if (!ReadEntryListKindTest(
                body[index],
                IsNil,
                out int failure,
                out var input
            ) || !(flow ??= new BeamProjectionFlow(body)).TryGetSingleForwardTarget(index, failure, out int target))
            {
                continue;
            }

            int test = target + 1;
            while (test < body.Count && body[test].Definition.Opcode == Line)
            {
                test++;
            }

            // Only the nil failure edge enters this label. The original
            // register is unchanged, so is_list succeeds only for a cons.
            if (test < body.Count && ReadEntryListKindTest(
                body[test],
                IsList,
                out _,
                out var list
            ) && list == input)
            {
                rewritten ??= code.ToArray();
                rewritten[body[test].Offset] = IsNonemptyList;
            }
        }

        return rewritten;
    }

    private static byte[]? OrderEntryListKinds(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Label)> labelOperands
    )
    {
        if (!code.Span.Contains(IsNil) || !code.Span.Contains(IsNonemptyList))
        {
            return null;
        }

        using var rewritten = new MemoryStream();
        Dictionary<int, int>? incomingLabels = null;
        int copied = 0;
        BeamOperand? precedingLine = null;
        for (int entry = 0; entry + 4 < body.Count; entry++)
        {
            if (body[entry].Definition.Opcode == Line)
            {
                precedingLine = body[entry].Operands[0];
            }

            int nilTest = entry + 2;
            if (body[entry].Definition.Opcode != FunctionInfo || body[entry + 1].Definition.Opcode != Label
                || !ReadEntryListKindTest(
                    body[nilTest],
                    IsNil,
                    out int alternate,
                    out BeamOperand? input
                ))
            {
                continue;
            }

            int nilStart = nilTest + 1;
            int middle = EntryListBlockEnd(body, nilStart);
            int consTest = middle + 1;
            if (middle == nilStart || consTest >= body.Count
                || body[middle].Definition.Opcode != Label || body[middle].Operands[0].Value != alternate
                || !ReadEntryListKindTest(
                    body[consTest],
                    IsNonemptyList,
                    out int remaining,
                    out BeamOperand? consInput
                )
                || alternate == remaining || input != consInput || !IsEntryListTail(body[middle - 1]))
            {
                continue;
            }

            int consStart = consTest + 1;
            int suffix = EntryListBlockEnd(body, consStart);
            if (suffix >= body.Count || suffix == consStart || !IsEntryListTail(body[suffix - 1])
                || !CanReorderEntryListBlocks(
                    body,
                    nilStart,
                    middle,
                    consStart,
                    suffix,
                    precedingLine,
                    out BeamOperand? oldLastLine
                ))
            {
                continue;
            }

            incomingLabels ??= labelOperands.GroupBy(reference => reference.Label)
                .ToDictionary(group => group.Key, group => group.Count());
            if (incomingLabels.GetValueOrDefault(alternate) != 1)
            {
                continue;
            }

            // OTP select_types tries cons before nil. Both predicates are pure and their arms are disjoint.
            ReadOnlySpan<byte> wire = code.Span;
            rewritten.Write(wire[copied..body[nilTest].Offset]);
            rewritten.WriteByte(IsNonemptyList);
            rewritten.Write(wire[(body[nilTest].Offset + 1)..body[nilStart].Offset]);
            rewritten.Write(wire[body[consStart].Offset..body[suffix].Offset]);
            rewritten.Write(wire[body[middle].Offset..body[consTest].Offset]);
            rewritten.WriteByte(IsNil);
            rewritten.Write(wire[(body[consTest].Offset + 1)..body[consStart].Offset]);
            rewritten.Write(wire[body[nilStart].Offset..body[middle].Offset]);
            copied = body[suffix].Offset;
            precedingLine = oldLastLine;
            entry = suffix - 1;
        }

        if (copied == 0)
        {
            return null;
        }

        rewritten.Write(code.Span[copied..]);

        return rewritten.ToArray();
    }

    private byte[]? OrderDisjointNilAndIntegerEntries(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(IsNil)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body }
            || body.Any(instruction => instruction.Definition.Opcode is DebugLine or ExecutableLine))
        {
            return null;
        }

        BeamProjectionFlow? flow = null;
        using var rewritten = new MemoryStream();
        int copied = 0;
        for (int entry = 1; entry + 4 < body.Count; entry++)
        {
            int nilTest = entry + 2;
            if (body[entry - 1].Definition.Opcode != Line || body[entry].Definition.Opcode != FunctionInfo
                || body[entry + 1].Definition.Opcode != Label
                || !ReadEntryListKindTest(
                    body[nilTest],
                    IsNil,
                    out int alternate,
                    out BeamOperand? input
                ) || input is not { } value)
            {
                continue;
            }

            int nilStart = nilTest + 1;
            int middle = EntryListBlockEnd(body, nilStart);
            int integerTest = middle + 1;
            if (integerTest >= body.Count || body[integerTest] is not
                {
                    Definition.Opcode: ExactEqualTest,
                    Operands: [{ Kind: BeamOperandParameters.Label, Value.Sign: > 0 } failure,
                        var compared, { Kind: BeamOperandParameters.Integer, TypeIndex: null } literal]
                }
                || compared != value || failure.Value == alternate
                || !(flow ??= new BeamProjectionFlow(body)).TryGetSingleForwardTarget(nilTest, alternate, out int target)
                || target != middle || !IsTerminalConstruction(nilStart, middle, needsOwnLine: false))
            {
                continue;
            }

            int integerStart = integerTest + 1;
            int fallback = EntryListBlockEnd(body, integerStart);
            if (fallback >= body.Count || body[fallback] is not { Definition.Opcode: Label, Operands: [var fallbackLabel] }
                || fallbackLabel.Value != failure.Value || !IsTerminalConstruction(integerStart, fallback, needsOwnLine: true))
            {
                continue;
            }

            int fallbackStart = fallback + 1;
            int end = EntryListBlockEnd(body, fallbackStart);
            if (!IsTerminalConstruction(fallbackStart, end, needsOwnLine: true))
            {
                continue;
            }

            // Core matching partitions disjoint literal types before comparing
            // their values. A different integer bypasses the nil arm directly.
            int type = GetOperandTypeIndex((IntegerTypeBits, null, null, null));
            ReadOnlySpan<byte> bytes = code.Span;
            rewritten.Write(bytes[copied..body[nilTest].Offset]);
            WritePredicate(IsInteger, alternate, value);
            rewritten.WriteByte(ExactEqualTest);
            BeamCompactWriter.Write(rewritten, LabelTag, failure.Value);
            rewritten.WriteByte(ExtendedTypedRegisterOperand);
            WriteRegister(rewritten, value);
            BeamCompactWriter.Write(rewritten, BeamWire.UnsignedTag, type);
            BeamCompactWriter.Write(rewritten, BeamWire.IntegerTag, literal.Value);
            rewritten.Write(bytes[body[integerStart].Offset..body[fallback].Offset]);
            rewritten.Write(bytes[body[middle].Offset..body[integerTest].Offset]);
            WritePredicate(IsNil, (int)failure.Value, value);
            int nilTail = middle - 1;
            rewritten.Write(bytes[body[nilStart].Offset..body[nilTail].Offset]);
            if (body[nilTail - 1].Definition.Opcode != Line)
            {
                // The nil arm originally inherited the function's entry line.
                rewritten.Write(bytes[body[entry - 1].Offset..body[entry].Offset]);
            }

            rewritten.Write(bytes[body[nilTail].Offset..body[middle].Offset]);
            copied = body[fallback].Offset;
            entry = fallback;
        }

        if (copied == 0)
        {
            return null;
        }

        rewritten.Write(code.Span[copied..]);

        return RebuildLineTable(OrderTypeOperandsByFirstUse(rewritten.ToArray(), labelCount).ToArray(), labelCount);

        bool IsTerminalConstruction(int start, int end, bool needsOwnLine)
        {
            if (end <= start || body[end - 1].Definition.Opcode != TailExternalCall)
            {
                return false;
            }

            int last = end - 1;
            bool ownLine = last > start && body[last - 1].Definition.Opcode == Line;
            if (ownLine)
            {
                last--;
            }

            return (!needsOwnLine || ownLine) && body.Skip(start).Take(last - start).All(instruction =>
                instruction.Definition.Opcode is Move or TestHeap or PutTuple);
        }

        void WritePredicate(byte opcode, int failure, BeamOperand input)
        {
            rewritten.WriteByte(opcode);
            BeamCompactWriter.Write(rewritten, LabelTag, failure);
            WriteRegister(rewritten, input);
        }
    }

    private static bool ReadEntryListKindTest(
        BeamInstruction instruction,
        byte opcode,
        out int failure,
        out BeamOperand? input
    )
    {
        if (instruction.Definition.Opcode == opcode && instruction.Operands is
            [{ Kind: BeamOperandParameters.Label } label, { Kind: BeamOperandParameters.X, TypeIndex: null } source])
        {
            failure = (int)label.Value;
            input = source;

            return failure != 0;
        }

        failure = 0;
        input = null;

        return false;
    }

    private static int EntryListBlockEnd(IReadOnlyList<BeamInstruction> body, int start)
    {
        while (start < body.Count && body[start].Definition.Opcode is not (Label or FunctionInfo or EndCode))
        {
            start++;
        }

        return start;
    }

    private static bool IsEntryListTail(BeamInstruction instruction) => instruction.Definition.Opcode
        is Return or TailLocalCall or TailLocalCallWithFrame or TailExternalCall or TailExternalCallWithFrame;

    private static bool CanReorderEntryListBlocks(
        IReadOnlyList<BeamInstruction> body,
        int nilStart,
        int middle,
        int consStart,
        int suffix,
        BeamOperand? precedingLine,
        out BeamOperand? oldLastLine
    )
    {
        oldLastLine = precedingLine;
        if (!ReadEntryListBlockLine(
            body,
            nilStart,
            middle,
            out BeamOperand? nilLine
        )
            || !ReadEntryListBlockLine(
                body,
                consStart,
                suffix,
                out BeamOperand? consLine
            ))
        {
            return false;
        }

        oldLastLine = consLine ?? nilLine ?? precedingLine;

        return PreservesEntryListLine(
            body,
            nilStart,
            middle,
            precedingLine,
            consLine ?? precedingLine
        )
            && PreservesEntryListLine(
                body,
                consStart,
                suffix,
                nilLine ?? precedingLine,
                precedingLine
            )
            && PreservesEntryListLine(
                body,
                suffix,
                body.Count,
                oldLastLine,
                nilLine ?? consLine ?? precedingLine
            );
    }

    private static bool ReadEntryListBlockLine(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        out BeamOperand? lastLine
    )
    {
        lastLine = null;
        for (int index = start; index < end; index++)
        {
            BeamInstruction instruction = body[index];
            switch (instruction.Definition.Opcode)
            {
                case Line:
                    lastLine = instruction.Operands[0];
                    break;

                case DebugLine:
                case ExecutableLine:
                case CatchEnter:
                case CatchEnd:
                case TryEnter:
                case TryEnd:
                case TryCase:
                case TryCaseEnd:
                case LoopReceive:
                case LoopReceiveEnd:
                case RemoveMessage:
                case ReceiveTimeout:
                case ReceiveMarkerReserve:
                case ReceiveMarkerBind:
                case ReceiveMarkerUse:
                case ReceiveMarkerClear:
                case Wait:
                case WaitTimeout:
                case OnLoad:
                case NifStart:
                    return false;
            }
        }

        return true;
    }

    private static bool PreservesEntryListLine(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        BeamOperand? oldLine,
        BeamOperand? newLine
    )
    {
        if (oldLine == newLine)
        {
            return true;
        }

        // Instructions preceding their own line inherit the previous line in physical code order.
        for (int index = start; index < end; index++)
        {
            BeamInstruction instruction = body[index];
            byte opcode = instruction.Definition.Opcode;
            if (opcode is Line or EndCode)
            {
                return true;
            }

            if (opcode is not (
                    Label or Move or GetList or GetHead or GetTail or GetTupleElement or Deallocate or Return
                    or TailLocalCall or TailLocalCallWithFrame
                )
                && !(IsEntryFramePredicate(opcode) && instruction.Operands[0].Kind == BeamOperandParameters.Label))
            {
                return false;
            }
        }

        return true;
    }
}
