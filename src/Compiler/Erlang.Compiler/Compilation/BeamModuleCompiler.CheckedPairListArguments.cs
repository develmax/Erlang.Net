using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ScheduleCheckedPairListArguments(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(PutList)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var x1 = new BeamOperand(BeamOperandParameters.X, 1);
        var nil = new BeamOperand(BeamOperandParameters.Nil, NilAtomIndex);
        BeamProjectionFlow? flow = null;
        using var output = new MemoryStream();
        int copied = 0;
        for (int start = 1; start + 14 < body.Count; start++)
        {
            if (body[start - 1] is not
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [{ Kind: BeamOperandParameters.Label, Value.Sign: > 0 } rejected, var selector, { Value: var width }, _]
                }
                || width != 2 || selector is not { Kind: BeamOperandParameters.Y, TypeIndex: null }
                || body[start] is not { Definition.Opcode: GetTupleElement, Operands: [var parent, { Value.IsOne: true }, var pair] }
                || parent != selector || pair != x1
                || body[start + 1] is not { Definition.Opcode: IsTuple, Operands: [var tupleFailure, var checkedPair] }
                || tupleFailure != rejected || checkedPair != pair
                || body[start + 2] is not
                { Definition.Opcode: TestArity, Operands: [var arityFailure, var arityPair, { Value: var arity }] }
                || arityFailure != rejected || arityPair != pair || arity != 2
                || body[start + 3] is not
                { Definition.Opcode: GetTupleElement, Operands: [var firstPair, { Value.IsZero: true }, var first] }
                || firstPair != pair || first is not { Kind: BeamOperandParameters.Y, TypeIndex: null }
                || body[start + 4] is not
                { Definition.Opcode: GetTupleElement, Operands: [var secondPair, { Value.IsOne: true }, var second] }
                || secondPair != pair || second is not { Kind: BeamOperandParameters.Y, TypeIndex: null }
                || first == second || first == selector || second == selector
                || body[start + 5] is not
                { Definition.Opcode: TestHeap, Operands: [{ Kind: BeamOperandParameters.Unsigned, Value: var heap }, { Value.IsZero: true }] }
                || heap != 2 * ConsHeapWords
                || body[start + 6] is not { Definition.Opcode: PutList, Operands: [var tailValue, var empty, var suffix] }
                || tailValue != second || empty != nil || suffix != x0
                || body[start + 7] is not { Definition.Opcode: PutList, Operands: [var headValue, var tail, var list] }
                || headValue != first || tail != suffix || list != x1
                || body[start + 8] is not
                {
                    Definition.Opcode: Move,
                    Operands: [
                    {
                        Kind: BeamOperandParameters.Atom or BeamOperandParameters.Integer or BeamOperandParameters.Nil
                        or BeamOperandParameters.LiteralValue, TypeIndex: null
                    }, var leading]
                }
                || leading != x0)
            {
                continue;
            }

            int call = start + 9;
            int trim = -1;
            BeamOperand returnSource = selector;
            if (body[call] is
                {
                    Definition.Opcode: Move,
                    Operands: [var saved, { Kind: BeamOperandParameters.Y, TypeIndex: null } remapped]
                } && saved == selector && body[call + 1] is
                {
                    Definition.Opcode: TrimFrame,
                    Operands: [{ Value: var dropped }, { Value.IsOne: true }]
                } && dropped == remapped.Value && selector.Value < dropped)
            {
                trim = call + 1;
                call += 2;
                returnSource = new BeamOperand(BeamOperandParameters.Y, 0);
            }

            if (body[call].Definition.Opcode == Line)
            {
                call++;
            }

            if (call + 3 >= body.Count || body[call] is not
                { Definition.Opcode: LocalCall or ExternalCall, Operands: [{ Value: var callArity }, _] }
                || callArity != 2 || body[call + 1] is not
                { Definition.Opcode: Move, Operands: [var returned, var returnRegister] }
                || returned != returnSource || returnRegister != x0 || body[call + 2].Definition.Opcode != Deallocate
                || trim >= 0 && !body[call + 2].Operands[0].Value.IsOne
                || body[call + 3].Definition.Opcode != Return || !HasDedicatedPairSlots(
                    body,
                    start,
                    first,
                    second,
                    trim,
                    call + 1
                ))
            {
                continue;
            }

            flow ??= new BeamProjectionFlow(body);
            if (!flow.IsGuardTemporaryDeadOnFailure(start + 1, x0, preserveHeapRoots: true)
                || !flow.IsGuardTemporaryDeadOnFailure(start + 2, x0, preserveHeapRoots: true)
                || !flow.IsGuardTemporaryDeadOnFailure(start + 1, x1, preserveHeapRoots: true)
                || !flow.IsGuardTemporaryDeadOnFailure(start + 2, x1, preserveHeapRoots: true))
            {
                continue;
            }

            // The saved outer value is untouched on rejection and after the call.
            // Root its checked payload in X0 through reservation, then load
            // each field directly into the outgoing list's register pair.
            output.Write(code.Span[copied..body[start].Offset]);
            WriteProjection(selector, 1, x0);
            output.WriteByte(IsTuple);
            BeamCompactWriter.Write(output, LabelTag, rejected.Value);
            WriteRegister(output, x0);
            output.WriteByte(TestArity);
            BeamCompactWriter.Write(output, LabelTag, rejected.Value);
            WriteRegister(output, x0);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 2);
            WriteHeapRoots(
                output,
                code,
                body[start + 5].Offset,
                body[start + 6].Offset,
                0,
                1
            );
            WriteProjection(x0, 0, x1);
            WriteProjection(x0, 1, x0);
            output.WriteByte(PutList);
            WriteRegister(output, x0);
            BeamCompactWriter.Write(output, AtomTag, NilAtomIndex);
            WriteRegister(output, x0);
            output.WriteByte(PutList);
            WriteRegister(output, x1);
            WriteRegister(output, x0);
            WriteRegister(output, x1);
            copied = body[start + 8].Offset;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        void WriteProjection(BeamOperand source, int index, BeamOperand target)
        {
            output.WriteByte(GetTupleElement);
            WriteRegister(output, source);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, index);
            WriteRegister(output, target);
        }
    }

    private static bool HasDedicatedPairSlots(
        IReadOnlyList<BeamInstruction> body,
        int projection,
        BeamOperand first,
        BeamOperand second,
        int trim,
        int restore
    )
    {
        int begin = projection - 1;
        while (begin > 0 && body[begin - 1].Definition.Opcode != FunctionInfo)
        {
            begin--;
        }

        if (body[begin].Definition.Opcode != Label)
        {
            return false;
        }

        int allocation = begin + 1;
        if (body[allocation].Definition.Opcode == OnLoad)
        {
            allocation++;
        }

        if (allocation >= projection || body[allocation] is not
            { Definition.Opcode: Allocate, Operands: [var width, _] } || first.Value >= width.Value || second.Value >= width.Value)
        {
            return false;
        }

        if (trim >= 0 && body[trim].Operands[0].Value + 1 != width.Value)
        {
            return false;
        }

        int initialized = allocation + 1;
        while (initialized < projection && (body[initialized].Definition.Opcode is Move or Line
            || body[initialized] is { Definition.Opcode: BifOne, Operands: [{ Value.IsZero: true }, ..] }))
        {
            initialized++;
        }

        if (initialized >= projection || body[initialized].Definition.Opcode != InitYRegisters
            || !body[initialized].Operands.Contains(first) || !body[initialized].Operands.Contains(second))
        {
            return false;
        }

        for (int index = allocation + 1; index < body.Count && body[index].Definition.Opcode is not (FunctionInfo or EndCode); index++)
        {
            BeamInstruction instruction = body[index];
            if (trim >= 0 && (index == trim - 1 || index == trim || index == restore))
            {
                // This copy overwrites the sole surviving slot, and restore
                // reads that slot in the new frame numbering.
                continue;
            }

            if (instruction.Definition.Opcode is Allocate or AllocateHeap or TrimFrame or CatchEnter or TryEnter
                or LoopReceive or DebugLine or ExecutableLine)
            {
                return false;
            }

            if (index != projection + 3 && index != projection + 4
                && index != projection + 6 && index != projection + 7 && instruction.Definition.Opcode != InitYRegisters
                && instruction.Operands.Any(operand => SameProjectionRegister(operand, first) || SameProjectionRegister(operand, second)))
            {
                return false;
            }
        }

        return true;
    }
}
