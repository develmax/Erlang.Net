using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? PartitionDeferredTupleReturns(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Label)> references
    )
    {
        var edits = new SortedDictionary<int, (int End, byte[] Bytes)>();
        var useCounts = references.GroupBy(reference => reference.Label)
            .ToDictionary(group => group.Key, group => group.Count());
        var selector = new BeamOperand(BeamOperandParameters.X, 0);
        int entry = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = end + 1;
            var instructions = body.Skip(start).Take(end - start);
            if (instructions.Any(instruction => instruction.Definition.Opcode is TryEnter or CatchEnter or LoopReceive
                or Wait or WaitTimeout or DebugLine or ExecutableLine))
            {
                continue;
            }

            var labels = new Dictionary<int, int>();
            for (int index = start; index < end; index++)
            {
                if (body[index] is { Definition.Opcode: Label, Operands: [var label] })
                {
                    labels.Add((int)label.Value, index);
                }
            }

            var candidates = new List<(int Decision, int TupleReturn, int Failure, BeamOperand Atom,
                BeamOperand Tag, BeamOperand Frame, BeamOperand TupleLabel, BeamOperand FailureLabel)>();
            for (int index = start + 1; index + 6 < end; index++)
            {
                if (body[index - 1].Definition.Opcode != ExternalCall
                    || body[index] is not
                    {
                        Definition.Opcode: ExactEqualTest,
                        Operands: [var tupleLabel, var input, { Kind: BeamOperandParameters.Atom } atom]
                    } || input != selector || !useCounts.TryGetValue((int)tupleLabel.Value, out int uses) || uses != 1
                    || !labels.TryGetValue((int)tupleLabel.Value, out int tupleReturn) || tupleReturn <= index + 1
                    || tupleReturn + 5 >= end || !EndsBeamBlock(body[tupleReturn - 1].Definition.Opcode)
                    || body[tupleReturn - 1] is { Definition.Opcode: Jump, Operands: [var jump] } && jump.Value == tupleLabel.Value
                    || body[tupleReturn + 1] is not
                    {
                        Definition.Opcode: IsTaggedTuple,
                        Operands: [var failureLabel, var tested, var arity, { Kind: BeamOperandParameters.Atom } tag]
                    } || tested != selector || arity.Value != 2
                    || body[tupleReturn + 2] is not { Definition.Opcode: Deallocate, Operands: [var frame] }
                    || body[tupleReturn + 3].Definition.Opcode != Return
                    || !labels.TryGetValue((int)failureLabel.Value, out int failure) || failure != tupleReturn + 4)
                {
                    continue;
                }

                int fault = failure + 1;
                if (body[fault].Definition.Opcode == Line)
                {
                    fault++;
                }

                if (fault >= end || body[fault] is not { Definition.Opcode: CaseEnd, Operands: [var value] } || value != selector)
                {
                    continue;
                }

                candidates.Add((
                    index,
                    tupleReturn,
                    failure,
                    atom,
                    tag,
                    frame,
                    tupleLabel,
                    failureLabel
                ));
            }

            if (candidates.Count < 2 || candidates[^1].Decision >= candidates.Min(candidate => candidate.TupleReturn)
                || candidates.Skip(1).Any(candidate => candidate.Atom != candidates[0].Atom
                    || candidate.Tag != candidates[0].Tag || candidate.Frame != candidates[0].Frame))
            {
                continue;
            }

            // Core groups the disjoint tuple and atom constructors before their value tests.
            // Each outlined tuple arm returns its original selector and has a single incoming edge.
            foreach (var candidate in candidates)
            {
                using var tests = new MemoryStream();
                tests.WriteByte(IsTuple);
                BeamCompactWriter.Write(tests, LabelTag, candidate.TupleLabel.Value);
                WriteRegister(tests, selector);
                tests.WriteByte(IsTaggedTuple);
                BeamCompactWriter.Write(tests, LabelTag, candidate.FailureLabel.Value);
                tests.WriteByte(ExtendedTypedRegisterOperand);
                WriteRegister(tests, selector);
                BeamCompactWriter.Write(tests, BeamWire.UnsignedTag, GetOperandTypeIndex((TupleTypeBits, null, null, null)));
                BeamCompactWriter.Write(tests, BeamWire.UnsignedTag, 2);
                BeamCompactWriter.Write(tests, AtomTag, candidate.Tag.Value);
                tests.Write(code.Span[body[candidate.TupleReturn + 2].Offset..body[candidate.Failure].Offset]);
                tests.Write(code.Span[body[candidate.TupleReturn].Offset..body[candidate.TupleReturn + 1].Offset]);
                tests.WriteByte(ExactEqualTest);
                BeamCompactWriter.Write(tests, LabelTag, candidate.FailureLabel.Value);
                WriteRegister(tests, selector);
                BeamCompactWriter.Write(tests, AtomTag, candidate.Atom.Value);
                edits.Add(body[candidate.Decision].Offset, (body[candidate.Decision + 1].Offset, tests.ToArray()));
                edits.Add(body[candidate.TupleReturn].Offset, (body[candidate.Failure].Offset, []));
            }
        }

        if (edits.Count == 0)
        {
            return null;
        }

        using var output = new MemoryStream(code.Length);
        int copied = 0;
        foreach (var (offset, edit) in edits)
        {
            output.Write(code.Span[copied..offset]);
            output.Write(edit.Bytes);
            copied = edit.End;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
