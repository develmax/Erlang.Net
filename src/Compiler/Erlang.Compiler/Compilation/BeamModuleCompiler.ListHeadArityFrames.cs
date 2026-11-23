using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? PlaceListHeadArityBranchFrames(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(SelectTupleArity)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        for (int info = 0; info + 12 < body.Count; info++)
        {
            if (body[info] is not { Definition.Opcode: FunctionInfo, Operands: [_, _, { Value.IsOne: true }] }
                || body[info + 1].Definition.Opcode != Label
                || body[info + 2] is not
                {
                    Definition.Opcode: IsNonemptyList,
                    Operands: [var nil, var input]
                }
                || nil.Kind != BeamOperandParameters.Label || !IsBareX0(input)
                || body[info + 3] is not
                {
                    Definition.Opcode: GetList,
                    Operands: [var list, { Kind: BeamOperandParameters.X, Value.IsOne: true, TypeIndex: null } head, var rest]
                }
                || list != input || rest != input)
            {
                continue;
            }

            int allocation = info + 4;
            if (body[allocation] is not
                {
                    Definition.Opcode: Allocate,
                    Operands: [
                    { Kind: BeamOperandParameters.Unsigned, Value.Sign: > 0 } size,
                    { Kind: BeamOperandParameters.Unsigned, Value: var live }]
                }
                || live != 2 || size.Value < 2 || size.Value > BeamOperandParameters.MaximumRegister)
            {
                continue;
            }

            int saves = allocation + 1;
            if (body[saves].Definition.Opcode == InitYRegisters)
            {
                saves++;
            }

            if (body[saves] is not { Definition.Opcode: Move, Operands: [var headInput, var carrier] }
                || body[saves + 1] is not { Definition.Opcode: Move, Operands: [var tailInput, var tailSlot] }
                || headInput != head || tailInput != input || carrier == tailSlot
                || !IsOwnedSlot(carrier) || !IsOwnedSlot(tailSlot))
            {
                continue;
            }

            int test = saves + 2;
            int dispatch = test + 1;
            if (body[test] is not
                {
                    Definition.Opcode: IsTuple,
                    Operands: [{ Kind: BeamOperandParameters.Label } failed, var tuple]
                }
                || tuple != carrier
                || body[dispatch] is not
                {
                    Definition.Opcode: SelectTupleArity,
                    Operands: [var tested, var other, _, _, _, _, _, ..]
                } selector
                || !SameProjectionRegister(tested, carrier) || other != failed)
            {
                continue;
            }

            int end = dispatch + 1;
            while (end < body.Count && body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                end++;
            }

            flow ??= new BeamProjectionFlow(body);
            if (end >= body.Count
                || !flow.TryGetSingleForwardTarget(info + 2, (int)nil.Value, out int nilLabel)
                || nilLabel <= dispatch || nilLabel >= end
                || !flow.TryGetExclusiveForwardTarget(
                    test,
                    dispatch,
                    (int)failed.Value,
                    out int rejected
                )
                || rejected + 3 >= end
                || body[rejected + 1] is not { Definition.Opcode: Move, Operands: [var failedInput, var failedOutput] }
                || failedInput != carrier || failedOutput != input)
            {
                continue;
            }

            int failure = SkipLines(rejected + 2);
            if (failure >= end || body[failure] is not { Definition.Opcode: CaseEnd, Operands: [var value] }
                || value != input || body.Skip(allocation).Take(end - allocation).Any(instruction =>
                    instruction.Definition.Opcode is DebugLine or ExecutableLine or CatchEnter or TryEnter))
            {
                continue;
            }

            var arms = new SortedDictionary<int, int?>();
            for (int index = 4; index < selector.Operands.Count; index += 2)
            {
                BeamOperand arm = selector.Operands[index];
                if (arm.Kind != BeamOperandParameters.Label
                    || !flow.TryGetSingleForwardTarget(dispatch, (int)arm.Value, out int label)
                    || label + 1 >= end || !arms.TryAdd(label + 1, ReadRetainedHeadCall(label + 1)))
                {
                    arms.Clear();
                    break;
                }
            }

            if (!arms.Values.Any(call => call.HasValue))
            {
                continue;
            }

            int setup = body[allocation].Offset;
            int setupEnd = body[test].Offset;
            output.Write(code.Span[copied..setup]);
            output.WriteByte(IsTuple);
            BeamCompactWriter.Write(output, LabelTag, failed.Value);
            WriteRegister(output, head);
            output.WriteByte(SelectTupleArity);
            if (tested.TypeIndex is int type)
            {
                output.WriteByte(ExtendedTypedRegisterOperand);
                WriteRegister(output, head);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, type);
            }
            else
            {
                WriteRegister(output, head);
            }

            int operands = body[dispatch].Offset + 1;
            var reader = new BeamCursor(code[operands..]);
            _ = BeamCompactReader.Read(reader);
            if (tested.TypeIndex is not null)
            {
                _ = BeamCompactReader.Read(reader);
                _ = BeamCompactReader.Read(reader);
            }

            output.Write(code.Span[(operands + reader.Position)..body[dispatch + 1].Offset]);
            copied = body[dispatch + 1].Offset;

            // Only a selected arity can own the old setup. A retained head needs
            // one Y slot and its tail is still the original X0 argument.
            var sites = arms.Keys.Append(rejected + 1).Order();
            foreach (int site in sites)
            {
                int offset = body[site].Offset;
                output.Write(code.Span[copied..offset]);
                if (site == rejected + 1)
                {
                    output.Write(code.Span[body[site + 1].Offset..body[failure].Offset]);
                    output.WriteByte(CaseEnd);
                    WriteRegister(output, head);
                    copied = body[failure + 1].Offset;
                }
                else if (arms[site] is int call)
                {
                    output.WriteByte(Allocate);
                    BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
                    BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 2);
                    output.WriteByte(Move);
                    WriteRegister(output, head);
                    BeamCompactWriter.Write(output, YRegisterTag, 0);
                    output.Write(code.Span[offset..body[call + 2].Offset]);
                    output.WriteByte(PutList);
                    BeamCompactWriter.Write(output, YRegisterTag, 0);
                    WriteRegister(output, input);
                    WriteRegister(output, input);
                    output.WriteByte(Deallocate);
                    BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
                    copied = body[call + 4].Offset;
                }
                else
                {
                    output.Write(code.Span[setup..setupEnd]);
                    copied = offset;
                }
            }

            info = end - 1;

            bool IsOwnedSlot(BeamOperand operand) =>
                operand is { Kind: BeamOperandParameters.Y, Value.Sign: >= 0, TypeIndex: null }
                && operand.Value < size.Value;

            int SkipLines(int index)
            {
                while (index < end && body[index].Definition.Opcode == Line)
                {
                    index++;
                }

                return index;
            }

            int? ReadRetainedHeadCall(int start)
            {
                int call = SkipLines(start);
                if (call + 4 >= end
                    || body[call] is not { Definition.Opcode: LocalCall, Operands: [{ Value.IsOne: true }, _] }
                    || body[call + 1] is not { Definition.Opcode: TestHeap, Operands: [var heap, { Value.IsOne: true }] }
                    || heap.Value != ConsHeapWords
                    || body[call + 2] is not { Definition.Opcode: PutList, Operands: [var kept, var listTail, var result] }
                    || kept != carrier || listTail != input || result != input
                    || body[call + 3] is not { Definition.Opcode: Deallocate, Operands: [var discarded] }
                    || discarded != size || body[call + 4].Definition.Opcode != Return)
                {
                    return null;
                }

                return call;
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
