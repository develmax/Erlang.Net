using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ShareCommonCheckedTupleProjection(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(SelectValue)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var body = decoded.Body;
        BeamProjectionFlow? flow = null;
        var projections = new Dictionary<int, (BeamOperand Tuple, BeamOperand Field, BeamOperand Destination)>();
        var copies = new Dictionary<int, (BeamOperand Source, BeamOperand Destination)>();
        int functionEnd = 0;
        bool protectedFunction = false;
        for (int index = 0; index + 1 < body.Count; index++)
        {
            if (body[index].Definition.Opcode == FunctionInfo)
            {
                functionEnd = index + 1;
                protectedFunction = false;
                while (body[functionEnd].Definition.Opcode is not (FunctionInfo or EndCode))
                {
                    protectedFunction |= body[functionEnd].Definition.Opcode is TryEnter or CatchEnter or DebugLine or ExecutableLine;
                    functionEnd++;
                }
            }

            if (protectedFunction || index + 3 >= functionEnd
                || body[index] is not { Definition.Opcode: IsTuple, Operands: [var failure, var tuple] }
                || tuple is not { Kind: BeamOperandParameters.X, TypeIndex: null }
                || body[index + 1] is not
                { Definition.Opcode: TestArity, Operands: [var rejected, var subject, var arity] }
                || failure != rejected || subject != tuple
                || body[index + 2] is not
                { Definition.Opcode: GetTupleElement, Operands: [var projected, { Value.IsZero: true }, var tag] }
                || projected != tuple || tag is not { Kind: BeamOperandParameters.X, TypeIndex: null } || tag == tuple
                || body[index + 3] is not
                { Definition.Opcode: SelectValue, Operands: [var selector, var otherwise, _, _, _, _, _, ..] }
                || selector != tag || otherwise != failure)
            {
                continue;
            }

            var register = System.Numerics.BigInteger.Max(tuple.Value, tag.Value) + 1;
            if (register >= FirstReservedXRegister)
            {
                continue;
            }

            var scratch = new BeamOperand(BeamOperandParameters.X, register);
            int dispatch = index + 3;
            var keys = body[dispatch].Operands;
            BeamOperand? field = null;
            var reads = new Dictionary<int, BeamOperand>();
            for (int arm = 3; arm + 1 < keys.Count; arm += 2)
            {
                if (keys[arm].Kind != BeamOperandParameters.Atom
                    || keys[arm + 1] is not { Kind: BeamOperandParameters.Label } branch || branch == otherwise
                    || !(flow ??= new BeamProjectionFlow(body)).TryGetSingleForwardTarget(dispatch, (int)branch.Value, out int entry)
                    || entry + 1 >= functionEnd
                    || body[entry + 1] is not
                    { Definition.Opcode: GetTupleElement, Operands: [var source, var offset, var destination] }
                    || source != tuple || offset.Value <= 0 || offset.Value >= arity.Value
                    || field is { } shared && shared != offset || !IsBareProjectionRegister(destination)
                    || !reads.TryAdd(entry + 1, destination))
                {
                    break;
                }

                field = offset;
            }

            if (field is not { } payload || reads.Count * 2 != keys.Count - 3
                || !flow!.IsBranchTemporaryDead(dispatch, scratch))
            {
                continue;
            }

            // Every arm already reads this field from the checked tuple.
            // The new X value is dead along both the arms and the default edge
            // until the original projection defines or consumes it.
            projections.Add(dispatch, (tuple, payload, scratch));
            foreach (var (read, destination) in reads)
            {
                copies.Add(read, (scratch, destination));
            }
        }

        if (projections.Count == 0)
        {
            return null;
        }

        using var output = new MemoryStream();
        for (int index = 0; index + 1 < body.Count; index++)
        {
            if (projections.TryGetValue(index, out var projection))
            {
                output.WriteByte(GetTupleElement);
                WriteRegister(output, projection.Tuple);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, projection.Field.Value);
                WriteRegister(output, projection.Destination);
            }

            if (copies.TryGetValue(index, out var copy))
            {
                if (copy.Source != copy.Destination)
                {
                    output.WriteByte(Move);
                    WriteRegister(output, copy.Source);
                    WriteRegister(output, copy.Destination);
                }
            }
            else
            {
                output.Write(code.Span[body[index].Offset..body[index + 1].Offset]);
            }
        }

        output.Write(code.Span[body[^1].Offset..]);

        return output.ToArray();
    }
}
