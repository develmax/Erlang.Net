using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? HoistCheckedNestedRecordAllocations(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(BadRecord)
            || !code.Span.Contains(Allocate) || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        BeamProjectionFlow? flow = null;
        var edits = new SortedDictionary<int, (int End, byte[] Bytes)>();
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        int entry = -1;
        int arity = 0;
        bool? unprotected = null;
        for (int outer = 0; outer + 8 < body.Count; outer++)
        {
            if (body[outer] is { Definition.Opcode: FunctionInfo, Operands: [_, _, var arguments] })
            {
                entry = outer + 1;
                arity = (int)arguments.Value;
                unprotected = null;
            }

            if (entry < 0 || arity is < 2 or >= FirstReservedXRegister
                || body[outer] is not
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [_, { Kind: BeamOperandParameters.X, TypeIndex: null } parent, var parentWidth, _]
                }
                || parent.Value <= 0 || parent.Value >= arity
                || body[outer + 1] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var projectionSource, var field, { Kind: BeamOperandParameters.X, TypeIndex: null } scratch]
                }
                || parent != projectionSource || scratch.Value != arity || field.Value >= parentWidth.Value
                || body[outer + 2] is not { Definition.Opcode: Allocate, Operands: [var width, var live] }
                || live.Value != arity + 1
                || body[outer + 3] is not { Definition.Opcode: InitYRegisters, Operands: [_, var cleared] }
                || body[outer + 4] is not { Definition.Opcode: Move, Operands: [var original, var parentSave] }
                || original != parent || !IsEntryCarrierSlot(parentSave, width)
                || body[outer + 5] is not { Definition.Opcode: Move, Operands: [var projected, var innerSave] }
                || projected != scratch || !IsEntryCarrierSlot(innerSave, width) || innerSave == parentSave
                || body[outer + 6] is not
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [{ Kind: BeamOperandParameters.Label } fail, var tested, var innerWidth, var tag]
                }
                || tested != innerSave
                || body[outer + 7] is not { Definition.Opcode: GetTupleElement, Operands: [var inner, var leaf, var fieldResult] }
                || inner != innerSave || fieldResult != x0 || leaf.Value >= innerWidth.Value)
            {
                continue;
            }

            unprotected ??= HasUnprotectedCallKillSchedule(body, entry);
            if (unprotected != true)
            {
                continue;
            }

            flow ??= new BeamProjectionFlow(body);
            if (!flow.ReachesLinearFrameUnframed(entry, outer + 2)
                || !flow.TryGetDedicatedBadRecordTarget(outer + 6, (int)fail.Value, out int cold)
                || body[cold + 1] is not { Definition.Opcode: Line, Operands: [{ Value.Sign: > 0 }] }
                || body[cold + 2] is not { Definition.Opcode: BadRecord, Operands: [var failed] }
                || failed != innerSave || edits.ContainsKey(body[cold + 2].Offset))
            {
                continue;
            }

            int next = outer + 8;
            BeamOperand destination = x0;
            if (body[next] is { Definition.Opcode: Move, Operands: [var from, var to] }
                && from == x0 && IsEntryCarrierSlot(to, width) && to != innerSave && to != parentSave && cleared == to)
            {
                destination = to;
                next++;
            }

            if (next >= cold || (destination == x0 ? cleared != parentSave : !flow.IsEntryTemporaryDead(next, x0))
                || !flow.IsEntryTemporaryDead(next, scratch))
            {
                continue;
            }

            using var prefix = new MemoryStream();
            prefix.WriteByte(GetTupleElement);
            WriteRegister(prefix, parent);
            BeamCompactWriter.Write(prefix, BeamWire.UnsignedTag, field.Value);
            WriteRegister(prefix, x0);
            prefix.WriteByte(IsTaggedTuple);
            BeamCompactWriter.Write(prefix, LabelTag, fail.Value);
            WriteRegister(prefix, x0);
            BeamCompactWriter.Write(prefix, BeamWire.UnsignedTag, innerWidth.Value);
            BeamCompactWriter.Write(prefix, AtomTag, tag.Value);
            prefix.WriteByte(Allocate);
            BeamCompactWriter.Write(prefix, BeamWire.UnsignedTag, width.Value);
            BeamCompactWriter.Write(prefix, BeamWire.UnsignedTag, arity);
            prefix.WriteByte(Move);
            WriteRegister(prefix, x0);
            WriteRegister(prefix, innerSave);
            prefix.Write(code.Span[body[outer + 4].Offset..body[outer + 5].Offset]);
            prefix.WriteByte(GetTupleElement);
            WriteRegister(prefix, x0);
            BeamCompactWriter.Write(prefix, BeamWire.UnsignedTag, leaf.Value);
            WriteRegister(prefix, destination);

            // The checked inner value travels in X0 on the sole failure edge.
            // Its exception location and all success Y bindings are preserved.
            edits.Add(body[outer + 1].Offset, (body[next].Offset, prefix.ToArray()));
            edits.Add(body[cold + 2].Offset, (body[cold + 3].Offset, [BadRecord, XRegisterTag]));
            outer = next - 1;
        }

        if (edits.Count == 0)
        {
            return null;
        }

        using var output = new MemoryStream(code.Length);
        int copied = 0;
        foreach (var (start, edit) in edits)
        {
            if (start < copied)
            {
                return null;
            }

            output.Write(code.Span[copied..start]);
            output.Write(edit.Bytes);
            copied = edit.End;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private sealed partial class BeamProjectionFlow
    {
        internal bool TryGetDedicatedBadRecordTarget(int from, int label, out int target)
        {
            if (!TryGetForwardLabel(from, label, out target) || target + 3 >= body.Count)
            {
                return false;
            }

            int terminal = target - 1;

            return predecessors[target].Contains(from) && predecessors[target].All(index =>
                index == from || index == terminal && body[terminal].Definition.Opcode == BadRecord);
        }
    }
}
