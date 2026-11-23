using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? RewriteGuardedCallPayloadCarrier(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var x1 = new BeamOperand(BeamOperandParameters.X, 1);
        var x2 = new BeamOperand(BeamOperandParameters.X, 2);
        var x3 = new BeamOperand(BeamOperandParameters.X, 3);
        var edits = new List<CodeEdit>();
        BeamProjectionFlow? flow = null;
        int functionEnd = 0;
        for (int index = 1; index + 5 < body.Count; index++)
        {
            if (body[index].Definition.Opcode == FunctionInfo)
            {
                functionEnd = index + 1;
                while (body[functionEnd].Definition.Opcode is not (FunctionInfo or EndCode))
                {
                    functionEnd++;
                }
            }

            if (index + 5 >= functionEnd || body[index - 1].Definition.Opcode is not (LocalCall or ExternalCall)
                || body[index] is not { Definition.Opcode: IsTuple, Operands: [var rejected, var tuple] }
                || tuple != x0 || body[index + 1] is not
                {
                    Definition.Opcode: TestArity,
                    Operands: [var arityReject, var checkedTuple, { Value: var arity }]
                } || arityReject != rejected || checkedTuple != x0 || arity != 2
                || !IsProjection(
                    index + 2,
                    x0,
                    0,
                    x1
                )
                || body[index + 3] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var result, { Value.IsOne: true }, { Kind: BeamOperandParameters.Y, TypeIndex: null } payload]
                } || result != x0 || body[index + 4] is not
                {
                    Definition.Opcode: SelectValue,
                    Operands: [
                        var selector, var failedSelect, _, { Kind: BeamOperandParameters.Atom }, var left,
                    { Kind: BeamOperandParameters.Atom }, var right
                    ]
                } || selector != x1 || failedSelect != rejected || left == right)
            {
                continue;
            }

            flow ??= new BeamProjectionFlow(body);
            int select = index + 4;
            if (!flow.TryGetSingleForwardTarget(select, (int)left.Value, out int first)
                || !flow.TryGetSingleForwardTarget(select, (int)right.Value, out int second)
                || !flow.TryGetForwardLabel(select, (int)failedSelect.Value, out int defaultArm)
                || defaultArm >= functionEnd || !ReadsOriginalCaseFailure(defaultArm))
            {
                continue;
            }

            int hot = Math.Min(first, second);
            int other = Math.Max(first, second);
            int address = hot + 4;
            if (hot != index + 5 || other >= functionEnd || body[hot + 1] is not
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [var guardReject, var carrier, _, _]
                } || carrier != payload || !IsProjection(
                    hot + 2,
                    payload,
                    null,
                    x0
                )
                || body[hot + 3] is not
                {
                    Definition.Opcode: ExactEqualTest,
                    Operands: [
                        var fieldReject, var read,
                    { Kind: BeamOperandParameters.Atom or BeamOperandParameters.Integer }
                    ]
                } || fieldReject != guardReject || read != x0
                || !IsProjection(
                    address,
                    payload,
                    null,
                    null
                )
                || ReadGuardedProjectedCallArguments(body, address, ref flow) is not { } guard
                || !IsProjection(
                    guard.FirstArgument + 1,
                    payload,
                    null,
                    x3
                )
                || !ReadsTerminalError(other, payload))
            {
                continue;
            }

            int call = SkipLines(guard.AfterArguments);
            int lastGuardTest = guard.FirstArgument - 2;
            if (call >= functionEnd || body[lastGuardTest].Operands[0] != guardReject
                || !flow!.TryGetForwardLabel(hot + 1, (int)guardReject.Value, out int cold)
                || cold >= functionEnd || !ReadsTerminalError(cold, null)
                || !flow!.IsInitialized(
                    index + 3,
                    payload,
                    allowFramePredicates: true,
                    requireClearedSlot: true
                ) || !flow!.IsDead(
                    call,
                    payload,
                    allowBadRecordExit: true,
                    allowEntryFramePredicates: true
                ))
            {
                continue;
            }

            // The only unguarded incoming value is the producer's X0
            // result. The selected record and its guard scratch can use
            // two distinct X registers until the call's own setup.
            var replacements = new List<CodeEdit>();
            int[] uses = [index + 3, hot + 1, hot + 2, address, guard.FirstArgument, guard.FirstArgument + 1, other + 1];
            bool admitted = uses.All(use => AddCarrierRegisterEdits(
                code,
                body[use],
                payload,
                2,
                replacements
            ));
            for (int use = address + 2; admitted && use < guard.FirstArgument; use++)
            {
                admitted = !body[use].Operands.Any(operand =>
                    SameProjectionRegister(operand, x3) || SameProjectionRegister(operand, payload))
                    && AddCarrierRegisterEdits(
                        code,
                        body[use],
                        x2,
                        3,
                        replacements
                    );
            }

            if (admitted)
            {
                edits.AddRange(replacements);
            }
        }

        if (edits.Count == 0)
        {
            return null;
        }

        using var output = new MemoryStream();
        int copied = 0;
        foreach (CodeEdit edit in edits.OrderBy(edit => edit.Start))
        {
            if (edit.Start < copied)
            {
                return null;
            }

            output.Write(code.Span[copied..edit.Start]);
            BeamCompactWriter.Write(output, edit.Tag, edit.Value);
            copied = edit.End;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        bool IsProjection(
            int index,
            BeamOperand source,
            int? field,
            BeamOperand? destination
        ) => body[index] is
        {
            Definition.Opcode: GetTupleElement,
            Operands: [var original, var offset, var target]
        } && original == source && (!field.HasValue || offset.Value == field.Value)
            && (destination is null || target == destination);

        int SkipLines(int index)
        {
            while (index < functionEnd && body[index].Definition.Opcode == Line)
            {
                index++;
            }

            return index;
        }

        bool ReadsOriginalCaseFailure(int entry)
        {
            int failure = SkipLines(entry + 1);

            return failure < functionEnd && body[failure] is { Definition.Opcode: CaseEnd, Operands: [var original] }
                && original == x0;
        }

        bool ReadsTerminalError(int entry, BeamOperand? source)
        {
            if (entry + 2 >= functionEnd || body[entry + 1] is not
                { Definition.Opcode: Move, Operands: [var input, var target] }
                || target != x0 || (source is null ? input.Kind != BeamOperandParameters.Atom : input != source))
            {
                return false;
            }

            int exit = SkipLines(entry + 2);

            return exit < functionEnd && body[exit] is
            {
                Definition.Opcode: TailLocalCallWithFrame or TailExternalCallWithFrame,
                Operands: [{ Value.IsOne: true }, _, _]
            };
        }
    }

    private static bool AddCarrierRegisterEdits(
        ReadOnlyMemory<byte> code,
        BeamInstruction instruction,
        BeamOperand source,
        int destination,
        List<CodeEdit> edits
    )
    {
        if (!instruction.Operands.Any(operand => SameProjectionRegister(operand, source)))
        {
            return true;
        }

        int offset = instruction.Offset + 1;
        var cursor = new BeamCursor(code[offset..]);
        foreach (BeamOperand operand in instruction.Operands)
        {
            int start = cursor.Position;
            var compact = BeamCompactReader.Read(cursor);
            if (compact.WireTag == BeamOperandParameters.ExtendedTag)
            {
                if (compact.Value == BeamOperandParameters.TypedRegister)
                {
                    start = cursor.Position;
                    _ = BeamCompactReader.Read(cursor);
                    int registerEnd = cursor.Position;
                    _ = BeamCompactReader.Read(cursor);
                    Record(registerEnd);
                    continue;
                }

                if (compact.Value != BeamOperandParameters.Literal)
                {
                    return false;
                }

                _ = BeamCompactReader.Read(cursor);
            }

            Record(cursor.Position);

            void Record(int end)
            {
                if (SameProjectionRegister(operand, source))
                {
                    edits.Add(new(
                        offset + start,
                        offset + end,
                        XRegisterTag,
                        destination
                    ));
                }
            }
        }

        return true;
    }
}
