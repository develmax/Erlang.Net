using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ReturnExactMatchPredicates(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !atoms.TryGetValue(ErlangBooleanAtoms.True, out int trueAtom)
            || !atoms.TryGetValue(ErlangBooleanAtoms.False, out int falseAtom)
            || !code.Span.Contains(ExactEqualTest) && !code.Span.Contains(IsNil))
        {
            return null;
        }

        var operands = new Dictionary<int, (int End, int Label)>();
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
                recordLabelOperand: (start, end, label) => operands.Add(start, (end, label))
            );
        }
        catch (Exception error) when (error is InvalidDataException or NotSupportedException)
        {
            return null;
        }

        if (body.Any(instruction => instruction.Definition.Opcode is DebugLine or ExecutableLine))
        {
            return null;
        }

        var falseReturns = new Dictionary<int, int>();
        for (int index = 0; index + 2 < body.Count; index++)
        {
            if (body[index] is { Definition.Opcode: Label, Operands: [var label] }
                && ReturnsAtom(index + 1, falseAtom))
            {
                falseReturns.Add((int)label.Value, index);
            }
        }

        var folds = new Dictionary<int, (int Operand, int Failure)>();
        for (int index = 0; index + 3 < body.Count; index++)
        {
            BeamInstruction test = body[index];
            if (test.Definition.Opcode is ExactEqualTest or IsNil
                && operands.TryGetValue(test.Offset + 1, out var reference)
                && falseReturns.ContainsKey(reference.Label) && ReturnsAtom(index + 1, trueAtom))
            {
                folds.Add(index, (reference.End, reference.Label));
            }
        }

        if (folds.Count == 0)
        {
            return null;
        }

        var references = operands.Values.GroupBy(operand => operand.Label)
            .ToDictionary(group => group.Key, group => group.Count());
        foreach (var (_, failure) in folds.Values)
        {
            references[failure]--;
        }

        // A previous return excludes fallthrough from the preceding block.
        var replacedTargets = folds.Values.Select(fold => fold.Failure).ToHashSet();
        var entries = exports.Concat(locals).Select(entry => MapEmittedLabel(entry.Entry))
            .Concat(closures.Select(closure => MapClosureEntryLabel(closure.Entry))).ToHashSet();
        var dead = falseReturns.Where(pair => replacedTargets.Contains(pair.Key) && references[pair.Key] == 0
            && !entries.Contains(pair.Key)
            && pair.Value > 0
            && body[pair.Value - 1].Definition.Opcode == Return).Select(pair => pair.Value).ToHashSet();
        int imported = Import(ErlangModuleNames.Module, ErlangOperators.ExactEqual, 2);
        using var output = new MemoryStream(code.Length);
        int copied = 0;
        for (int index = 0; index + 3 < body.Count; index++)
        {
            if (dead.Contains(index))
            {
                output.Write(code.Span[copied..body[index].Offset]);
                copied = body[index + 3].Offset;
                index += 2;
            }
            else if (folds.TryGetValue(index, out var fold))
            {
                BeamInstruction test = body[index];
                output.Write(code.Span[copied..test.Offset]);
                output.WriteByte(BifTwo);
                BeamCompactWriter.Write(output, LabelTag, 0);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, imported);
                output.Write(code.Span[fold.Operand..body[index + 1].Offset]);
                if (test.Definition.Opcode == IsNil)
                {
                    BeamCompactWriter.Write(output, AtomTag, 0);
                }

                BeamCompactWriter.Write(output, XRegisterTag, 0);
                output.WriteByte(Return);
                copied = body[index + 3].Offset;
                index += 2;
            }
        }

        output.Write(code.Span[copied..]);
        highestOpcode = Math.Max(highestOpcode, BifTwo);

        return output.ToArray();

        bool ReturnsAtom(int index, int atom) =>
            body[index] is { Definition.Opcode: Move, Operands: [{ Kind: BeamOperandParameters.Atom } value, var destination] }
            && value.Value == atom && IsBareX0(destination)
            && body[index + 1].Definition.Opcode == Return;
    }
}
