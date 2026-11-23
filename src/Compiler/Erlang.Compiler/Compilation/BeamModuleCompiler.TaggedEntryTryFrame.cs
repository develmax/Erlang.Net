using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? PlaceTaggedEntryTailBeforeTryFrame(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(TryEnter)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        const int argumentCount = 2;
        const int frameWidth = 2;
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        for (int info = 0; info + 15 < body.Count; info++)
        {
            if (body[info] is not
                { Definition.Opcode: FunctionInfo, Operands: [_, _, { Value.IsOne: true }] }
                || body[info + 1].Definition.Opcode != Label)
            {
                continue;
            }

            int allocation = info + 2;
            if (body[allocation] is not
                { Definition.Opcode: Allocate, Operands: [var size, { Value.IsOne: true }] }
                || size.Value != frameWidth)
            {
                continue;
            }

            int save = allocation + 1;
            BeamInstruction? initialize = body[save].Definition.Opcode == InitYRegisters ? body[save++] : null;
            int predicate = save + 1;
            int tail = predicate + 4;
            int fallback = tail + 1;
            int protect = fallback + 1;
            int restore = protect + 1;
            if (body[save] is not
                { Definition.Opcode: Move, Operands: [var input, { Kind: BeamOperandParameters.Y, TypeIndex: null } carrier] }
                || !IsBareX0(input) || carrier.Value >= frameWidth
                || body[predicate] is not
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [{ Kind: BeamOperandParameters.Label } rejected, var checkedSource, var arity,
                    { Kind: BeamOperandParameters.Atom } tag]
                } || checkedSource != carrier
                || body[predicate + 1] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var firstSource, var first, { Kind: BeamOperandParameters.Y, TypeIndex: null } temporary]
                } || firstSource != carrier || temporary == carrier || temporary.Value >= frameWidth
                || body[predicate + 2] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var secondSource, var second, { Kind: BeamOperandParameters.X, Value.IsOne: true, TypeIndex: null }]
                } || secondSource != carrier || first == second || first.Value <= 0 || second.Value <= 0
                || first.Value >= arity.Value || second.Value >= arity.Value
                || !IsMove(body[predicate + 3], temporary, input)
                || body[tail] is not
                {
                    Definition.Opcode: TailLocalCallWithFrame,
                    Operands: [var args, { Kind: BeamOperandParameters.Label } target, var width]
                } || args.Value != argumentCount || width != size
                || body[fallback] is not { Definition.Opcode: Label, Operands: [var entry] } || entry.Value != rejected.Value
                || body[protect] is not { Definition.Opcode: TryEnter, Operands: [var token, var landing] }
                || token != temporary || (initialize is not null && !initialize.Operands.Contains(token))
                || (initialize is not null && initialize.Operands.Count(operand => operand.Kind == BeamOperandParameters.Y) != 1)
                || !IsMove(body[restore], carrier, input))
            {
                continue;
            }

            int call = restore + 1;
            if (body[call].Definition.Opcode == Line)
            {
                call++;
            }

            if (body[call] is not { Definition.Opcode: ExternalCall, Operands: [{ Value.IsOne: true }, _] }
                || !(flow ??= new BeamProjectionFlow(body)).TryGetSingleForwardTarget(
                    predicate,
                    (int)rejected.Value,
                    out int soleFallback
                ) || soleFallback != fallback
                || !TryReadOwnedTryFallback(
                    body,
                    flow,
                    protect,
                    landing,
                    token
                ))
            {
                continue;
            }

            // Record fields are pure after the exact tag/arity check. Finish the
            // selected local tail before acquiring the protected sibling's frame.
            output.Write(code.Span[copied..body[allocation].Offset]);
            output.WriteByte(IsTaggedTuple);
            BeamCompactWriter.Write(output, LabelTag, rejected.Value);
            WriteRegister(output, input);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, arity.Value);
            BeamCompactWriter.Write(output, AtomTag, tag.Value);
            WriteField(first, 1);
            WriteField(second, 0);
            output.WriteByte(Swap);
            BeamCompactWriter.Write(output, XRegisterTag, 0);
            BeamCompactWriter.Write(output, XRegisterTag, 1);
            output.WriteByte(TailLocalCall);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, argumentCount);
            BeamCompactWriter.Write(output, LabelTag, target.Value);

            output.Write(code.Span[body[fallback].Offset..body[protect].Offset]);
            output.Write(code.Span[body[allocation].Offset..body[allocation + 1].Offset]);
            output.Write(code.Span[body[save].Offset..body[predicate].Offset]);
            output.Write(code.Span[body[protect].Offset..body[restore].Offset]);
            copied = body[restore + 1].Offset;

            void WriteField(BeamOperand field, int outputRegister)
            {
                output.WriteByte(GetTupleElement);
                WriteRegister(output, input);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, field.Value);
                BeamCompactWriter.Write(output, XRegisterTag, outputRegister);
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static bool IsMove(BeamInstruction instruction, BeamOperand source, BeamOperand destination) =>
        instruction is { Definition.Opcode: Move, Operands: [var from, var to] } && from == source && to == destination;

    private static bool TryReadOwnedTryFallback(
        IReadOnlyList<BeamInstruction> body,
        BeamProjectionFlow flow,
        int protect,
        BeamOperand handler,
        BeamOperand token
    )
    {
        if (handler.Kind != BeamOperandParameters.Label
            || !flow.TryGetSingleForwardTarget(protect, (int)handler.Value, out int landing))
        {
            return false;
        }

        int end = protect + 1;
        while (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
        {
            end++;
        }

        if (landing + 1 >= end || body[landing + 1] is not { Definition.Opcode: TryCase, Operands: [var tag] }
            || tag != token)
        {
            return false;
        }

        var labels = body.Skip(protect).Take(end - protect).Where(instruction => instruction.Definition.Opcode == Label)
            .Select(instruction => instruction.Operands[0].Value).ToHashSet();
        int normal = 0;
        for (int index = protect + 1; index < end; index++)
        {
            BeamInstruction instruction = body[index];
            byte opcode = instruction.Definition.Opcode;
            if (opcode is TryEnter or CatchEnter or CatchEnd or TryCaseEnd or Allocate or AllocateHeap or TrimFrame
                    or DebugLine or ExecutableLine
                || opcode == TryCase && index != landing + 1
                || opcode is not (LocalCall or TailLocalCall or TailLocalCallWithFrame)
                    && instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.Label
                        && !labels.Contains(operand.Value)))
            {
                return false;
            }

            if (opcode == TryEnd)
            {
                if (instruction.Operands[0] != token)
                {
                    return false;
                }

                normal++;
            }
        }

        return normal == 1;
    }
}
