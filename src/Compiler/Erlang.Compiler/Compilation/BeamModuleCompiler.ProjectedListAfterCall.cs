using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ScheduleProjectedListAfterCall(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(GetList)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        return RewriteProjectedListAfterCall(code, body);
    }

    private static byte[]? RewriteProjectedListAfterCall(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        var result = new BeamOperand(BeamOperandParameters.X, 0);
        var projected = new BeamOperand(BeamOperandParameters.X, 1);
        BeamProjectionFlow? flow = null;
        var replacements = new Dictionary<int, byte[]>();
        for (int call = 0; call + 5 < body.Count; call++)
        {
            if (body[call].Definition.Opcode is not (LocalCall or ExternalCall)
                || body[call + 1] is not
                {
                    Definition.Opcode: Move, Operands: [var value, { Kind: BeamOperandParameters.Y, TypeIndex: null } saved]
                } || value != result
                || body[call + 2] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } carrier, _, var field]
                } || carrier == saved || field != result
                || body[call + 3] is not { Definition.Opcode: IsNonemptyList, Operands: [var empty, var tested] }
                || tested != result
                || body[call + 4] is not { Definition.Opcode: GetList, Operands: [var list, var head, var tail] }
                || list != result || head != result || tail != projected)
            {
                continue;
            }

            flow ??= new BeamProjectionFlow(body);
            if (!flow.HasStraightCheckedCallPrefix(call, call + 4)
                || !flow.TryGetSingleForwardTarget(call + 3, (int)empty.Value, out int emptyEntry)
                || emptyEntry + 2 >= body.Count
                || body[emptyEntry + 1] is not { Definition.Opcode: IsNil, Operands: [var failure, var nil] }
                || nil != result
                || !flow.HasStraightCheckedCallPrefix(emptyEntry, emptyEntry + 1)
                || !flow.TryGetSingleForwardTarget(emptyEntry + 1, (int)failure.Value, out int failureEntry))
            {
                continue;
            }

            int terminal = failureEntry + 1;
            while (terminal < body.Count && body[terminal].Definition.Opcode == Line)
            {
                terminal++;
            }

            if (terminal + 1 >= body.Count
                || body[terminal] is not { Definition.Opcode: CaseEnd, Operands: [var reason] } || reason != result
                || !HasUnprotectedCheckedFailureRegion(body, call, terminal)
                || !flow.HasStraightCheckedCallPrefix(failureEntry, terminal)
                || !flow.IsFallthroughTemporaryDead(emptyEntry + 1, result)
                || !flow.IsFallthroughTemporaryDead(emptyEntry + 1, projected))
            {
                continue;
            }

            // Move the already checked tuple read before the copy of
            // X0 into its saved Y. The two operations use independent slots.
            // The list's success defines both output registers, and the nil
            // edge drops the values. Keep the projected reason on rejection.
            replacements.Add(call + 1, [.. ReplaceLast(call + 2), .. InstructionBytes(call + 1)]);
            replacements.Add(call + 2, []);
            replacements.Add(call + 3, ReplaceLast(call + 3));
            replacements.Add(call + 4, [GetList, .. EncodeProjectedRegister(), .. InstructionBytes(call + 4)[2..]]);
            replacements.Add(emptyEntry + 1, ReplaceLast(emptyEntry + 1));
            replacements.Add(terminal, ReplaceLast(terminal));
        }

        if (replacements.Count == 0)
        {
            return null;
        }

        using var output = new MemoryStream();
        for (int index = 0; index + 1 < body.Count; index++)
        {
            if (replacements.TryGetValue(index, out var replacement))
            {
                output.Write(replacement);
            }
            else
            {
                output.Write(InstructionBytes(index));
            }
        }

        output.Write(code.Span[body[^1].Offset..]);

        return output.ToArray();

        ReadOnlySpan<byte> InstructionBytes(int index) => code.Span[body[index].Offset..body[index + 1].Offset];

        byte[] ReplaceLast(int index) => [.. InstructionBytes(index)[..^1], .. EncodeProjectedRegister()];

        byte[] EncodeProjectedRegister()
        {
            using var register = new MemoryStream();
            WriteRegister(register, projected);

            return register.ToArray();
        }
    }
}
