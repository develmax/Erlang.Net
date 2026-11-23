using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record EntryLiteralArm(
        BeamOperand Key,
        int Label,
        int Start,
        int End
    );

    private byte[]? GroupEntryLiteralTests(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(ExactEqualTest))
        {
            return null;
        }

        var references = new Dictionary<int, int>();
        IReadOnlyList<BeamInstruction> body;
        try
        {
            var header = new BeamCodeHeader(
                BeamWire.FormatVersion,
                highestOpcode,
                labelCount,
                exports.Count + locals.Count,
                code
            );
            body = BeamInstructionReader.Read(
                header,
                atoms.Count + 1,
                literalBytes.Count,
                recordLabelOperand: (_, _, label) => references[label] = references.GetValueOrDefault(label) + 1
            );
        }
        catch (Exception error) when (error is InvalidDataException or NotSupportedException)
        {
            return null;
        }

        var atomNames = atoms.OrderBy(atom => atom.Value).Select(atom => new Atom(atom.Key)).ToArray();
        using var output = new MemoryStream();
        int copied = 0;
        for (int index = 0; index + 4 < body.Count; index++)
        {
            if (body[index].Definition.Opcode != FunctionInfo || body[index + 1].Definition.Opcode != Label
                || body[index + 2] is not
                {
                    Definition.Opcode: ExactEqualTest,
                    Operands: [_, { Kind: BeamOperandParameters.X } selector, var firstKey]
                }
                || firstKey.Kind is not (BeamOperandParameters.Atom or BeamOperandParameters.Integer))
            {
                continue;
            }

            int info = index - 1;
            while (info >= 0 && body[info].Definition.Opcode == Line)
            {
                info--;
            }

            if (info < 0 || body[info].Definition.Opcode != Label)
            {
                continue;
            }

            var cases = new List<EntryLiteralArm>();
            int cursor = index + 2;
            while (cursor + 1 < body.Count && body[cursor] is
                {
                    Definition.Opcode: ExactEqualTest,
                    Operands: [{ Kind: BeamOperandParameters.Label } failure, var source, var key]
                } && source == selector && key.Kind == firstKey.Kind)
            {
                int start = cursor + 1;
                int end = start;
                while (end < body.Count && body[end].Definition.Opcode is not (Label or FunctionInfo or EndCode))
                {
                    end++;
                }

                int next = (int)failure.Value;
                if (end == start || end + 1 >= body.Count || body[end].Definition.Opcode != Label
                    || (int)body[end].Operands[0].Value != next || references.GetValueOrDefault(next) != 1
                    || body[end - 1].Definition.Opcode is not
                        (Return or TailLocalCall or TailLocalCallWithFrame or TailExternalCall or TailExternalCallWithFrame))
                {
                    break;
                }

                cases.Add(new EntryLiteralArm(
                    key,
                    next,
                    start,
                    end
                ));
                cursor = end + 1;
            }

            int fallback = (int)body[info].Operands[0].Value;
            if (cases.Count < 2 || cases.DistinctBy(item => item.Key).Count() != cases.Count
                || cursor + 1 >= body.Count || body[cursor] is not { Definition.Opcode: Jump, Operands: [var target] }
                || target.Value != fallback)
            {
                continue;
            }

            EntryLiteralArm[] ordered = cases.OrderBy(
                item => item.Key.Kind == BeamOperandParameters.Atom ? (Term)atomNames[(int)item.Key.Value - 1] : new Integer(item.Key.Value),
                Comparer<Term>.Create((a, b) => TermOrder.Compare(a, b, exact: true))
            ).ToArray();
            IReadOnlyList<EntryLiteralArm> layout = cases;
            var function = body[index].Operands;
            var integerLayout = firstKey.Kind == BeamOperandParameters.Integer && selector.TypeIndex is null
                ? PlanBoundedIntegerTailSelector(
                    (atomNames[(int)function[1].Value - 1].Name, (int)function[2].Value),
                    selector,
                    ordered,
                    body
                )
                : null;

            output.Write(code.Span[copied..body[index + 2].Offset]);
            output.WriteByte(SelectValue);
            if (integerLayout is { } plan)
            {
                output.WriteByte(ExtendedTypedRegisterOperand);
                BeamCompactWriter.Write(output, XRegisterTag, selector.Value);
                int type = GetOperandTypeIndex((IntegerTypeBits, plan.Lower, plan.Upper, null));
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, type);
                ordered = plan.Table;
                layout = plan.Layout;
            }
            else
            {
                BeamCompactWriter.Write(output, XRegisterTag, selector.Value);
            }

            BeamCompactWriter.Write(output, LabelTag, fallback);
            output.WriteByte(ExtendedListOperand);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, cases.Count * 2);
            foreach (var branch in ordered)
            {
                if (branch.Key.Kind == BeamOperandParameters.Atom)
                {
                    BeamCompactWriter.Write(output, AtomTag, branch.Key.Value);
                }
                else
                {
                    WriteIntegerOperand(output, branch.Key.Value);
                }

                BeamCompactWriter.Write(output, LabelTag, branch.Label);
            }

            foreach (var branch in layout)
            {
                output.WriteByte(Label);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, branch.Label);
                output.Write(code.Span[body[branch.Start].Offset..body[branch.End].Offset]);
            }

            copied = body[cursor + 1].Offset;
        }

        if (copied == 0)
        {
            return null;
        }

        highestOpcode = Math.Max(highestOpcode, SelectValue);
        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private (long Lower, long Upper, EntryLiteralArm[] Table, IReadOnlyList<EntryLiteralArm> Layout)? PlanBoundedIntegerTailSelector(
        (string Name, int Arity) signature,
        BeamOperand selector,
        IReadOnlyList<EntryLiteralArm> ordered,
        IReadOnlyList<BeamInstruction> body
    )
    {
        if (ReadIntegerEntryRange(signature, (int)selector.Value) is not { } bounds
            || bounds.Lower == bounds.Upper
            || ordered.Any(branch => branch.Key.Value < bounds.Lower || branch.Key.Value > bounds.Upper))
        {
            return null;
        }

        var seen = new Dictionary<(BeamOperand Target, BeamOperand? Constant), int>();
        var table = new EntryLiteralArm[ordered.Count];
        var layout = new List<EntryLiteralArm>();
        for (int index = 0; index < ordered.Count; index++)
        {
            EntryLiteralArm branch = ordered[index];
            if (body[branch.End - 1] is not { Definition.Opcode: TailLocalCall, Operands: [var arity, var target] }
                || arity.Value != signature.Arity)
            {
                return null;
            }

            BeamOperand? constant = null;
            if (branch.End - branch.Start == 2 && body[branch.Start] is
                { Definition.Opcode: Move, Operands: [{ Kind: BeamOperandParameters.Integer } value, var destination] }
                && destination == selector)
            {
                constant = value;
            }
            else if (branch.End - branch.Start != 1)
            {
                return null;
            }

            if (seen.TryGetValue((target, constant), out int label))
            {
                // A code entry can also be retained through module metadata.
                if (exports.Any(entry => MapEmittedLabel(entry.Entry) == branch.Label)
                    || locals.Any(entry => MapEmittedLabel(entry.Entry) == branch.Label)
                    || closures.Any(closure => MapClosureEntryLabel(closure.Entry) == branch.Label))
                {
                    return null;
                }

                table[index] = branch with { Label = label };
                continue;
            }

            seen.Add((target, constant), branch.Label);
            table[index] = branch;
            layout.Add(branch);
        }

        // OTP visits switch destinations in ascending key order and emits reverse postorder.
        layout.Reverse();

        return (bounds.Lower, bounds.Upper, table, layout);
    }
}
