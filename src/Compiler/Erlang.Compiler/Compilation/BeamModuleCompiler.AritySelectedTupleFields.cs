using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ShareAritySelectedTupleFields(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(SelectTupleArity)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        return RewriteAritySelectedTupleFields(code, body);
    }

    private static byte[]? RewriteAritySelectedTupleFields(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body)
    {
        BeamProjectionFlow? flow = null;
        var replacements = new Dictionary<int, (BeamInstruction Selector, BeamOperand Scratch, int FirstRead, int FieldCount)>();
        var removals = new HashSet<int>();
        for (int aritySelect = 0; aritySelect + 1 < body.Count; aritySelect++)
        {
            if (body[aritySelect] is not
                {
                    Definition.Opcode: SelectTupleArity,
                    Operands: [var tuple, _, _, _, _, ..]
                } arities || tuple.Kind != BeamOperandParameters.X)
            {
                continue;
            }

            flow ??= new BeamProjectionFlow(body);
            for (int option = 3; option + 1 < arities.Operands.Count; option += 2)
            {
                var tupleWidth = arities.Operands[option].Value;
                var branch = arities.Operands[option + 1];
                if (tupleWidth < 2 || branch.Kind != BeamOperandParameters.Label
                    || !flow.TryGetSingleForwardTarget(aritySelect, (int)branch.Value, out int entry)
                    || entry + 2 >= body.Count
                    || body[entry + 1] is not
                    {
                        Definition.Opcode: GetTupleElement,
                        Operands: [var carrier, { Value.IsZero: true }, var tag]
                    } || !SameProjectionRegister(tuple, carrier) || !IsBareX(carrier) || !IsBareX(tag) || carrier == tag
                    || body[entry + 2] is not
                    {
                        Definition.Opcode: SelectValue,
                        Operands: [var discriminator, var rejected, var count, ..]
                    } selector || discriminator != tag)
                {
                    continue;
                }

                int dispatch = entry + 2;
                if (count.Value >= 6)
                {
                    ShareSingleField();
                    continue;
                }

                if (tupleWidth < 3 || count.Value != 4 || selector.Operands is not
                    [
                        _, _, _,
                    { Kind: BeamOperandParameters.Atom } firstKey,
                    { Kind: BeamOperandParameters.Label } firstTarget,
                    { Kind: BeamOperandParameters.Atom } secondKey,
                    { Kind: BeamOperandParameters.Label } secondTarget
                    ] || firstKey == secondKey)
                {
                    continue;
                }

                if (!flow.TryGetSingleForwardTarget(dispatch, (int)firstTarget.Value, out int first)
                    || !flow.TryGetSingleForwardTarget(dispatch, (int)secondTarget.Value, out int second)
                    || first == second || Math.Max(first, second) + 2 >= body.Count
                    || !HasUnprotectedCheckedFailureRegion(body, aritySelect, Math.Max(first, second) + 2)
                    || !ReadsField(first + 1, out var left) || !ReadsField(first + 2, out var right)
                    || left == right || body[first + 1].Operands[1] == body[first + 2].Operands[1]
                    || !body[first + 1].Operands.SequenceEqual(body[second + 1].Operands)
                    || !body[first + 2].Operands.SequenceEqual(body[second + 2].Operands)
                    || body[second + 1].Definition.Opcode != GetTupleElement
                    || body[second + 2].Definition.Opcode != GetTupleElement)
                {
                    continue;
                }

                int register = (int)new[] { carrier, tag, left, right }.Max(operand => operand.Value) + 1;
                var scratch = new BeamOperand(BeamOperandParameters.X, register);
                if (register >= FirstReservedXRegister
                    || !new[] { tag, left, right, scratch }.Distinct().All(operand => flow.IsBranchTemporaryDead(dispatch, operand))
                    || removals.Contains(entry + 1) || removals.Contains(dispatch))
                {
                    continue;
                }

                // Read the checked tuple once. The old tag register may be a
                // payload destination, so select using an independent scratch.
                replacements.Add(entry + 1, (selector, scratch, first + 1, 2));
                removals.UnionWith([dispatch, first + 1, first + 2, second + 1, second + 2]);

                void ShareSingleField()
                {
                    var reads = new List<int>();
                    var keys = new HashSet<BeamOperand>();
                    for (int index = 3; index + 1 < selector.Operands.Count; index += 2)
                    {
                        var key = selector.Operands[index];
                        var target = selector.Operands[index + 1];
                        if (key.Kind != BeamOperandParameters.Atom || !keys.Add(key)
                            || target.Kind != BeamOperandParameters.Label || target == rejected
                            || !flow!.TryGetSingleForwardTarget(dispatch, (int)target.Value, out int arm)
                            || arm + 1 >= body.Count || !ReadsField(arm + 1, out _)
                            || (reads.Count != 0 && !body[reads[0]].Operands.SequenceEqual(body[arm + 1].Operands)))
                        {
                            return;
                        }

                        reads.Add(arm + 1);
                    }

                    if (reads.Count < 3 || reads.Distinct().Count() != reads.Count
                        || !HasUnprotectedCheckedFailureRegion(body, aritySelect, reads.Max())
                        || removals.Contains(entry + 1) || removals.Contains(dispatch)
                        || reads.Any(removals.Contains))
                    {
                        return;
                    }

                    var destination = body[reads[0]].Operands[2];
                    int scratchIndex = destination == tag
                        ? Math.Max((int)carrier.Value, (int)tag.Value) + 1
                        : (int)tag.Value;
                    var scratchRegister = new BeamOperand(BeamOperandParameters.X, scratchIndex);
                    if (scratchIndex >= FirstReservedXRegister
                        || !flow!.IsBranchTemporaryDead(dispatch, destination)
                        || (scratchRegister != tag && (!flow.IsBranchTemporaryDead(dispatch, tag)
                            || !flow.IsBranchTemporaryDead(dispatch, scratchRegister))))
                    {
                        return;
                    }

                    // The successful arity edge already proves the read is safe.
                    // The rejection and every tag arm must kill any register written early.
                    replacements.Add(entry + 1, (selector, scratchRegister, reads[0], 1));
                    removals.Add(dispatch);
                    removals.UnionWith(reads);
                }

                bool ReadsField(int index, out BeamOperand result)
                {
                    if (body[index] is
                        {
                            Definition.Opcode: GetTupleElement,
                            Operands: [var from, var offset, var destination]
                        } && from == carrier && offset.Value > 0 && offset.Value < tupleWidth
                        && IsBareX(destination) && destination != carrier)
                    {
                        result = destination;

                        return true;
                    }

                    result = null!;

                    return false;
                }
            }
        }

        if (replacements.Count == 0)
        {
            return null;
        }

        using var output = new MemoryStream();
        for (int index = 0; index + 1 < body.Count; index++)
        {
            if (replacements.TryGetValue(index, out var replacement))
            {
                output.WriteByte(GetTupleElement);
                WriteRegister(output, body[index].Operands[0]);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 0);
                WriteRegister(output, replacement.Scratch);
                for (int field = 0; field < replacement.FieldCount; field++)
                {
                    CopyInstruction(replacement.FirstRead + field);
                }

                BeamInstruction selector = replacement.Selector;
                int operands = selector.Offset + 1;
                var cursor = new BeamCursor(code[operands..]);
                _ = BeamCompactReader.Read(cursor);
                output.WriteByte(SelectValue);
                WriteRegister(output, replacement.Scratch);
                output.Write(code.Span[(operands + cursor.Position)..body[index + 2].Offset]);
            }
            else if (!removals.Contains(index))
            {
                CopyInstruction(index);
            }
        }

        output.Write(code.Span[body[^1].Offset..]);

        return output.ToArray();

        static bool IsBareX(BeamOperand operand) => operand is { Kind: BeamOperandParameters.X, TypeIndex: null };

        void CopyInstruction(int index) => output.Write(code.Span[body[index].Offset..body[index + 1].Offset]);
    }
}
