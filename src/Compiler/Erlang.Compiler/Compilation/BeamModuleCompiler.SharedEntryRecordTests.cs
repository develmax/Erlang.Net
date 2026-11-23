using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ShareCheckedEntryRecordTests(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Label)> labels
    )
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled)
        {
            return null;
        }

        var edits = new List<CodeEdit>();
        for (int entry = 1; entry + 3 < body.Count; entry++)
        {
            int check = entry + 1;
            if (body[entry - 1].Definition.Opcode != FunctionInfo || body[entry].Definition.Opcode != Label
                || body[check] is not
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [var rejected, var selector, var width, var tag]
                }
                || selector is not { Kind: BeamOperandParameters.X, TypeIndex: null }
                || rejected is not { Kind: BeamOperandParameters.Label, Value.Sign: > 0 })
            {
                continue;
            }

            int guard = check + 1;
            while (guard + 1 < body.Count && TryReadSharedEntryField(
                body[guard],
                selector,
                out _,
                out _
            ))
            {
                guard++;
            }

            if (body[guard] is not
                {
                    Definition.Opcode: ExactEqualTest,
                    Operands: [var failed, var compare,
                    {
                        Kind: BeamOperandParameters.Atom or
                        BeamOperandParameters.Integer or BeamOperandParameters.LiteralValue or BeamOperandParameters.Nil
                    }]
                }
                || failed != rejected || !Enumerable.Range(check + 1, guard - check - 1)
                    .Any(index => SameProjectionRegister(body[index].Operands[^1], compare)))
            {
                continue;
            }

            int alternate = guard + 1;
            while (alternate + 1 < body.Count && body[alternate].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                if (body[alternate] is { Definition.Opcode: Label, Operands: [var label] }
                    && label.Value == rejected.Value)
                {
                    break;
                }

                alternate++;
            }

            int failure = EntryFrameFailureLabel(body, entry);
            if (failure <= 0 || alternate <= guard + 1 || alternate + 2 >= body.Count
                || body[alternate] is not { Definition.Opcode: Label, Operands: [var target] }
                || target.Value != rejected.Value || !EndsBeamBlock(body[alternate - 1].Definition.Opcode)
                || body[alternate + 1] is not
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [var secondFailure, var carrier, var secondWidth, var secondTag]
                }
                || secondFailure.Kind != BeamOperandParameters.Label || secondFailure.Value != failure
                || carrier != selector || secondWidth != width || secondTag != tag
                || labels.Count(item => item.Label == (int)rejected.Value) != 2)
            {
                continue;
            }

            // The rejected type sees the original arguments. Only the field-test
            // failure still reaches the alternate, with the same checked carrier.
            int from = body[check].Offset + 1;
            var cursor = new BeamCursor(code[from..]);
            _ = BeamCompactReader.Read(cursor);
            edits.Add(new(
                from,
                from + cursor.Position,
                LabelTag,
                failure
            ));
            edits.Add(new(
                body[alternate + 1].Offset,
                body[alternate + 2].Offset,
                0,
                0,
                IsRemoval: true
            ));

            var projected = new Dictionary<BeamOperand, BeamOperand>();
            for (int projection = check + 1; projection < guard; projection++)
            {
                var projectionOperands = body[projection].Operands;
                projected[projectionOperands[2]] = projectionOperands[1];
            }

            for (int projection = alternate + 2; projection + 1 < body.Count && TryReadSharedEntryField(
                body[projection],
                selector,
                out var field,
                out var register
            ); projection++)
            {
                if (projected.TryGetValue(register, out var existing) && existing == field)
                {
                    edits.Add(new(
                        body[projection].Offset,
                        body[projection + 1].Offset,
                        0,
                        0,
                        IsRemoval: true
                    ));
                }

                projected[register] = field;
            }
        }

        return ApplyDuplicatedFrameReadEdits(code, edits);
    }

    private static bool TryReadSharedEntryField(
        BeamInstruction instruction,
        BeamOperand selector,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out BeamOperand? field,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out BeamOperand? register
    )
    {
        if (instruction is
            {
                Definition.Opcode: GetTupleElement,
                Operands: [var source, { Kind: BeamOperandParameters.Unsigned } member,
                { Kind: BeamOperandParameters.X, TypeIndex: null } destination]
            }
            && source == selector && destination != selector)
        {
            field = member;
            register = destination;

            return true;
        }

        field = null;
        register = null;

        return false;
    }
}
