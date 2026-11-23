using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? OrderAritySelectedFrameRootCopies(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(SelectTupleArity)
            || !code.Span.Contains(IsTaggedTuple) || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        BeamProjectionFlow? flow = null;
        using var output = new MemoryStream();
        int copied = 0;
        int entry = -1;
        for (int frame = 0; frame + 4 < body.Count; frame++)
        {
            if (body[frame].Definition.Opcode == FunctionInfo)
            {
                entry = frame + 1;
            }

            if (entry < 0 || frame < entry + 8 || body[frame] is not
                { Definition.Opcode: Allocate, Operands: [var width, var live] }
                || width.Value != 2 || body[frame + 1] is not
                {
                    Definition.Opcode: Move,
                    Operands: [
                    { Kind: BeamOperandParameters.X, TypeIndex: null } formal,
                    { Kind: BeamOperandParameters.Y, Value.IsOne: true, TypeIndex: null }
                    ]
                } || body[frame + 2] is not
                {
                    Definition.Opcode: Move,
                    Operands: [
                    { Kind: BeamOperandParameters.X, TypeIndex: null } projected,
                    { Kind: BeamOperandParameters.Y, Value.IsZero: true, TypeIndex: null }
                    ]
                } || formal == projected || formal.Value >= live.Value || projected.Value >= live.Value
                || body[frame + 3].Definition.Opcode != Line || !IsOwnedProjectionUnaryCall(body[frame + 4])
                || !HasFormalAndProjectedArityOwner(
                    body,
                    entry,
                    frame,
                    formal,
                    projected,
                    flow ??= new BeamProjectionFlow(body)
                ))
            {
                continue;
            }

            // reserve_yregs copies the older formal before the derived field
            // in SSA. Codegen reverses these two independent physical copies.
            int first = body[frame + 1].Offset;
            int second = body[frame + 2].Offset;
            int after = body[frame + 3].Offset;
            output.Write(code.Span[copied..first]);
            output.Write(code.Span[second..after]);
            output.Write(code.Span[first..second]);
            copied = after;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static bool HasFormalAndProjectedArityOwner(
        IReadOnlyList<BeamInstruction> body,
        int entry,
        int frame,
        BeamOperand formal,
        BeamOperand projected,
        BeamProjectionFlow flow
    )
    {
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        if (formal == x0 || projected == x0
            || body[entry - 1] is not { Operands: [_, _, var arguments] } || formal.Value >= arguments.Value
            || body[entry].Definition.Opcode != Label
            || body[entry + 1] is not { Definition.Opcode: IsTuple, Operands: [_, var tuple] } || tuple != x0
            || body[entry + 2] is not { Definition.Opcode: SelectTupleArity, Operands: [var carrier, _, _, ..] } arities
            || !SameProjectionRegister(carrier, x0)
            || body[frame - 1] is not
            { Definition.Opcode: IsTaggedTuple, Operands: [var rejected, var checkedRecord, _, _] }
            || checkedRecord != formal)
        {
            return false;
        }

        int arm = frame - 2;
        if (body[arm] is { Definition.Opcode: ExactNotEqualTest, Operands: [var failed, var tested, var literal] }
            && failed == rejected && tested == projected && literal.Kind == BeamOperandParameters.Nil)
        {
            arm--;
        }

        int select = arm - 1;
        int group = select - 4;
        if (group <= entry + 2 || body[group] is not { Definition.Opcode: Label, Operands: [var arityLabel] }
            || !flow.TryGetSingleForwardTarget(entry + 2, (int)arityLabel.Value, out int arityEntry) || arityEntry != group
            || body[arm] is not { Definition.Opcode: Label, Operands: [var armLabel] }
            || !flow.TryGetSingleForwardTarget(select, (int)armLabel.Value, out int armEntry) || armEntry != arm
            || body[select] is not { Definition.Opcode: SelectValue, Operands: [var selector, _, ..] }
            || !IsBareX(selector) || selector == formal || selector == projected || selector == x0
            || !IsProjection(group + 1, 0, selector)
            || !IsProjection(group + 2, 1, projected)
            || !IsProjection(group + 3, 2, x0))
        {
            return false;
        }

        for (int index = 3; index + 1 < arities.Operands.Count; index += 2)
        {
            if (arities.Operands[index].Value == 3 && arities.Operands[index + 1].Value == arityLabel.Value)
            {
                return true;
            }
        }

        return false;

        bool IsProjection(int index, int field, BeamOperand result) =>
            body[index] is { Definition.Opcode: GetTupleElement, Operands: [var source, var offset, var destination] }
            && source == x0 && offset.Value == field && destination == result;
    }
}
