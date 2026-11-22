using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? SwapClearedStackCallArguments(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(InitYRegisters)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        using var output = new MemoryStream();
        int copied = 0;
        for (int first = 0; first + 6 < body.Count; first++)
        {
            if (body[first] is not
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.X, TypeIndex: null } left,
                    { Kind: BeamOperandParameters.Y, TypeIndex: null } temporary]
                }
                || body[first + 1] is not
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.X, TypeIndex: null } right, var destination]
                } || destination != left || left.Value >= right.Value
                || body[first + 2] is not
                {
                    Definition.Opcode: Move, Operands: [var saved, var outgoing]
                } || saved != temporary || outgoing != right)
            {
                continue;
            }

            int clear = first + 3;
            var constants = new SortedDictionary<int, int>();
            while (body[clear] is
                {
                    Definition.Opcode: Move,
                    Operands: [var literal, { Kind: BeamOperandParameters.X, TypeIndex: null } target]
                } && IsNonRegisterValue(literal) && target != left && target != right
                && constants.TryAdd((int)target.Value, clear))
            {
                clear++;
            }

            if (constants.Count == 0 || body[clear].Definition.Opcode != InitYRegisters
                || !body[clear].Operands.Contains(temporary))
            {
                continue;
            }

            int call = clear + 1;
            while (body[call].Definition.Opcode == Line)
            {
                call++;
            }

            if (body[call] is not
                { Definition.Opcode: LocalCall or ExternalCall, Operands: [var arity, _] }
                || right.Value >= arity.Value || constants.Keys.Last() >= arity.Value
                || !HasUnprotectedCheckedFailureRegion(body, first, call))
            {
                continue;
            }

            // A plain literal move does not collect or observe the scratch.
            // The existing clear still initializes its slot for the call.
            output.Write(code.Span[copied..body[first].Offset]);
            foreach (int constant in constants.Values.Reverse())
            {
                output.Write(code.Span[body[constant].Offset..body[constant + 1].Offset]);
            }

            output.WriteByte(Swap);
            WriteRegister(output, left);
            WriteRegister(output, right);
            copied = body[clear].Offset;
            first = call;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
