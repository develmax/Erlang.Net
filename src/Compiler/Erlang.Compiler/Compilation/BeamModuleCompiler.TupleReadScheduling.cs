using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private readonly HashSet<(int Branch, int Tuple, int Position, int Output)> frameEntryTupleReads = [];

    private byte[]? SinkTupleReadsPastGuards(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body)
    {
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        var retained = frameEntryTupleReads.Count == 0 ? null : frameEntryTupleReads.Select(read =>
            (MapEmittedLabel(read.Branch), read.Tuple, read.Position, read.Output)).ToHashSet();
        int branch = 0;
        for (int index = 1; index + 2 < body.Count; index++)
        {
            if (retained is not null && body[index] is { Definition.Opcode: Label, Operands: [var label] })
            {
                branch = (int)label.Value;
            }

            // Some source scheduling plans define the projection before a separate
            // record check to place it in the live prefix at frame entry.
            if (retained is not null && body[index] is
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [{ Kind: BeamOperandParameters.X } tuple, var field, { Kind: BeamOperandParameters.X } outputRegister]
                }
                && retained.Contains((branch, (int)tuple.Value, (int)field.Value, (int)outputRegister.Value)))
            {
                continue;
            }
            int success = ReadAdjacentSiblingTest(body, index, ref flow)
                ? index + 2
                : FindCheckedTupleGuardSuccess(body, index, ref flow);
            if (success <= index + 1)
            {
                if (ReadTerminalProjectionConsumer(body, index) is { } terminal)
                {
                    output.Write(code.Span[copied..body[index].Offset]);
                    output.WriteByte(GetTupleElement);
                    BeamCompactWriter.Write(output, XRegisterTag, terminal.Tuple.Value);
                    BeamCompactWriter.Write(output, BeamWire.UnsignedTag, terminal.Field.Value);
                    BeamCompactWriter.Write(output, XRegisterTag, terminal.Destination.Value);
                    copied = body[index + 2].Offset;
                    index++;
                }

                continue;
            }

            int read = body[index].Offset;
            int guard = body[index + 1].Offset;
            int next = body[success].Offset;
            output.Write(code.Span[copied..read]);
            output.Write(code.Span[guard..next]);
            output.Write(code.Span[read..guard]);
            copied = next;
            index = success - 1;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

    }

    private static bool ReadAdjacentSiblingTest(
        IReadOnlyList<BeamInstruction> body,
        int index,
        ref BeamProjectionFlow? flow
    )
    {
        if (body[index - 1] is not { Definition.Opcode: GetTupleElement, Operands: [var tuple, _, var tested] }
            || body[index] is not { Definition.Opcode: GetTupleElement, Operands: [var source, _, var destination] }
            || source != tuple || source.Kind != BeamOperandParameters.X || destination.Kind != BeamOperandParameters.X
            || SameProjectionRegister(tested, destination)
            || !IsTypeTest(body[index + 1], tested))
        {
            return false;
        }

        // Replacing the tuple keeps an initialized term in this X root even on failure.
        // Other destinations can introduce an initialized register on the original failure path.
        bool replacesTuple = SameProjectionRegister(source, destination);

        return (flow ??= new BeamProjectionFlow(body)).IsGuardTemporaryDeadOnFailure(
            index + 1,
            destination,
            preserveHeapRoots: !replacesTuple
        );

        static bool IsTypeTest(BeamInstruction instruction, BeamOperand tested) =>
            instruction.Definition.Opcode is IsInteger or IsFloat or IsNumber or IsAtom
                or IsTuple or IsList or IsNonemptyList or IsNil or IsBinary or IsMap
            && instruction.Operands is [{ Kind: BeamOperandParameters.Label }, var value]
            && SameProjectionRegister(value, tested);
    }

    private static int FindCheckedTupleGuardSuccess(
        IReadOnlyList<BeamInstruction> body,
        int projection,
        ref BeamProjectionFlow? flow
    )
    {
        if (body[projection] is not
            {
                Definition.Opcode: GetTupleElement,
                Operands: [{ Kind: BeamOperandParameters.X } tuple, var field, { Kind: BeamOperandParameters.X } destination]
            }
            || field.Value < 0
            || !IsIndependentGuard(body[projection + 1], destination)
            || !HasLocalTupleCheck(
                body,
                projection,
                tuple,
                field
            ))
        {
            return -1;
        }

        // The success edge is the only path that now defines the projection.
        // Each earlier failure must preserve its original values and initialized GC roots.
        bool replacesTuple = SameProjectionRegister(tuple, destination);
        int success = projection + 1;
        while (success + 1 < body.Count && IsIndependentGuard(body[success], destination)
            && (flow ??= new BeamProjectionFlow(body)).IsGuardTemporaryDeadOnFailure(
                success,
                destination,
                preserveHeapRoots: !replacesTuple
            ))
        {
            success++;
        }

        return success;
    }

    private static (BeamOperand Tuple, BeamOperand Field, BeamOperand Destination)? ReadTerminalProjectionConsumer(
        IReadOnlyList<BeamInstruction> body,
        int projection
    )
    {
        if (body[projection] is not
            {
                Definition.Opcode: GetTupleElement,
                Operands: [{ Kind: BeamOperandParameters.X, TypeIndex: null } tuple, var field,
                { Kind: BeamOperandParameters.X, TypeIndex: null } temporary]
            }
            || body[projection + 1] is not
            {
                Definition.Opcode: Move,
                Operands: [{ Kind: BeamOperandParameters.X, TypeIndex: null } source,
                { Kind: BeamOperandParameters.X, TypeIndex: null } destination]
            }
            || !SameProjectionRegister(temporary, source)
            || !IsEntryFramePredicate(body[projection - 1].Definition.Opcode)
            || body[projection - 1].Operands[0].Kind != BeamOperandParameters.Label)
        {
            return null;
        }

        int terminal = projection + 2;
        while (terminal < body.Count && body[terminal].Definition.Opcode == Line)
        {
            terminal++;
        }

        if (terminal == body.Count)
        {
            return null;
        }

        BeamInstruction instruction = body[terminal];
        int arity = instruction.Definition.Opcode switch
        {
            Return => 1,
            TailLocalCall or TailExternalCall => (int)instruction.Operands[0].Value,
            _ => -1
        };

        // The intermediate value has no other consumer before the
        // terminal instruction and is outside its entire argument prefix.
        return destination.Value < arity && temporary.Value >= arity
            && HasLocalTupleCheck(
                body,
                projection,
                tuple,
                field
            )
            ? (tuple, field, destination)
            : null;
    }

    private static bool HasLocalTupleCheck(
        IReadOnlyList<BeamInstruction> body,
        int projection,
        BeamOperand tuple,
        BeamOperand field
    )
    {
        for (int index = projection - 1; index >= 0; index--)
        {
            BeamInstruction instruction = body[index];
            if (instruction is
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [{ Kind: BeamOperandParameters.Label }, var source, var arity, _]
                } && SameProjectionRegister(source, tuple))
            {
                return field.Value < arity.Value;
            }

            if (instruction is
                {
                    Definition.Opcode: TestArity,
                    Operands: [{ Kind: BeamOperandParameters.Label } failure, var checkedTuple, var checkedArity]
                }
                && SameProjectionRegister(checkedTuple, tuple)
                && index > 0
                && body[index - 1] is { Definition.Opcode: IsTuple, Operands: [var tupleFailure, var typed] }
                && tupleFailure == failure && SameProjectionRegister(typed, tuple))
            {
                return field.Value < checkedArity.Value;
            }

            if (!IsEntryFramePredicate(instruction.Definition.Opcode)
                && !(instruction is { Definition.Opcode: GetTupleElement, Operands: [_, _, var other] }
                    && !SameProjectionRegister(other, tuple)))
            {
                break;
            }
        }

        return false;
    }

    private static bool IsIndependentGuard(BeamInstruction instruction, BeamOperand destination) =>
        IsEntryFramePredicate(instruction.Definition.Opcode)
        && instruction.Operands[0].Kind == BeamOperandParameters.Label
        && !instruction.Operands.Skip(1).Any(operand => SameProjectionRegister(operand, destination));

}
