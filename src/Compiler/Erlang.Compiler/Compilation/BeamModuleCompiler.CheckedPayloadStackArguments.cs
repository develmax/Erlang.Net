using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? OrderFinalCheckedPayloadStackArguments(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(InitYRegisters)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        return OrderCheckedPayloadStackArguments(code, body);
    }

    private static byte[]? OrderCheckedPayloadStackArguments(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body)
    {
        using var output = new MemoryStream();
        int copied = 0;
        for (int call = 0; call < body.Count; call++)
        {
            if (body[call] is not { Definition.Opcode: LocalCall or ExternalCall, Operands: [var count, _] }
                || count.Value < 3 || count.Value > BeamOperandParameters.MaximumRegister)
            {
                continue;
            }

            int end = call;
            while (end > 0 && body[end - 1].Definition.Opcode == Line)
            {
                end--;
            }

            int arity = (int)count.Value;
            int start = end - arity - 1;
            if (start < 3 || body[start - 3] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var returned, { Kind: BeamOperandParameters.Y, TypeIndex: null } retained]
                } || !IsBareX0(returned)
                || body[start - 2] is not
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [_, var checkedTuple, var tupleArity, _]
                } || !IsBareX0(checkedTuple)
                || body[start - 1] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var original, var field, { Kind: BeamOperandParameters.Y, TypeIndex: null } receiver]
                } || !IsBareX0(original) || receiver == retained || field.Value >= tupleArity.Value
                || ReadSplitStackArgumentOrder(body, end, arity) is not { } order
                || body[end - 1].Operands[0] != receiver
                || order.SequenceEqual(Enumerable.Range(start, arity - 1)))
            {
                continue;
            }

            // Only the adjacent independent stack loads move. The clear
            // group and the separately owned X0 receiver keep their places.
            output.Write(code.Span[copied..body[start].Offset]);
            foreach (int index in order)
            {
                output.Write(code.Span[body[index].Offset..body[index + 1].Offset]);
            }

            copied = body[end - 2].Offset;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
