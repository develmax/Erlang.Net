using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? DelayFramePastNilTailCall(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(IsNil)
            || !code.Span.Contains(TailExternalCallWithFrame)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        int copied = 0;
        for (int info = 0; info + 8 < body.Count; info++)
        {
            if (body[info] is not { Definition.Opcode: FunctionInfo, Operands: [_, _, var arity] }
                || body[info + 1].Definition.Opcode != Label || body[info + 2] is not
                { Definition.Opcode: Allocate, Operands: [var width, var live] }
                || width.Value <= 0 || live.Value != arity.Value || !HasUnprotectedCallKillSchedule(body, info + 1))
            {
                continue;
            }

            int allocation = info + 2;
            int check = allocation + 1;
            if (body[check].Definition.Opcode == InitYRegisters)
            {
                check++;
            }

            var spills = new Dictionary<BeamOperand, BeamOperand>();
            while (check < body.Count && body[check] is
                { Definition.Opcode: Move, Operands: [var argument, { Kind: BeamOperandParameters.Y, TypeIndex: null } saved] }
                && IsBareX(argument) && argument.Value < live.Value && saved.Value < width.Value
                && spills.TryAdd(saved, argument))
            {
                check++;
            }

            if (check + 3 >= body.Count || body[check] is not
                { Definition.Opcode: IsNil, Operands: [var other, var subject] }
                || !IsBareX(subject) || subject.Value >= live.Value
                || body[check + 1] is not { Definition.Opcode: Move, Operands: [var savedValue, var destination] }
                || destination != x0 || !spills.TryGetValue(savedValue, out BeamOperand? original))
            {
                continue;
            }

            int call = check + 2;
            while (call < body.Count && body[call].Definition.Opcode == Line)
            {
                call++;
            }

            if (call + 2 >= body.Count || body[call] is not
                {
                    Definition.Opcode: TailExternalCallWithFrame,
                    Operands: [{ Value.IsOne: true }, var import, var release]
                } || release != width
                || !(flow ??= new BeamProjectionFlow(body)).TryGetSingleForwardTarget(check, (int)other.Value, out int fallback)
                || fallback != call + 1)
            {
                continue;
            }

            // The entry test only reads an unchanged argument. Its nil arm
            // needs one original formal, so only the retry owns the frame.
            output.Write(code.Span[copied..body[allocation].Offset]);
            output.Write(code.Span[body[check].Offset..body[check + 1].Offset]);
            if (original != x0)
            {
                output.WriteByte(Move);
                WriteRegister(output, original);
                WriteRegister(output, x0);
            }

            output.Write(code.Span[body[check + 2].Offset..body[call].Offset]);
            output.WriteByte(TailExternalCall);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, import.Value);
            output.Write(code.Span[body[fallback].Offset..body[fallback + 1].Offset]);
            output.Write(code.Span[body[allocation].Offset..body[check].Offset]);
            copied = body[fallback + 1].Offset;
            info = fallback;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
