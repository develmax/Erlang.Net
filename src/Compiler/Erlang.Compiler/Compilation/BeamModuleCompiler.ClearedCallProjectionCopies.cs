using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private const int CallProjectionCopyWindow = 5;

    private byte[]? RemoveClearedCallProjectionCopies(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(InitYRegisters)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var affected = new HashSet<(int Atom, int Arity)>();
        byte[]? rewritten = RewriteClearedCallProjectionCopies(
            code,
            decoded.Body,
            affected
        );
        if (rewritten is null || DecodeLateAtomicProjectionSlots(rewritten, labelCount) is not { } reduced)
        {
            return rewritten;
        }

        return PromoteFrameSlots(
            rewritten,
            reduced.Body,
            reduced.StackOperands,
            allowTerminalTuple: true,
            recheckAfterCopyRemoval: affected
        ) ?? rewritten;
    }

    private static byte[]? RemoveClearedCallProjectionCopyPairs(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    ) => RewriteClearedCallProjectionCopies(code, body, affected: null);

    private static byte[]? RewriteClearedCallProjectionCopies(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        HashSet<(int Atom, int Arity)>? affected
    )
    {
        var edits = new List<CodeEdit>();
        int entry = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = end + 1;
            if (body.Skip(start).Take(end - start).Any(instruction =>
                instruction.Definition.Opcode is TryEnter or CatchEnter or DebugLine or ExecutableLine))
            {
                continue;
            }

            for (int index = start; index + 2 < end; index++)
            {
                var producer = body[index];
                if (producer.Definition.Opcode is not (GetTupleElement or GetHead or GetTail)
                    || producer.Operands[^1] is not { Kind: BeamOperandParameters.X, TypeIndex: null } original
                    || body[index + 1] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [var through, { Kind: BeamOperandParameters.Y, TypeIndex: null } saved]
                    } || through != original)
                {
                    continue;
                }

                int reload = FindCallProjectionReload(
                    body,
                    index + 2,
                    end,
                    original,
                    saved
                );
                if (reload < 0 || !HasClearedCallProjection(
                    body,
                    reload + 1,
                    end,
                    saved
                ))
                {
                    continue;
                }

                if (affected is not null && start > 0)
                {
                    affected.Add(ReadCallCopyFunction(body[start - 1]));
                }

                // The intervening instructions leave the old X value intact.
                // The existing clear still initializes the unused physical Y slot before GC.
                edits.Add(new(
                    body[index + 1].Offset,
                    body[index + 2].Offset,
                    0,
                    0,
                    IsRemoval: true
                ));
                edits.Add(new(
                    body[reload].Offset,
                    body[reload + 1].Offset,
                    0,
                    0,
                    IsRemoval: true
                ));
            }
        }

        edits.Sort((left, right) => left.Start.CompareTo(right.Start));

        return ApplyDuplicatedFrameReadEdits(code, edits);
    }

    private static (int Atom, int Arity) ReadCallCopyFunction(BeamInstruction info) =>
        ((int)info.Operands[1].Value, (int)info.Operands[2].Value);

    private static int FindCallProjectionReload(
        IReadOnlyList<BeamInstruction> body,
        int first,
        int end,
        BeamOperand original,
        BeamOperand saved
    )
    {
        int limit = Math.Min(end - 1, first + CallProjectionCopyWindow);
        for (int index = first; index <= limit; index++)
        {
            BeamInstruction instruction = body[index];
            if (instruction is { Definition.Opcode: Move, Operands: [var source, var destination] }
                && source == saved && destination == original)
            {
                return index;
            }

            if (instruction.Definition.Opcode is not (Move or GetTupleElement or GetHead or GetTail)
                || SameProjectionRegister(instruction.Operands[^1], original)
                || instruction.Operands.Any(operand => SameProjectionRegister(operand, saved)))
            {
                break;
            }
        }

        return -1;
    }

    private static bool HasClearedCallProjection(
        IReadOnlyList<BeamInstruction> body,
        int first,
        int end,
        BeamOperand saved
    )
    {
        bool cleared = false;
        int limit = Math.Min(end - 1, first + CallProjectionCopyWindow);
        for (int index = first; index <= limit; index++)
        {
            BeamInstruction instruction = body[index];
            switch (instruction.Definition.Opcode)
            {
                case Line:
                    break;
                case Move when !instruction.Operands.Any(operand => SameProjectionRegister(operand, saved)):
                    break;
                case InitYRegisters:
                    cleared |= instruction.Operands.Skip(1).Any(operand => SameProjectionRegister(operand, saved));
                    break;
                case LocalCall or ExternalCall:
                    return cleared && instruction.Operands[0].Value > 0;
                default:
                    return false;
            }
        }

        return false;
    }
}
