using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? FuseTerminalLiteralMapBranches(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(PutMapAssoc)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        BeamProjectionFlow? flow = null;
        using var output = new MemoryStream();
        int copied = 0;
        for (int index = 0; index + 10 < body.Count; index++)
        {
            if (body[index] is not
                {
                    Definition.Opcode: PutMapAssoc,
                    Operands: [
                    { Kind: BeamOperandParameters.Failure },
                    { Kind: BeamOperandParameters.LiteralValue } original,
                    { Kind: BeamOperandParameters.X, TypeIndex: null, Value.IsZero: true } result,
                    { Kind: BeamOperandParameters.Unsigned } live,
                    { Kind: BeamOperandParameters.Count }, _, _, ..
                    ]
                } initial || live.Value.Sign <= 0 || live.Value + 1 >= BeamOperandParameters.MaximumRegister
                || body[index + 1] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var returned, { Kind: BeamOperandParameters.X, TypeIndex: null } stash]
                } || returned != result || stash == result
                || body[index + 2] is not
                {
                    Definition.Opcode: GetMapElements,
                    Operands: [
                    { Kind: BeamOperandParameters.Label, Value.Sign: > 0 } missing,
                    { Kind: BeamOperandParameters.X } map,
                    { Kind: BeamOperandParameters.Count },
                    { Kind: BeamOperandParameters.Atom } additionalKey,
                        var lookupResult
                    ]
                } || lookupResult != result || SameProjectionRegister(map, result) || SameProjectionRegister(map, stash)
                || map.Value >= live.Value
                || body[index + 3] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var recalled, { Kind: BeamOperandParameters.X, TypeIndex: null } field]
                } || recalled != result || field == stash || field == result
                || body[index + 4] is not
                {
                    Definition.Opcode: ExactEqualTest,
                    Operands: [
                    { Kind: BeamOperandParameters.Label, Value.Sign: > 0 } update,
                        var selected,
                    { Kind: BeamOperandParameters.Atom } sentinel
                    ]
                } || selected != field || update == missing
                || body[index + 5] is not { Definition.Opcode: Move, Operands: [var saved, var outputRegister] }
                || saved != stash || outputRegister != result
                || body[index + 6].Definition.Opcode != Return
                || body[index + 7] is not { Definition.Opcode: Label, Operands: [var updateLabel] }
                || update.Value != updateLabel.Value
                || body[index + 8] is not
                {
                    Definition.Opcode: PutMapAssoc,
                    Operands: [
                    { Kind: BeamOperandParameters.Failure },
                        var currentBase,
                        var updateDestination,
                    { Kind: BeamOperandParameters.Unsigned },
                    { Kind: BeamOperandParameters.Count },
                        var updateKey,
                        var updateValue
                    ]
                } || currentBase != stash || updateDestination != result
                || updateKey != additionalKey || updateValue != field
                || body[index + 9].Definition.Opcode != Return)
            {
                continue;
            }

            var fields = initial.Operands.Skip(5).Chunk(2).ToArray();
            if (fields.Any(pair => pair is not
                [
                { Kind: BeamOperandParameters.Atom },
                { Kind: BeamOperandParameters.X, TypeIndex: null }
                ] || pair[0] == additionalKey || pair[1].Value >= live.Value)
                || fields.Select(pair => pair[0]).Distinct().Count() != fields.Length)
            {
                continue;
            }

            int resultRegister = (int)live.Value;
            var addedResult = new BeamOperand(BeamOperandParameters.X, resultRegister);
            var addedField = new BeamOperand(BeamOperandParameters.X, resultRegister + 1);
            flow ??= new BeamProjectionFlow(body);
            if (!flow.TryGetSingleForwardTarget(index + 4, (int)update.Value, out int target) || target != index + 7
                || !flow.IsGuardTemporaryDeadOnFailure(index + 2, result, preserveHeapRoots: true)
                || !flow.IsGuardTemporaryDeadOnFailure(index + 2, stash, preserveHeapRoots: true)
                || !flow.IsGuardTemporaryDeadOnFailure(index + 2, addedResult, preserveHeapRoots: true)
                || !flow.IsGuardTemporaryDeadOnFailure(index + 2, addedField, preserveHeapRoots: true))
            {
                continue;
            }

            output.Write(code.Span[copied..initial.Offset]);
            WriteConstructor(addedResult, resultRegister, appendField: false);
            output.WriteByte(GetMapElements);
            BeamCompactWriter.Write(output, LabelTag, missing.Value);
            if (map.TypeIndex is not null)
            {
                output.WriteByte(ExtendedTypedRegisterOperand);
            }

            WriteRegister(output, map);
            if (map.TypeIndex is int type)
            {
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, type);
            }

            output.WriteByte(ExtendedListOperand);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 2);
            BeamCompactWriter.Write(output, AtomTag, additionalKey.Value);
            WriteRegister(output, addedField);
            output.WriteByte(ExactEqualTest);
            BeamCompactWriter.Write(output, LabelTag, update.Value);
            WriteRegister(output, addedField);
            BeamCompactWriter.Write(output, AtomTag, sentinel.Value);
            output.WriteByte(Move);
            WriteRegister(output, addedResult);
            WriteRegister(output, result);
            output.Write(code.Span[body[index + 6].Offset..body[index + 8].Offset]);
            WriteConstructor(result, resultRegister + 2, appendField: true);
            copied = body[index + 9].Offset;
            index += 8;

            void WriteConstructor(BeamOperand destination, int roots, bool appendField)
            {
                output.WriteByte(PutMapAssoc);
                BeamCompactWriter.Write(output, LabelTag, 0);
                output.WriteByte(ExtendedLiteralOperand);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, original.Value);
                WriteRegister(output, destination);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, roots);
                output.WriteByte(ExtendedListOperand);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 2 * (fields.Length + (appendField ? 1 : 0)));
                foreach (BeamOperand[] pair in fields)
                {
                    BeamCompactWriter.Write(output, AtomTag, pair[0].Value);
                    WriteRegister(output, pair[1]);
                }

                if (appendField)
                {
                    BeamCompactWriter.Write(output, AtomTag, additionalKey.Value);
                    WriteRegister(output, addedField);
                }
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
