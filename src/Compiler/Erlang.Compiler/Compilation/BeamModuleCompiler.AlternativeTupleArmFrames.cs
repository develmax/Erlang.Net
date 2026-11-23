using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private (ReadOnlyMemory<byte> Code, int LabelCount)? DeferAlternativeTupleArmFrame(
        ReadOnlyMemory<byte> code,
        int labelCount
    )
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(IsTaggedTuple)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

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
            if (start < 1 || start + 7 >= end || body[start].Definition.Opcode != Label
                || body[start - 1] is not { Definition.Opcode: FunctionInfo, Operands: [_, _, var arity] }
                || arity.Value <= 0 || arity.Value >= FirstReservedXRegister
                || body[start + 1] is not
                {
                    Definition.Opcode: ExactEqualTest or ExactNotEqualTest,
                    Operands: [var alternative, { Kind: BeamOperandParameters.X, TypeIndex: null } tested, { Kind: BeamOperandParameters.Atom }]
                }
                || tested.Value >= arity.Value
                || body[start + 2] is not { Definition.Opcode: TailExternalCall or TailLocalCall, Operands: [var tailArity, _] }
                || tailArity.Value > arity.Value || body[start + 3] is not { Definition.Opcode: Label, Operands: [var arm] }
                || arm.Value != alternative.Value || body[start + 4] is not
                { Definition.Opcode: Allocate, Operands: [{ Value.Sign: > 0 } width, var roots] }
                || roots.Value != arity.Value || width.Value > BeamOperandParameters.MaximumRegister)
            {
                continue;
            }

            int allocation = start + 4;
            int initialized = allocation + 1;
            bool hasInit = body[initialized].Definition.Opcode == InitYRegisters;
            int spill = hasInit ? initialized + 1 : initialized;
            int check = spill;
            var saved = new Dictionary<BeamOperand, BeamOperand>();
            while (check < end && body[check] is
                {
                    Definition.Opcode: Move,
                    Operands: [
                    { Kind: BeamOperandParameters.X, TypeIndex: null } original,
                    { Kind: BeamOperandParameters.Y, TypeIndex: null } storage
                    ]
                } && original.Value < arity.Value && storage.Value < width.Value && saved.TryAdd(storage, original))
            {
                check++;
            }

            int outside = EntryFrameFailureLabel(body, start);
            if (check + 1 >= end || outside <= 0 || body[check] is not
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands:
                    [
                        var rejected,
                    { Kind: BeamOperandParameters.X, TypeIndex: null } selector,
                    { Value.Sign: > 0 } tupleArity,
                    { Kind: BeamOperandParameters.Atom } tag
                    ]
                } || selector.Value >= arity.Value)
            {
                continue;
            }

            flow ??= new BeamProjectionFlow(body);
            if (!flow.TryGetSingleForwardTarget(start + 1, (int)arm.Value, out int alternateEntry) || alternateEntry != start + 3
                || !flow.TryGetSingleForwardTarget(check, (int)rejected.Value, out int cleanup) || cleanup + 2 >= end)
            {
                continue;
            }

            int restored = cleanup + 1;
            var restoredSlots = new HashSet<BeamOperand>();
            while (restored < end && body[restored] is { Definition.Opcode: Move, Operands: [var from, var to] }
                && saved.TryGetValue(from, out var prior) && prior == to && restoredSlots.Add(from))
            {
                restored++;
            }

            if (restored + 1 >= end || restoredSlots.Count != saved.Count
                || body[restored] is not { Definition.Opcode: Deallocate, Operands: [var size] } || size != width
                || body[restored + 1] is not { Definition.Opcode: Jump, Operands: [var failure] }
                || failure.Value != outside
                || ReadDisjointFrameOwnership(body, start, end) is not { } ownership || ownership.Uses.Count != 1
                || ownership.Owners[cleanup - start] != allocation || ownership.Owners[check + 1 - start] != allocation)
            {
                continue;
            }

            bool omitInitialization = false;
            if (hasInit)
            {
                var pending = body[initialized].Operands.Skip(1).Where(slot => !saved.ContainsKey(slot)).ToHashSet();
                for (int projection = check + 1; projection < cleanup && body[projection] is
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var tuple, var field, { Kind: BeamOperandParameters.Y, TypeIndex: null } slot]
                } && tuple == selector && field.Value >= 0 && field.Value < tupleArity.Value; projection++)
                {
                    pending.Remove(slot);
                }

                omitInitialization = pending.Count == 0;
            }

            // The failed tag test still sees the same original X values.
            // Only the successful continuation installs and initializes a frame.
            output.Write(code.Span[copied..body[allocation].Offset]);
            output.WriteByte(IsTaggedTuple);
            BeamCompactWriter.Write(output, LabelTag, outside);
            WriteRegister(output, selector);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, tupleArity.Value);
            BeamCompactWriter.Write(output, AtomTag, tag.Value);
            int setupEnd = omitInitialization ? spill : initialized;
            output.Write(code.Span[body[allocation].Offset..body[initialized].Offset]);
            output.Write(code.Span[body[setupEnd].Offset..body[check].Offset]);
            output.Write(code.Span[body[check + 1].Offset..body[cleanup].Offset]);
            copied = body[restored + 2].Offset;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return OrderFinalPhysicalLabelsCore(
            output.ToArray(),
            labelCount,
            formMultiKeySelections: false,
            relocateOnly: true
        );
    }
}
