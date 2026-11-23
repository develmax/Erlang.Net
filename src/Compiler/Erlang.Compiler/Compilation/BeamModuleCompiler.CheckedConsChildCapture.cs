using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ScheduleCheckedConsChildCapture(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(GetTupleElement)
            || !code.Span.Contains(TestArity) || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        using var output = new MemoryStream();
        int copied = 0;
        int entry = -1;
        bool unprotected = false;
        BeamProjectionFlow? flow = null;
        for (int check = 0; check + 10 < body.Count; check++)
        {
            if (body[check].Definition.Opcode == FunctionInfo)
            {
                entry = check + 1;
                unprotected = HasUnprotectedCallKillSchedule(body, entry);
                continue;
            }

            if (!unprotected || check <= entry || body[check] is not
                { Definition.Opcode: IsTuple, Operands: [var rejected, var child] }
                || !IsBareX(child) || body[check + 1] is not
                { Definition.Opcode: TestArity, Operands: [var childFailure, var checkedChild, var childWidth] }
                || checkedChild != child || childFailure != rejected || childWidth.Value <= 0
                || body[check + 2] is not
                { Definition.Opcode: IsNonemptyList, Operands: [var empty, var remaining] }
                || empty != rejected || !IsBareX(remaining) || remaining == child
                || body[check + 3] is not { Definition.Opcode: GetList, Operands: [var source, var parent, var tail] }
                || source != remaining || tail != remaining || !IsBareX(parent) || parent == remaining || parent == child
                || body[check + 4] is not { Definition.Opcode: IsTuple, Operands: [var badParent, var testedParent] }
                || badParent != rejected || testedParent != parent
                || body[check + 5] is not
                { Definition.Opcode: TestArity, Operands: [var wrongArity, var sizedParent, var parentWidth] }
                || wrongArity != rejected || sizedParent != parent || body[check + 6] is not
                { Definition.Opcode: Allocate, Operands: [var width, var live] }
                || live.Value != parent.Value + 1 || live.Value >= FirstReservedXRegister
                || parent.Value < 1 || child.Value >= live.Value || remaining.Value >= live.Value)
            {
                continue;
            }

            var newParent = new BeamOperand(BeamOperandParameters.X, live.Value);
            if (ReadCheckedConsChildSpills(
                body,
                check + 7,
                child,
                childWidth,
                parent,
                newParent
            ) is not { } spills)
            {
                continue;
            }

            BeamProjectionFlow proof = flow ??= new BeamProjectionFlow(body);
            if (!DeadOnRejection(check + 2) || !DeadOnRejection(check + 4) || !DeadOnRejection(check + 5))
            {
                continue;
            }

            var field = body[spills.Field].Operands;
            output.Write(code.Span[copied..body[check + 2].Offset]);
            output.WriteByte(GetTupleElement);
            WriteRegister(output, child);
            Unsigned(field[1]);
            WriteRegister(output, parent);
            Copy(check + 2);
            output.WriteByte(GetList);
            WriteRegister(output, remaining);
            WriteRegister(output, newParent);
            WriteRegister(output, remaining);
            output.WriteByte(IsTuple);
            WriteFailure(rejected);
            WriteRegister(output, newParent);
            output.WriteByte(TestArity);
            WriteFailure(rejected);
            WriteRegister(output, newParent);
            Unsigned(parentWidth);
            output.WriteByte(Allocate);
            Unsigned(width);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, live.Value + 1);

            int last = Math.Max(spills.Parent, spills.Field);
            for (int index = check + 7; index <= last; index++)
            {
                if (index == spills.Field || index == spills.Parent)
                {
                    output.WriteByte(Move);
                    WriteRegister(output, index == spills.Field ? parent : newParent);
                    WriteRegister(output, body[index].Operands[^1]);
                }
                else
                {
                    Copy(index);
                }
            }

            copied = body[last + 1].Offset;
            check = last;

            bool DeadOnRejection(int test) =>
                proof.IsGuardTemporaryDeadOnFailure(test, parent, preserveHeapRoots: true)
                && proof.IsGuardTemporaryDeadOnFailure(test, newParent, preserveHeapRoots: true);
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        void Copy(int index) => output.Write(code.Span[body[index].Offset..body[index + 1].Offset]);

        void Unsigned(BeamOperand operand) => BeamCompactWriter.Write(output, BeamWire.UnsignedTag, operand.Value);

        void WriteFailure(BeamOperand target) => BeamCompactWriter.Write(output, LabelTag, target.Value);
    }

    private static (int Field, int Parent)? ReadCheckedConsChildSpills(
        IReadOnlyList<BeamInstruction> body,
        int start,
        BeamOperand child,
        BeamOperand childWidth,
        BeamOperand parent,
        BeamOperand newParent
    )
    {
        int field = -1;
        int parentSpill = -1;
        for (int index = start; index < body.Count; index++)
        {
            BeamInstruction instruction = body[index];
            if (IsOwnedProjectionUnaryCall(instruction))
            {
                return field >= 0 && parentSpill >= 0 ? (field, parentSpill) : null;
            }

            if (instruction is
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var original, var offset, { Kind: BeamOperandParameters.Y, TypeIndex: null }]
                } && original == child)
            {
                if (field >= 0 || offset.Value < 0 || offset.Value >= childWidth.Value)
                {
                    return null;
                }

                field = index;
                continue;
            }

            if (instruction is
                {
                    Definition.Opcode: Move,
                    Operands: [var input, { Kind: BeamOperandParameters.Y, TypeIndex: null }]
                } && input == parent)
            {
                if (parentSpill >= 0)
                {
                    return null;
                }

                parentSpill = index;
                continue;
            }

            int destination = StackWriteOperand(instruction);
            if (instruction.Definition.Opcode is not (Line or Move or GetTupleElement or InitYRegisters)
                || (field < 0 && destination >= 0 && SameProjectionRegister(instruction.Operands[destination], child))
                || instruction.Operands.Any(operand =>
                    SameProjectionRegister(operand, parent) || SameProjectionRegister(operand, newParent)))
            {
                return null;
            }
        }

        return null;
    }
}
