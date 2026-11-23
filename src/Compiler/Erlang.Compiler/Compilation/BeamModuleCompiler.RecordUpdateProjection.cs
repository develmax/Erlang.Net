using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static (int First, int End, int Live)? ReadRecordUpdateProjection(
        IReadOnlyList<BeamInstruction> body,
        int heap,
        int original,
        ref BeamProjectionFlow? flow
    )
    {
        if (original != 0 || heap < 2 || body[heap - 2] is not
            {
                Definition.Opcode: GetTupleElement,
                Operands: [
                { Kind: BeamOperandParameters.X, Value.IsZero: true, TypeIndex: null },
                    _,
                { Kind: BeamOperandParameters.Y, TypeIndex: null } field
                ]
            } || body[heap - 1] is not
            {
                Definition.Opcode: IsTaggedTuple,
                Operands: [
                { Kind: BeamOperandParameters.Label } failure,
                { Kind: BeamOperandParameters.Y } record,
                    var arity,
                    _
                ]
            } || SameProjectionRegister(field, record) || body[heap + 1] is not
            {
                Definition.Opcode: UpdateRecord,
                Operands: [_, var size, var source, _, { Kind: BeamOperandParameters.Count } count, _, var replacement]
            } || !SameProjectionRegister(source, record) || !SameProjectionRegister(replacement, field)
            || count.Value != 2 || size.Value != arity.Value)
        {
            return null;
        }

        // Sink the projection into the successful record branch. Keep the original tuple
        // rooted across the heap test, and require an already initialized field slot.
        flow ??= new BeamProjectionFlow(body);
        if (!flow.IsInitialized(heap - 2, field) || !flow.TryGetSingleForwardTarget(
            heap - 1,
            (int)failure.Value,
            out int target
        ))
        {
            return null;
        }

        int failureExit = target + 1;
        while (failureExit < body.Count && body[failureExit].Definition.Opcode == Line)
        {
            failureExit++;
        }

        if (failureExit >= body.Count || body[failureExit] is not
            {
                Definition.Opcode: BadRecord,
                Operands: [var rejected]
            } || !SameProjectionRegister(rejected, record))
        {
            return null;
        }

        return (heap - 2, heap - 1, 1);
    }
}
