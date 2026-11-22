using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ClearClosedStringArmCallRoot(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled
            || !atoms.TryGetValue(ErlangBooleanAtoms.False, out int falseAtom)
            || !atoms.TryGetValue(ErlangBooleanAtoms.True, out int trueAtom)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        byte[]? rewritten = RewriteClosedStringArmCallRoot(
            code,
            decoded.Body,
            falseAtom,
            trueAtom
        );
        if (rewritten is not null)
        {
            highestOpcode = Math.Max(highestOpcode, InitYRegisters);
        }

        return rewritten;
    }

    private static byte[]? RewriteClosedStringArmCallRoot(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        int falseAtom,
        int trueAtom
    )
    {
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var x1 = new BeamOperand(BeamOperandParameters.X, 1);
        var x2 = new BeamOperand(BeamOperandParameters.X, 2);
        int copied = 0;
        for (int entry = 0, end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = end + 1;
            (int[] Owners, Dictionary<int, bool[]> Uses)? ownership = null;
            bool? unprotected = null;
            for (int call = start + 4; call + 1 < end; call++)
            {
                if (body[call] is not { Definition.Opcode: LocalCall or ExternalCall, Operands: [{ Value.IsOne: true }, _] }
                    || body[call + 1] is not
                    {
                        Definition.Opcode: SelectValue,
                        Operands: [var selected, _, _, { Kind: BeamOperandParameters.Atom } first, _,
                        { Kind: BeamOperandParameters.Atom } second, _]
                    } || selected != x0 || first.Value != falseAtom || second.Value != trueAtom)
                {
                    continue;
                }

                int load = body[call - 1].Definition.Opcode == Line ? call - 2 : call - 1;
                int guard = load - 1;
                if (body[load] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [{ Kind: BeamOperandParameters.Y } payload, var argument]
                    } || argument != x0 || body[guard] is not
                    {
                        Definition.Opcode: ExactEqualTest,
                        Operands: [{ Kind: BeamOperandParameters.Label } retry, var suffix, { Kind: BeamOperandParameters.LiteralValue }]
                    } || suffix != x0 || !(unprotected ??= HasUnprotectedCallKillSchedule(body, start)))
                {
                    continue;
                }

                flow ??= new BeamProjectionFlow(body);
                if (!flow.TryGetForwardLabel(guard, (int)retry.Value, out int fallback)
                    || fallback + 5 >= end || body[fallback + 1].Definition.Opcode != TestHeap
                    || body[fallback + 2] is not { Definition.Opcode: PutList, Operands: [_, _, var accumulator] }
                    || accumulator != x1 || body[fallback + 3] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } retained, var third]
                    } || retained == payload || third != x2
                    || body[fallback + 4] is not { Definition.Opcode: Move, Operands: [{ Kind: BeamOperandParameters.Y }, var beforeTail] }
                    || beforeTail != x0 || body[fallback + 5] is not
                    {
                        Definition.Opcode: TailLocalCallWithFrame,
                        Operands: [{ Value: var arity }, _, _]
                    } || arity != 3 || (ownership ??= ReadDisjointFrameOwnership(
                        body,
                        start,
                        end,
                        trackSiblingTrimEpochs: true
                    )) is not { } frame)
                {
                    continue;
                }

                int owner = frame.Owners[call - start];
                if (owner < 0 || frame.Owners[guard - start] != owner
                    || frame.Owners[fallback + 3 - start] != owner || retained.Value >= frame.Uses[owner].Length
                    || !flow.IsDead(call, retained, allowEntryFramePredicates: true))
                {
                    continue;
                }

                // The fallback skips this definition and keeps the original input.
                // The accepted string arm never reads the slot on any exit.
                int insert = body[load].Offset;
                output.Write(code.Span[copied..insert]);
                WriteFrameInitialization(output, [(int)retained.Value]);
                copied = insert;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
