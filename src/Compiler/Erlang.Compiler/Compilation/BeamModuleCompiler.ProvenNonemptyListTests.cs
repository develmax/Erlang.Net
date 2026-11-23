using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? RemoveProvenNonemptyListTests(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (properListSourceFunctions is null || !SsaTypeInferenceEnabled || ModuleOptimizationDisabled
            || OnLoadFunction is not null || sourceModule?.DeclaredNifs is { Count: > 0 })
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

        string[] atomNames = atoms.OrderBy(entry => entry.Value).Select(entry => entry.Key).ToArray();
        var retainedLabels = exports.Select(entry => MapEmittedLabel(entry.Entry))
            .Concat(locals.Select(entry => MapEmittedLabel(entry.Entry)))
            .Concat(closures.Select(closure => MapClosureEntryLabel(closure.Entry))).ToHashSet();
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        (string Name, int Arity) signature = default;
        int entry = -1;
        int copied = 0;
        for (int index = 0; index + 1 < body.Count; index++)
        {
            BeamInstruction instruction = body[index];
            if (instruction is { Definition.Opcode: FunctionInfo, Operands: [_, var function, var arity] })
            {
                signature = (atomNames[(int)function.Value - 1], (int)arity.Value);
                entry = body[index + 1].Definition.Opcode == Label && !exportedFunctions.Contains(signature) ? index + 1 : -1;
                continue;
            }

            if (entry < 0 || instruction is not
                {
                    Definition.Opcode: IsNonemptyList,
                    Operands: [
                    { Kind: BeamOperandParameters.Label } failure,
                    { Kind: BeamOperandParameters.X, TypeIndex: null } tested]
                })
            {
                continue;
            }

            int failureLabel = (int)failure.Value;
            int head = SkipLines(index + 1);
            if (head >= body.Count || body[head] is not { Definition.Opcode: GetHead, Operands: [var source, _] }
                || SameProjectionRegister(source, tested))
            {
                continue;
            }

            int tail = SkipLines(head + 1);
            if (tail + 2 >= body.Count || body[tail].Definition.Opcode != TailLocalCall
                || references.GetValueOrDefault(failureLabel) != 1 || retainedLabels.Contains(failureLabel))
            {
                continue;
            }

            flow ??= new BeamProjectionFlow(body);
            if (!flow.TryGetSingleForwardTarget(index, failureLabel, out int target) || target != tail + 1)
            {
                continue;
            }

            int exit = SkipLines(target + 1);
            if (exit + 1 >= body.Count || body[exit] is not { Definition.Opcode: BadMatch, Operands: [var rejected] }
                || (rejected.Kind != BeamOperandParameters.Nil && rejected != tested)
                || !flow.ReadProperListEntryOrigins(
                    index,
                    entry,
                    tested,
                    argument => HasNonemptyListEntryFact(signature, argument)
                ))
            {
                continue;
            }

            // The only incoming edge to the cold failure comes from this proven test.
            output.Write(code.Span[copied..instruction.Offset]);
            output.Write(code.Span[body[index + 1].Offset..body[target].Offset]);
            copied = body[exit + 1].Offset;
            index = exit;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        int SkipLines(int index)
        {
            while (index < body.Count && body[index].Definition.Opcode == Line)
            {
                index++;
            }

            return index;
        }
    }
}
