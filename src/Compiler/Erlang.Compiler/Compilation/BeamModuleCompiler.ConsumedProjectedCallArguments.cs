using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ClearConsumedProjectedCallArguments(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(GetTupleElement)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var body = decoded.Body;
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var x1 = new BeamOperand(BeamOperandParameters.X, 1);
        var x2 = new BeamOperand(BeamOperandParameters.X, 2);
        BeamProjectionFlow? flow = null;
        using var output = new MemoryStream();
        int copied = 0;
        int end = 0;
        bool protectedFunction = false;
        for (int first = 0; first + 5 < body.Count; first++)
        {
            if (body[first].Definition.Opcode == FunctionInfo)
            {
                end = first + 1;
                protectedFunction = false;
                while (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
                {
                    protectedFunction |= body[end].Definition.Opcode is TryEnter or CatchEnter or DebugLine or ExecutableLine;
                    end++;
                }
            }

            if (protectedFunction || first + 5 >= end
                || body[first] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } record, _, var projected]
                } || projected != x0
                || body[first + 1] is not
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } consumed, var second]
                } || second != x1 || consumed == record
                || body[first + 2] is not
                {
                    Definition.Opcode: Move,
                    Operands: [
                    {
                        Kind: BeamOperandParameters.Atom or BeamOperandParameters.Nil
                        or BeamOperandParameters.Integer or BeamOperandParameters.LiteralValue
                    }, var third]
                } || third != x2)
            {
                continue;
            }

            int call = first + 3;
            while (call < end && body[call].Definition.Opcode == Line)
            {
                call++;
            }

            if (call + 1 >= end
                || body[call] is not { Definition.Opcode: LocalCall or ExternalCall, Operands: [var arity, _] }
                || arity.Value != 3
                || body[call + 1] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var result, { Kind: BeamOperandParameters.Y, TypeIndex: null } overwritten]
                } || result != x0 || overwritten == consumed || overwritten == record)
            {
                continue;
            }

            flow ??= new BeamProjectionFlow(body);
            if (!flow.IsInitialized(first, overwritten) || !flow.IsDead(call, consumed, allowBadRecordExit: true))
            {
                continue;
            }

            // The constant argument is independent. Both old Y values are
            // dead across the unprotected call after X1 has been loaded.
            output.Write(code.Span[copied..body[first + 1].Offset]);
            output.Write(code.Span[body[first + 2].Offset..body[first + 3].Offset]);
            output.Write(code.Span[body[first + 1].Offset..body[first + 2].Offset]);
            int[] dead = [(int)consumed.Value, (int)overwritten.Value];
            Array.Sort(dead);
            WriteFrameInitialization(output, dead);
            copied = body[first + 3].Offset;
            first += 2;
        }

        if (copied == 0)
        {
            return null;
        }

        highestOpcode = Math.Max(highestOpcode, InitYRegisters);
        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
