using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
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
