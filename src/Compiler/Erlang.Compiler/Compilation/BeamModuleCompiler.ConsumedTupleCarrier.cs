using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ReuseConsumedTupleCarrierInCallContinuation(
        ReadOnlyMemory<byte> code,
        int labelCount
    )
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(TrimFrame)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        return RewriteConsumedTupleCarrier(code, body);
    }

    private static byte[]? RewriteConsumedTupleCarrier(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var x1 = new BeamOperand(BeamOperandParameters.X, 1);
        var y0 = new BeamOperand(BeamOperandParameters.Y, 0);
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        int entry = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = end + 1;
            for (int extract = start + 3; extract + 13 < end; extract++)
            {
                if (ReadContinuation(start, extract, end) is not { } plan)
                {
                    continue;
                }

                // Read the final carrier field before reusing its slot for
                // the projected result. The old projection slot now saves
                // the action result on both sides of the status test.
                CopyTo(body[extract].Offset);
                WriteInstruction(extract + 1);
                output.WriteByte(GetTupleElement);
                WriteRegister(x0);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, body[extract].Operands[1].Value);
                WriteRegister(plan.Carrier);
                WriteMove(plan.Carrier, x0);
                copied = body[extract + 3].Offset;
                CopyTo(body[plan.Action + 1].Offset);
                WriteMove(x0, plan.Scratch);

                // Pass the retained output to cleanup and keep the action
                // result in the only slot surviving the existing trim.
                CopyTo(body[plan.Failure + 1].Offset);
                WriteMove(plan.Carrier, x0);
                WriteMove(plan.Scratch, plan.Carrier);
                copied = body[plan.Failure + 3].Offset;
                break;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        CopyTo(code.Length);

        return output.ToArray();

        (int Action, int Failure, BeamOperand Carrier, BeamOperand Scratch)? ReadContinuation(
            int start,
            int extract,
            int end
        )
        {
            if (body[extract] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var result, { Kind: BeamOperandParameters.Unsigned }, var scratch]
                }
                || result != x0 || scratch != y0
                || body[extract + 1] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } carrier, _, var second]
                }
                || carrier == scratch || second != x1 || !IsMove(extract + 2, scratch, x0))
            {
                return null;
            }

            int check = extract - 1;
            if (body[check] is { Definition.Opcode: Move })
            {
                check--;
            }

            int capture = check == extract - 1 ? check - 1 : check + 1;
            int producer = extract - 3;
            if (!IsDirectCall(producer, null) || body[check] is not
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [_, var tested, _, _]
                } || tested != x0 || body[capture] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var returned, { Kind: BeamOperandParameters.Y, TypeIndex: null } retained]
                } || returned != x0 || retained == scratch || retained == carrier)
            {
                return null;
            }

            int action = SkipLines(extract + 3, end);
            int test = action + 1;
            if (test + 3 >= end || !IsDirectCall(action, 2) || body[test] is not
                {
                    Definition.Opcode: ExactEqualTest,
                    Operands: [{ Kind: BeamOperandParameters.Label } failed, var status, var expected]
                } || status != x0 || !IsNonRegisterValue(expected)
                || !IsMove(test + 1, retained, x0)
                || body[test + 2] is not { Definition.Opcode: Deallocate, Operands: [var width] }
                || width.Value != carrier.Value + 1 || body[test + 3].Definition.Opcode != Return
                || !HasUnprotectedCallKillSchedule(body, start)
                || !(flow ??= new BeamProjectionFlow(body)).TryGetSingleForwardTarget(
                    test,
                    (int)failed.Value,
                    out int failure
                )
                || failure < test + 4 || failure + 4 >= end
                || !IsMove(failure + 1, x0, carrier) || !IsMove(failure + 2, scratch, x0)
                || body[failure + 3] is not
                {
                    Definition.Opcode: TrimFrame,
                    Operands: [var removed, { Value.IsOne: true }]
                } || removed.Value != carrier.Value)
            {
                return null;
            }

            int cleanup = SkipLines(failure + 4, end);
            if (cleanup + 3 >= end || !IsDirectCall(cleanup, 1) || !IsMove(cleanup + 1, y0, x0)
                || body[cleanup + 2] is not { Definition.Opcode: Deallocate, Operands: [{ Value.IsOne: true }] }
                || body[cleanup + 3].Definition.Opcode != Return
                || ReadDisjointFrameOwnership(
                    body,
                    start,
                    end,
                    trackSiblingTrimEpochs: true
                ) is not { } frame)
            {
                return null;
            }

            int owner = frame.Owners[extract - start];
            if (owner < 0 || frame.Owners[failure - start] != owner || owner + 2 > producer
                || body[owner].Definition.Opcode is not (Allocate or AllocateHeap)
                || frame.Uses[owner].Length != width.Value || body[owner + 1].Definition.Opcode != InitYRegisters
                || !body[owner + 1].Operands.Skip(1).Any(operand => SameProjectionRegister(operand, scratch)))
            {
                return null;
            }

            for (int index = owner + 2; index < extract; index++)
            {
                if (body[index].Operands.Any(operand => SameProjectionRegister(operand, scratch)))
                {
                    return null;
                }
            }

            return (action, failure, carrier, scratch);
        }

        bool IsMove(int index, BeamOperand source, BeamOperand target) =>
            body[index] is { Definition.Opcode: Move, Operands: [var read, var written] }
            && read == source && written == target;

        bool IsDirectCall(int index, int? arguments) =>
            body[index] is { Definition.Opcode: LocalCall or ExternalCall, Operands: [var arity, _] }
            && (arguments is null || arity.Value == arguments.Value);

        int SkipLines(int index, int end)
        {
            while (index < end && body[index].Definition.Opcode == Line)
            {
                index++;
            }

            return index;
        }

        void CopyTo(int next)
        {
            output.Write(code.Span[copied..next]);
            copied = next;
        }

        void WriteInstruction(int index) =>
            output.Write(code.Span[body[index].Offset..body[index + 1].Offset]);

        void WriteRegister(BeamOperand operand) =>
            BeamCompactWriter.Write(output, operand.Kind == BeamOperandParameters.X ? XRegisterTag : YRegisterTag, operand.Value);

        void WriteMove(BeamOperand source, BeamOperand target)
        {
            output.WriteByte(Move);
            WriteRegister(source);
            WriteRegister(target);
        }
    }
}
