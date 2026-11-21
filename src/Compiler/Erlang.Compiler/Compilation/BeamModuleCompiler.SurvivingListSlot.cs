using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? StoreListIntoSurvivingSlot(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !code.Span.Contains(PutList) || !code.Span.Contains(TrimFrame)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var body = decoded.Body;
        using var output = new MemoryStream();
        int copied = 0;
        for (int index = 0; index + 3 < body.Count; index++)
        {
            if (body[index] is not
                {
                    Definition.Opcode: PutList,
                    Operands: [
                    { Kind: BeamOperandParameters.Y, TypeIndex: null } head,
                    { Kind: BeamOperandParameters.X, TypeIndex: null, Value.IsZero: true },
                    { Kind: BeamOperandParameters.Y, TypeIndex: null } stored]
                } cons || head == stored)
            {
                continue;
            }

            int copy = index + 1;
            while (copy < body.Count && body[copy] is
                {
                    Definition.Opcode: Move,
                    Operands: [var literal, { Kind: BeamOperandParameters.X, TypeIndex: null }]
                } && IsNonRegisterValue(literal))
            {
                copy++;
            }

            if (copy + 2 >= body.Count || body[copy] is not
                { Definition.Opcode: Move, Operands: [var source, var destination] }
                || source != stored || destination != head || body[copy + 1] is not
                { Definition.Opcode: TrimFrame, Operands: [var removed, _] }
                || stored.Value >= removed.Value || head.Value < removed.Value)
            {
                continue;
            }

            // The cons reads the old head before assigning its destination. The intervening
            // literal moves access neither Y slot, and trim discards the original destination.
            int start = cons.Offset + 1;
            var cursor = new BeamCursor(code[start..]);
            _ = BeamCompactReader.Read(cursor);
            _ = BeamCompactReader.Read(cursor);
            output.Write(code.Span[copied..(start + cursor.Position)]);
            WriteRegister(output, head);
            output.Write(code.Span[body[index + 1].Offset..body[copy].Offset]);
            copied = body[copy + 1].Offset;
            index = copy;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
