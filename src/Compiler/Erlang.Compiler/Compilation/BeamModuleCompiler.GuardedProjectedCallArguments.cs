using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static (int FirstArgument, int AfterArguments)? ReadGuardedProjectedCallArguments(
        IReadOnlyList<BeamInstruction> body,
        int first,
        ref BeamProjectionFlow? flow
    )
    {
        const int guardSize = 13;
        int argument = first + guardSize + 2;
        if (argument + 3 >= body.Count || body[first] is not
            {
                Definition.Opcode: GetTupleElement,
                Operands: [var carrier, _, { Kind: BeamOperandParameters.Y, TypeIndex: null } saved]
            } || !IsBareProjectionRegister(carrier) || SameProjectionRegister(carrier, saved)
            || IsX(carrier, 0) || IsX(carrier, 1)
            || body[first + 1] is not { Definition.Opcode: Move, Operands: [var loaded, var input] }
            || loaded != saved || !IsX(input, 0)
            || body[first + 2] is not
            {
                Definition.Opcode: BifOne,
                Operands: [{ Kind: BeamOperandParameters.Label } rejectedBif, _, var measured, var boolean]
            } || !IsX(measured, 0) || !IsX(boolean, 1) || rejectedBif.Value.IsZero
            || !IsGuardBif(3, failure: false) || !IsGuardBif(4, failure: true)
            || !IsGuardBif(5, failure: false) || !IsGuardBif(6, failure: false)
            || body[first + 7] is not { Definition.Opcode: Jump, Operands: [var join] }
            || body[first + 8] is not { Definition.Opcode: Label, Operands: [var bifLanding] }
            || rejectedBif.Value != bifLanding.Value
            || body[first + 9] is not
            {
                Definition.Opcode: Move, Operands: [{ Kind: BeamOperandParameters.Atom }, var falseRegister]
            } || falseRegister != boolean
            || body[first + 10] is not { Definition.Opcode: Label, Operands: [var guardJoin] }
            || join.Value != guardJoin.Value || !IsGuardBif(11, failure: false)
            || body[first + 12] is not { Definition.Opcode: ExactEqualTest, Operands: [var accept, _, _] }
            || body[first + 13] is not { Definition.Opcode: ExactEqualTest, Operands: [var reject, _, _] }
            || body[first + 14] is not { Definition.Opcode: Label, Operands: [var accepted] }
            || accept.Value != accepted.Value || reject.Value.IsZero
            || body[argument] is not { Definition.Opcode: GetTupleElement, Operands: [var source, _, var firstArgument] }
            || source != carrier || !IsX(firstArgument, 0)
            || body[argument + 1] is not
            {
                Definition.Opcode: GetTupleElement,
                Operands: [var nextSource, _, { Kind: BeamOperandParameters.X, TypeIndex: null } nextArgument]
            } || nextSource != carrier || nextArgument.Value < 2 || nextArgument.Value >= FirstReservedXRegister
            || body[argument + 2] is not { Definition.Opcode: Move, Operands: [var retained, var secondArgument] }
            || retained != saved || !IsX(secondArgument, 1))
        {
            return null;
        }

        int afterArguments = argument + 3;
        var written = new HashSet<int> { 0, 1, (int)nextArgument.Value };
        while (afterArguments < body.Count && body[afterArguments] is
            {
                Definition.Opcode: Move,
                Operands: [
                {
                    Kind: BeamOperandParameters.Atom or BeamOperandParameters.Nil
                        or BeamOperandParameters.Integer or BeamOperandParameters.LiteralValue
                },
                { Kind: BeamOperandParameters.X, TypeIndex: null } destination
                ]
            })
        {
            if (destination.Value >= FirstReservedXRegister || !written.Add((int)destination.Value))
            {
                return null;
            }

            afterArguments++;
        }

        int call = afterArguments;
        while (call < body.Count && body[call].Definition.Opcode == Line)
        {
            call++;
        }

        if (call + 1 >= body.Count || body[call] is not
            { Definition.Opcode: LocalCall or ExternalCall, Operands: [var arity, _] }
            || arity.Value != written.Count || written.Any(argumentRegister => argumentRegister >= arity.Value))
        {
            return null;
        }

        int entry = first;
        while (entry > 0 && body[entry - 1].Definition.Opcode != FunctionInfo)
        {
            entry--;
        }

        int end = call + 1;
        while (end < body.Count && body[end].Definition.Opcode is not (FunctionInfo or EndCode))
        {
            end++;
        }

        if (Enumerable.Range(entry, end - entry).Any(index =>
            body[index].Definition.Opcode is TryEnter or CatchEnter or DebugLine or ExecutableLine))
        {
            return null;
        }

        flow ??= new BeamProjectionFlow(body);
        if (!flow.TryGetExclusiveForwardTarget(
            first + 2,
            first + 4,
            (int)bifLanding.Value,
            out int failed
        ) || failed != first + 8
            || !flow.TryGetExclusiveForwardTarget(
                first + 7,
                first + 9,
                (int)guardJoin.Value,
                out int joined
            )
            || joined != first + 10
            || !flow.TryGetExclusiveForwardTarget(
                first + 12,
                first + 13,
                (int)accepted.Value,
                out int picked
            )
            || picked != first + 14
            || !flow.TryGetForwardLabel(first + 13, (int)reject.Value, out int dropped)
            || dropped <= call || dropped >= end
            || !flow.IsInitialized(
                first,
                saved,
                allowFramePredicates: true,
                requireClearedSlot: true
            )
            || !flow.IsDead(
                call,
                saved,
                allowBadRecordExit: true,
                allowEntryFramePredicates: true
            )
            || !flow.IsDead(
                dropped,
                saved,
                allowBadRecordExit: true,
                afterCall: true,
                allowEntryFramePredicates: true
            )
            || body[first + 12].Operands.Skip(1).Concat(body[first + 13].Operands.Skip(1))
                .Any(operand => SameProjectionRegister(operand, saved)))
        {
            return null;
        }

        // The guard retains X0 and its two failure paths agree on the old
        // value. Once the other arguments have been loaded, only the parallel
        // X0/X1 copy is needed for the call. The obsolete Y root stays cleared.
        return (argument, afterArguments);

        bool IsGuardBif(int offset, bool failure) => body[first + offset] is
        {
            Definition.Opcode: BifTwo,
            Operands: [
            var rejected, _, var left, var right,
            { Kind: BeamOperandParameters.X, TypeIndex: null } destination
                ]
        }
            && (failure
                ? rejected.Kind == BeamOperandParameters.Label && rejected.Value == rejectedBif.Value
                : rejected.Kind == BeamOperandParameters.Failure && rejected.Value.IsZero)
            && !IsX(destination, 0) && !SameProjectionRegister(destination, carrier)
            && !SameProjectionRegister(left, saved) && !SameProjectionRegister(right, saved);

        static bool IsX(BeamOperand value, int register) =>
            value.Kind == BeamOperandParameters.X && value.TypeIndex is null && value.Value == register;
    }
}
