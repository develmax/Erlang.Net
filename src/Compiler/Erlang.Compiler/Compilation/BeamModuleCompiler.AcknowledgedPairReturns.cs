using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private ReadOnlyMemory<byte> FinalizeAcknowledgedPairReturns(ReadOnlyMemory<byte> code, ref int labelCount)
    {
        if (ModuleOptimizationDisabled || !code.Span.Contains(IsTaggedTuple)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body }
            || body.Any(instruction => instruction.Definition.Opcode is DebugLine or ExecutableLine)
            || RewriteAcknowledgedPairReturns(code, body) is not { } result)
        {
            return code;
        }

        int count = labelCount - result.RemovedLabels;
        if (RenumberOutlinedMapCaseLabels(result.Code, labelCount, physicalLabelCount: count) is not { } order)
        {
            return code;
        }

        labelCount = count;

        return order;
    }

    private static (byte[] Code, int RemovedLabels)? RewriteAcknowledgedPairReturns(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        BeamProjectionFlow? flow = null;
        using var output = new MemoryStream();
        int copied = 0;
        int removed = 0;
        var result = new BeamOperand(BeamOperandParameters.X, 0);
        for (int check = 1; check + 8 < body.Count; check++)
        {
            if (body[check - 1].Definition.Opcode is not (LocalCall or ExternalCall)
                || body[check] is not
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [var rejected, var checkedPair, { Value: var arity }, _]
                } || checkedPair != result || arity != 2
                || body[check + 1] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var source, { Kind: BeamOperandParameters.Y, TypeIndex: null } saved]
                } || source != result)
            {
                continue;
            }

            int call = check + 2;
            if (body[call].Definition.Opcode == Line)
            {
                call++;
            }

            int nextGuard = call + 1;
            int restore = call + 2;
            int fallback = call + 3;
            int shared = call + 4;
            int release = call + 5;
            int after = call + 7;
            if (after >= body.Count || body[call] is not
                {
                    Definition.Opcode: LocalCall or ExternalCall,
                    Operands: [{ Value.IsZero: true }, _]
                }
                || body[nextGuard] is not
                {
                    Definition.Opcode: ExactEqualTest,
                    Operands: [var declined, var selected, { Kind: BeamOperandParameters.Atom }]
                } || selected != result
                || body[restore] is not { Definition.Opcode: Move, Operands: [var pair, var destination] }
                || pair != saved || destination != result || rejected.Value == declined.Value
                || body[release].Definition.Opcode != Deallocate || body[release + 1].Definition.Opcode != Return
                || !(flow ??= new BeamProjectionFlow(body)).TryGetExclusiveForwardTarget(
                    check,
                    restore,
                    (int)rejected.Value,
                    out int outer
                ) || outer != fallback || !flow.TryGetExclusiveForwardTarget(
                    nextGuard,
                    fallback,
                    (int)declined.Value,
                    out int inner
                ) || inner != shared)
            {
                continue;
            }

            // Both rejections own the same return of the current X0 and never
            // inspect the stored pair. Keep the successful return in its own block.
            output.Write(code.Span[copied..body[check].Offset]);
            Copy(check + 1, check + 2);
            Copy(check, check + 1);
            Copy(check + 2, nextGuard);
            int guardOperand = body[nextGuard].Offset + 1;
            var cursor = new BeamCursor(code[guardOperand..body[restore].Offset]);
            _ = BeamCompactReader.Read(cursor);
            output.WriteByte(ExactEqualTest);
            BeamCompactWriter.Write(output, LabelTag, rejected.Value);
            output.Write(code.Span[(guardOperand + cursor.Position)..body[restore].Offset]);
            Copy(restore, fallback);
            Copy(release, after);
            Copy(fallback, shared);
            Copy(release, after);
            copied = body[after].Offset;
            removed++;
            check = after - 1;
        }
        if (removed == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return (output.ToArray(), removed);

        void Copy(int first, int end) => output.Write(code.Span[body[first].Offset..body[end].Offset]);
    }
}
