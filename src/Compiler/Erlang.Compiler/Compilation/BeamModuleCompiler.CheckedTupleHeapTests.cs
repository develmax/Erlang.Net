using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static (int First, int End, int Live)? ReadCheckedTupleHeap(
        IReadOnlyList<BeamInstruction> body,
        int heap,
        int original,
        ref BeamProjectionFlow? flow
    )
    {
        bool nilGuard = body[heap - 1].Definition.Opcode == IsNil;
        int end = nilGuard ? heap - 1 : heap;
        if (end < 3 || body[end - 1] is not
            {
                Definition.Opcode: GetTupleElement,
                Operands: [{ Kind: BeamOperandParameters.X, TypeIndex: null } tuple, _, _]
            })
        {
            return null;
        }

        var written = new HashSet<int>();
        int largestField = -1;
        int first = end;
        while (first > 0 && body[first - 1] is
            {
                Definition.Opcode: GetTupleElement,
                Operands: [var source, var field, { Kind: BeamOperandParameters.X, TypeIndex: null } destination]
            } && source == tuple)
        {
            if (field.Value < 0 || field.Value > int.MaxValue || (destination == tuple && first != end))
            {
                return null;
            }

            largestField = Math.Max(largestField, (int)field.Value);
            written.Add((int)destination.Value);
            first--;
        }

        if (end - first < 2 || first == 0 || body[first - 1] is not
            {
                Definition.Opcode: IsTaggedTuple,
                Operands: [{ Kind: BeamOperandParameters.Label } failure, var checkedTuple, var arity, _]
            } || !SameProjectionRegister(checkedTuple, tuple) || arity.Value <= largestField)
        {
            return null;
        }

        if (nilGuard)
        {
            if (body[end] is not
                {
                    Operands: [var nilFailure, { Kind: BeamOperandParameters.X } tested]
                } || nilFailure != failure || written.Contains((int)tested.Value))
            {
                return null;
            }

            foreach (int register in written)
            {
                if (!(flow ??= new BeamProjectionFlow(body)).IsGuardTemporaryDeadOnFailure(
                    end,
                    new BeamOperand(BeamOperandParameters.X, register),
                    preserveHeapRoots: true
                ))
                {
                    return null;
                }
            }
        }

        int receiver = (int)tuple.Value;
        int live = receiver + 1;
        for (int register = live; register < original; register++)
        {
            if (!written.Contains(register) && !(flow ??= new BeamProjectionFlow(body)).IsBranchTemporaryDead(
                heap,
                new BeamOperand(BeamOperandParameters.X, register)
            ))
            {
                live = register + 1;
            }
        }

        // Unchanged slots from the old prefix are already initialized here.
        // The checked tuple initializes its own slot before any projection.
        if (live >= original || written.Any(register => register < live && register != receiver))
        {
            return null;
        }

        return (first, end, live);
    }
}
