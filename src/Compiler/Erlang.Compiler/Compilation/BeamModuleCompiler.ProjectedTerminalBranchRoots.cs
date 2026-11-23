using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ClearProjectedTerminalBranchRoots(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(GetTupleElement)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var body = decoded.Body;
        var saved = new BeamOperand(BeamOperandParameters.Y, 0);
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var x1 = new BeamOperand(BeamOperandParameters.X, 1);
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        for (int info = 0; info + 10 < body.Count; info++)
        {
            if (body[info].Definition.Opcode != FunctionInfo)
            {
                continue;
            }

            int entry = info + 1;
            int end = entry;
            while (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                end++;
            }

            (int[] Owners, Dictionary<int, bool[]> Uses)? ownership = null;
            for (int projection = entry + 2; projection + 8 < end; projection++)
            {
                if (body[projection - 2].Definition.Opcode != Label
                    || body[projection - 1] is not
                    {
                        Definition.Opcode: ExactEqualTest,
                        Operands: [_, var compared, { Kind: BeamOperandParameters.Atom }]
                    } || compared != x0
                    || body[projection] is not
                    {
                        Definition.Opcode: GetTupleElement,
                        Operands: [var record, _, var argument]
                    } || record != saved || argument != x1
                    || body[projection + 1] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [{ Kind: BeamOperandParameters.Atom }, var key]
                    } || key != x0)
                {
                    continue;
                }

                int call = projection + 2;
                while (call < end && body[call].Definition.Opcode == Line)
                {
                    call++;
                }

                if (call + 5 >= end || body[call] is not
                    {
                        Definition.Opcode: LocalCall or ExternalCall,
                        Operands: [var arity, _]
                    } || arity.Value != 2
                    || body[call + 1] is not
                    {
                        Definition.Opcode: ExactEqualTest,
                        Operands: [var alternative, var selected, { Kind: BeamOperandParameters.Atom }]
                    } || selected != x0 || ReadLiteralReturn(call + 2, end) is null)
                {
                    continue;
                }

                if ((ownership ??= ReadDisjointFrameOwnership(body, entry, end)) is not { } owner
                    || owner.Uses.Count != 1 || owner.Owners[projection - entry] != owner.Owners[call - entry]
                    || !owner.Uses.TryGetValue(owner.Owners[call - entry], out bool[]? slots) || slots.Length != 1)
                {
                    continue;
                }

                flow ??= new BeamProjectionFlow(body);
                if (!flow.TryGetSingleForwardTarget(call + 1, (int)alternative.Value, out int fallback)
                    || ReadLiteralReturn(fallback + 1, end) is null
                    || !flow.IsInitialized(projection, saved) || !flow.IsDead(call, saved))
                {
                    continue;
                }

                int offset = body[projection + 1].Offset;
                output.Write(code.Span[copied..offset]);
                WriteFrameInitialization(output, [0]);
                copied = offset;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);
        highestOpcode = Math.Max(highestOpcode, InitYRegisters);

        return output.ToArray();

        BeamOperand? ReadLiteralReturn(int index, int end)
        {
            if (index + 2 >= end || body[index] is not
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.Atom } literal, var target]
                } || target != x0 || body[index + 1] is not
                { Definition.Opcode: Deallocate, Operands: [{ Value.IsOne: true }] }
                || body[index + 2].Definition.Opcode != Return)
            {
                return null;
            }

            return literal;
        }
    }
}
