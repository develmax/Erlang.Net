using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private const int OwnedComparisonFrameWidth = 3;

    private sealed record OwnedCallAdjustment(
        int Entry,
        int Allocation,
        int ArgumentSetup,
        int ExternalCallIndex,
        int Predicate,
        int Adjustment,
        int Released,
        int Unadjusted,
        int End,
        bool Direct
    );

    private byte[]? TrimOwnedCallResultBranches(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled)
        {
            return null;
        }

        var labels = new List<(int Start, int End, int Label)>();
        var stack = new List<(int Start, int End, int Slot)>();
        var header = new BeamCodeHeader(
            BeamWire.FormatVersion,
            highestOpcode,
            labelCount,
            exports.Count + locals.Count,
            code
        );
        IReadOnlyList<BeamInstruction> body;
        try
        {
            body = BeamInstructionReader.Read(
                header,
                atoms.Count + 1,
                literalBytes.Count,
                (start, end, label) => labels.Add((start, end, label)),
                (start, end, slot) => stack.Add((start, end, slot))
            );
        }
        catch (Exception error) when (error is InvalidDataException or NotSupportedException)
        {
            return null;
        }

        using var output = new MemoryStream();
        int copied = 0;
        BeamProjectionFlow? flow = null;
        for (int info = 0; info + 8 < body.Count; info++)
        {
            if (body[info] is not { Definition.Opcode: FunctionInfo, Operands: [_, _, { Value: var arity }] }
                || arity != 2 || body[info + 1].Definition.Opcode != Label
                || body[info + 2] is not
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [{ Kind: BeamOperandParameters.Label } failed, var source, _, _]
                }
                || !IsBareX0(source)
                || !ReadsField(info + 3, 2) || !ReadsField(info + 4, 3) || !ReadsField(info + 5, 4)
                || body[info + 6] is not
                {
                    Definition.Opcode: SelectValue, Operands: [var selector, var fallback,
                    { Value: var alternatives }, { Kind: BeamOperandParameters.Atom }, { Kind: BeamOperandParameters.Label } case1,
                    { Kind: BeamOperandParameters.Atom }, { Kind: BeamOperandParameters.Label } case2]
                }
                || alternatives != 4 || !Register(selector, BeamOperandParameters.X, 4) || fallback != failed)
            {
                continue;
            }

            int end = info + 7;
            while (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                end++;
            }

            if (ReadDisjointFrameOwnership(body, info + 1, end) is not { } ownership)
            {
                continue;
            }

            flow ??= new BeamProjectionFlow(body);
            int first = info + 7;
            if (!Entry(first, case1) && !Entry(first, case2)
                || ReadArm(first, end, flow) is not { } firstArm
                || !Entry(firstArm.End, case1) && !Entry(firstArm.End, case2)
                || ReadArm(firstArm.End, end, flow) is not { } secondArm
                || !Entry(secondArm.End, fallback)
                || !Owns(firstArm) || !Owns(secondArm))
            {
                continue;
            }

            int[] sites = [first, firstArm.Unadjusted, secondArm.Entry, secondArm.Unadjusted, secondArm.End];
            int[] oldLabels = sites.Select(index => (int)body[index].Operands[0].Value).ToArray();
            var ordered = oldLabels.Order().ToArray();
            if (ordered.Distinct().Count() != sites.Length
                || labels.Any(label => oldLabels.Contains(label.Label)
                    && (label.Start < body[info + 2].Offset || label.Start >= body[secondArm.End].Offset)))
            {
                continue;
            }

            var remapped = oldLabels.Zip(ordered).ToDictionary();
            output.Write(code.Span[copied..body[info + 2].Offset]);
            WriteSpan(info + 2, first, remapped);
            WriteArm(firstArm);
            WriteArm(secondArm);
            WriteLabel(secondArm.End);
            copied = body[secondArm.End + 1].Offset;
            info = end - 1;

            bool Owns(OwnedCallAdjustment arm) =>
                Enumerable.Range(arm.Allocation + 1, arm.End - arm.Allocation - 1)
                    .All(index => body[index].Definition.Opcode == Return
                        || ownership.Owners[index - (info + 1)] == arm.Allocation);

            void WriteLabel(int index)
            {
                output.WriteByte(Label);
                Unsigned(remapped[(int)body[index].Operands[0].Value]);
            }

            void WriteArm(OwnedCallAdjustment arm)
            {
                WriteLabel(arm.Entry);
                WriteSpan(arm.Entry + 1, arm.Allocation + 1, remapped);
                WriteMove(
                    XRegisterTag,
                    2,
                    YRegisterTag,
                    0
                );
                WriteMove(
                    XRegisterTag,
                    3,
                    YRegisterTag,
                    1
                );
                WriteMove(
                    XRegisterTag,
                    1,
                    YRegisterTag,
                    2
                );
                WriteSpan(arm.ArgumentSetup, arm.ExternalCallIndex, remapped);
                // The saved input is now in X0, and each continuation defines a
                // fresh value in its old slot before any use of that slot.
                WriteFrameInitialization(output, [OwnedComparisonFrameWidth - 1]);
                WriteSpan(arm.ExternalCallIndex, arm.Predicate, remapped);
                WriteSpan(
                    arm.Predicate,
                    arm.Predicate + 1,
                    remapped,
                    slot => slot == 1 ? (YRegisterTag, 0)
                        : arm.Direct ? (XRegisterTag, 0) : (YRegisterTag, slot)
                );
                WriteMove(
                    YRegisterTag,
                    1,
                    XRegisterTag,
                    0
                );
                output.WriteByte(TrimFrame);
                Unsigned(OwnedComparisonFrameWidth - 1);
                Unsigned(1);
                WriteSpan(arm.Predicate + 2, arm.Adjustment, remapped);
                WriteSpan(
                    arm.Adjustment,
                    arm.Released,
                    remapped,
                    _ => (YRegisterTag, 0)
                );
                output.WriteByte(Deallocate);
                Unsigned(1);
                output.WriteByte(Return);
                WriteLabel(arm.Unadjusted);
                if (!arm.Direct)
                {
                    WriteMove(
                        YRegisterTag,
                        OwnedComparisonFrameWidth - 1,
                        XRegisterTag,
                        0
                    );
                }

                output.WriteByte(Deallocate);
                Unsigned(OwnedComparisonFrameWidth);
                output.WriteByte(Return);
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        bool ReadsField(int index, int destination) => body[index] is
        { Definition.Opcode: GetTupleElement, Operands: [var tuple, _, var field] }
            && IsBareX0(tuple) && Register(field, BeamOperandParameters.X, destination);

        bool Entry(int index, BeamOperand target) => body[index] is
        { Definition.Opcode: Label, Operands: [var id] } && id.Value == target.Value;

        void WriteSpan(
            int start,
            int finish,
            Dictionary<int, int> remapped,
            Func<int, (int Tag, int Value)>? rewrite = null
        )
        {
            int offset = body[start].Offset;
            int end = body[finish].Offset;
            var edits = labels.Where(label => label.Start >= offset && label.Start < end && remapped.ContainsKey(label.Label))
                .Select(label => (label.Start, label.End, Tag: (int)LabelTag, Value: remapped[label.Label]));
            if (rewrite is not null)
            {
                edits = edits.Concat(stack.Where(slot => slot.Start >= offset && slot.Start < end)
                    .Select(slot =>
                    {
                        var (tag, value) = rewrite(slot.Slot);

                        return (slot.Start, slot.End, tag, value);
                    }));
            }

            int from = offset;
            foreach (var edit in edits.OrderBy(edit => edit.Start))
            {
                output.Write(code.Span[from..edit.Start]);
                BeamCompactWriter.Write(output, edit.Tag, edit.Value);
                from = edit.End;
            }

            output.Write(code.Span[from..end]);
        }

        void WriteMove(
            int fromKind,
            int from,
            int toKind,
            int to
        )
        {
            output.WriteByte(Move);
            BeamCompactWriter.Write(output, fromKind, from);
            BeamCompactWriter.Write(output, toKind, to);
        }

        void Unsigned(int value) => BeamCompactWriter.Write(output, BeamWire.UnsignedTag, value);

        OwnedCallAdjustment? ReadArm(int entry, int limit, BeamProjectionFlow projectionFlow)
        {
            if (entry + 24 >= limit)
            {
                return null;
            }

            int prelude = SkipLines(entry + 1);
            int allocation = prelude + 1;
            if (allocation + 7 >= limit || !IsF0Arithmetic(body[prelude])
                || !Register(body[prelude].Operands[3], BeamOperandParameters.X, 3)
                || !IsBareX0(body[prelude].Operands[^1])
                || body[allocation] is not { Definition.Opcode: Allocate, Operands: [var width, _] }
                || width.Value != OwnedComparisonFrameWidth
                || !MoveRegister(
                    allocation + 1,
                    BeamOperandParameters.X,
                    3,
                    BeamOperandParameters.Y,
                    0
                )
                || !MoveRegister(
                    allocation + 2,
                    BeamOperandParameters.X,
                    2,
                    BeamOperandParameters.Y,
                    1
                )
                || !MoveRegister(
                    allocation + 3,
                    BeamOperandParameters.X,
                    1,
                    BeamOperandParameters.Y,
                    2
                ))
            {
                return null;
            }

            int local = SkipLines(allocation + 4);
            int external = local + 3;
            if (external + 10 >= limit || !OrdinaryCall(local, LocalCall, 1)
                || !MoveRegister(
                    local + 1,
                    BeamOperandParameters.X,
                    0,
                    BeamOperandParameters.X,
                    1
                )
                || !MoveRegister(
                    local + 2,
                    BeamOperandParameters.Y,
                    2,
                    BeamOperandParameters.X,
                    0
                )
                || !OrdinaryCall(external, ExternalCall, 2))
            {
                return null;
            }

            int result = external + 1;
            bool direct = MoveRegister(
                result,
                BeamOperandParameters.X,
                0,
                BeamOperandParameters.Y,
                2
            );
            int predicate = result + 1;
            if (!direct && !(IsF0Arithmetic(body[result]) && IsBareOrTypedX0(body[result].Operands[3])
                    && body[result].Operands[4].Kind == BeamOperandParameters.Integer
                    && Register(body[result].Operands[^1], BeamOperandParameters.Y, 2))
                || body[predicate] is not
                { Definition.Opcode: IsLess, Operands: [{ Kind: BeamOperandParameters.Label } failed, var threshold, var saved] }
                || !Register(threshold, BeamOperandParameters.Y, 1)
                || !Register(saved, BeamOperandParameters.Y, 2)
                || direct && saved.TypeIndex is not null
                || !MoveRegister(
                    predicate + 1,
                    BeamOperandParameters.Y,
                    0,
                    BeamOperandParameters.X,
                    0
                )
                || !projectionFlow.TryGetSingleForwardTarget(predicate, (int)failed.Value, out int unadjusted))
            {
                return null;
            }

            int adjustmentCall = SkipLines(predicate + 2);
            int adjustment = adjustmentCall + 1;
            int release = adjustment + 1;
            if (unadjusted != release + 2 || unadjusted + 4 > limit
                || !OrdinaryCall(adjustmentCall, LocalCall, 1)
                || !IsF0Arithmetic(body[adjustment])
                || !Register(body[adjustment].Operands[3], BeamOperandParameters.Y, 2)
                || !IsBareOrTypedX0(body[adjustment].Operands[4]) || !IsBareX0(body[adjustment].Operands[^1])
                || !ReturnWithFrame(release)
                || !MoveRegister(
                    unadjusted + 1,
                    BeamOperandParameters.Y,
                    2,
                    BeamOperandParameters.X,
                    0
                )
                || !ReturnWithFrame(unadjusted + 2))
            {
                return null;
            }

            return new(
                entry,
                allocation,
                allocation + 4,
                external,
                predicate,
                adjustment,
                release,
                unadjusted,
                unadjusted + 4,
                direct
            );

            int SkipLines(int index)
            {
                while (index < limit && body[index].Definition.Opcode == Line)
                {
                    index++;
                }

                return index;
            }

            bool ReturnWithFrame(int index) => body[index] is
            { Definition.Opcode: Deallocate, Operands: [var size] }
                && size.Value == OwnedComparisonFrameWidth && body[index + 1].Definition.Opcode == Return;
        }

        bool OrdinaryCall(int index, byte opcode, int arity) => body[index].Definition.Opcode == opcode
            && body[index].Operands[0].Value == arity;

        bool MoveRegister(
            int index,
            string fromKind,
            int from,
            string toKind,
            int to
        ) => body[index] is
        { Definition.Opcode: Move, Operands: [var input, var outputRegister] }
            && input.TypeIndex is null && outputRegister.TypeIndex is null
            && Register(input, fromKind, from) && Register(outputRegister, toKind, to);

        static bool IsF0Arithmetic(BeamInstruction instruction) => instruction is
        { Definition.Opcode: GcBifTwo, Operands: [{ Kind: BeamOperandParameters.Failure }, _, _, _, _, _] };

        static bool IsBareOrTypedX0(BeamOperand operand) => Register(operand, BeamOperandParameters.X, 0);

        static bool Register(BeamOperand operand, string kind, int index) => operand.Kind == kind && operand.Value == index;
    }
}
