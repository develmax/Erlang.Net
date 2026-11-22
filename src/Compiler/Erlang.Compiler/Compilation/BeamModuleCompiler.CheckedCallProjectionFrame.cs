using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? PlaceCheckedCallProjectionAfterFrame(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(GetTupleElement)
            || !code.Span.Contains(IsTaggedTuple)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        using var output = new MemoryStream();
        int copied = 0;
        for (int start = 0; start + 7 < body.Count; start++)
        {
            if (!ReadCheckedCallProjection(body, start))
            {
                continue;
            }

            int projection = body[start + 3].Offset;
            int frame = body[start + 5].Offset;
            int call = body[start + 7].Offset;

            // The checked carrier stays in the same live X0 root across
            // allocation. The independent saved value keeps the same slot.
            output.Write(code.Span[copied..projection]);
            output.Write(code.Span[frame..call]);
            output.Write(code.Span[projection..frame]);
            copied = call;
            start += 6;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static bool ReadCheckedCallProjection(IReadOnlyList<BeamInstruction> body, int start) =>
        body[start] is
        {
            Definition.Opcode: IsTaggedTuple,
            Operands: [var failure, var carrier, var arity, _]
        }
        && !failure.Value.IsZero && IsBareX0(carrier)
        && body[start + 1] is
        {
            Definition.Opcode: GetTupleElement,
            Operands: [var filterTuple, var filterField, { Kind: BeamOperandParameters.X, TypeIndex: null } filter]
        }
        && carrier == filterTuple && !filter.Value.IsZero && filterField.Value < arity.Value
        && body[start + 2] is
        {
            Definition.Opcode: ExactEqualTest,
            Operands: [var rejected, var tested, { Kind: BeamOperandParameters.Atom }]
        }
        && rejected == failure && tested == filter
        && body[start + 3] is
        {
            Definition.Opcode: GetTupleElement,
            Operands: [var tuple, var payload, var destination]
        }
        && tuple == carrier && destination == carrier && payload != filterField && payload.Value < arity.Value
        && body[start + 4].Definition.Opcode == Line
        && body[start + 5] is
        {
            Definition.Opcode: Allocate,
            Operands: [{ Value.IsOne: true }, { Value: var live }]
        }
        && body[start + 6] is
        {
            Definition.Opcode: Move,
            Operands: [{ Kind: BeamOperandParameters.X, TypeIndex: null } from,
            { Kind: BeamOperandParameters.Y, Value.IsZero: true, TypeIndex: null }]
        }
        && from.Value > 0 && from.Value < live
        && body[start + 7] is
        {
            Definition.Opcode: LocalCall or ExternalCall,
            Operands: [{ Value.IsOne: true }, _]
        };
}
