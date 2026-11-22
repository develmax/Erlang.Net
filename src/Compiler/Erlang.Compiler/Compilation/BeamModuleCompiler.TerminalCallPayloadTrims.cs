using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record TerminalPayloadTrim(
        int Landing,
        int LastArgument,
        BeamOperand Payload,
        int Width
    );

    private byte[]? TrimTerminalCallPayloadFrames(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(TrimFrame)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var body = decoded.Body;
        BeamProjectionFlow? flow = null;
        using var output = new MemoryStream();
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
            if (Enumerable.Range(start, end - start).Any(index => body[index].Definition.Opcode is
                TryEnter or CatchEnter or DebugLine or ExecutableLine))
            {
                continue;
            }

            for (int call = start + 4; call + 5 < end; call++)
            {
                if (ReadTerminalPayloadTrim(
                    body,
                    start,
                    call,
                    end,
                    ref flow
                ) is not { } plan)
                {
                    continue;
                }

                int landing = body[plan.Landing + 1].Offset;
                output.Write(code.Span[copied..landing]);
                for (int index = plan.Landing + 1; index < call; index++)
                {
                    if (index == plan.LastArgument)
                    {
                        int last = plan.Width - 1;
                        if (plan.Payload.Value != last)
                        {
                            output.WriteByte(Move);
                            WriteRegister(output, plan.Payload);
                            BeamCompactWriter.Write(output, YRegisterTag, last);
                        }

                        WriteTrim(output, last, 1);
                    }

                    // Every cleared slot is about to disappear. The reader
                    // independently proves that argument preparation cannot read it.
                    if (body[index].Definition.Opcode != InitYRegisters)
                    {
                        output.Write(code.Span[body[index].Offset..body[index + 1].Offset]);
                    }
                }

                output.Write(code.Span[body[call].Offset..body[call + 2].Offset]);
                output.WriteByte(Move);
                BeamCompactWriter.Write(output, YRegisterTag, 0);
                BeamCompactWriter.Write(output, XRegisterTag, 0);
                WriteTrim(output, 1, 0);
                copied = body[call + 4].Offset;
                call += 4;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        static void WriteTrim(Stream output, int removed, int kept)
        {
            output.WriteByte(TrimFrame);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, removed);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, kept);
        }
    }

    private static TerminalPayloadTrim? ReadTerminalPayloadTrim(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int call,
        int end,
        ref BeamProjectionFlow? flow
    )
    {
        if (!IsBinaryOrdinaryCall(body[call]) || body[call + 1] is not
            {
                Definition.Opcode: Move,
                Operands: [
                { Kind: BeamOperandParameters.X, TypeIndex: null, Value.IsZero: true },
                { Kind: BeamOperandParameters.X, TypeIndex: null, Value.IsOne: true }
                ]
            } || body[call + 2] is not
            {
                Definition.Opcode: Move,
                Operands: [
                { Kind: BeamOperandParameters.Y, TypeIndex: null } payload,
                { Kind: BeamOperandParameters.X, TypeIndex: null, Value.IsZero: true }
                ]
            } || body[call + 3] is not
            {
                Definition.Opcode: TrimFrame,
                Operands: [var width, { Value.IsZero: true }]
            } || width.Value <= 1 || width.Value > BeamOperandParameters.MaximumRegister
            || payload.Value >= width.Value)
        {
            return null;
        }

        int nextCall = call + 4;
        while (nextCall < end && body[nextCall].Definition.Opcode == Line)
        {
            nextCall++;
        }

        if (nextCall >= end || !IsBinaryOrdinaryCall(body[nextCall]))
        {
            return null;
        }

        int landing = call - 1;
        while (landing > start && body[landing].Definition.Opcode is Line or Move or InitYRegisters or GetTupleElement)
        {
            landing--;
        }

        if (landing <= start || !EndsBeamBlock(body[landing - 1].Definition.Opcode)
            || body[landing] is not { Definition.Opcode: Label, Operands: [var label] })
        {
            return null;
        }

        int literal = -1;
        var cleared = new HashSet<int>();
        for (int index = landing + 1; index < call; index++)
        {
            BeamInstruction instruction = body[index];
            if (instruction.Definition.Opcode == Line)
            {
                continue;
            }

            if (instruction.Definition.Opcode == InitYRegisters)
            {
                foreach (BeamOperand slot in instruction.Operands.Skip(1))
                {
                    if (SameProjectionRegister(slot, payload) || slot.Value >= width.Value)
                    {
                        return null;
                    }

                    cleared.Add((int)slot.Value);
                }

                continue;
            }

            if (literal >= 0 || instruction.Operands.Any(operand =>
                operand.Kind == BeamOperandParameters.Y && (operand.Value >= width.Value || cleared.Contains((int)operand.Value))))
            {
                return null;
            }

            var target = instruction.Operands[^1];
            if (target is not { Kind: BeamOperandParameters.X, TypeIndex: null })
            {
                return null;
            }

            if (target.Value.IsZero && instruction is
                {
                    Definition.Opcode: Move,
                    Operands: [
                    {
                        Kind: BeamOperandParameters.Atom or BeamOperandParameters.Integer
                            or BeamOperandParameters.Nil or BeamOperandParameters.LiteralValue
                    }, _
                    ]
                })
            {
                literal = index;
            }
            else if (!target.Value.IsOne)
            {
                return null;
            }
        }

        if (literal < 0)
        {
            return null;
        }

        flow ??= new BeamProjectionFlow(body);
        for (int index = start; index < landing; index++)
        {
            if (body[index] is
                {
                    Definition.Opcode: ExactEqualTest or ExactNotEqualTest,
                    Operands: [var rejected, _, _]
                } && rejected.Value == label.Value
                && flow.TryGetSingleForwardTarget(index, (int)label.Value, out int target)
                && target == landing)
            {
                return new TerminalPayloadTrim(
                    landing,
                    literal,
                    payload,
                    (int)width.Value
                );
            }
        }

        return null;

        static bool IsBinaryOrdinaryCall(BeamInstruction instruction) => instruction is
        { Definition.Opcode: LocalCall or ExternalCall, Operands: [{ Value: var arity }, _] } && arity == 2;
    }
}
