using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record CheckedEntryCallSchedule(
        int End,
        int Allocation,
        int Width,
        int Spill,
        int Projection,
        bool IsUnary
    );

    private static IEnumerable<CheckedEntryCallSchedule> ReadCheckedEntryCallSchedules(
        IReadOnlyList<BeamInstruction> body
    )
    {
        var x1 = new BeamOperand(BeamOperandParameters.X, 1);
        BeamProjectionFlow? flow = null;
        for (int info = 0; info + 8 < body.Count; info++)
        {
            if (body[info].Definition.Opcode != FunctionInfo
                || body[info + 1].Definition.Opcode != Label
                || body[info + 2] is not
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [var failure, var checkedValue, var tupleWidth, _]
                }
                || !IsBareX0(checkedValue)
                || body[info + 3] is not
                {
                    Definition.Opcode: Allocate,
                    Operands: [{ Value: var frameWidth }, { Value.IsOne: true }]
                }
                || frameWidth < 2 || frameWidth > BeamOperandParameters.MaximumRegister)
            {
                continue;
            }

            int entry = info + 1;
            int allocation = info + 3;
            int end = info + 4;
            while (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                end++;
            }

            int carrierCopy = allocation + 1;
            while (carrierCopy < end && body[carrierCopy].Definition.Opcode is Line or InitYRegisters)
            {
                carrierCopy++;
            }

            if (carrierCopy + 4 >= end || body[carrierCopy] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var input, { Kind: BeamOperandParameters.Y, TypeIndex: null } carrier]
                }
                || !IsBareX0(input))
            {
                continue;
            }

            // Until the first call only the checked tuple's safe field may feed X0.
            int producer = carrierCopy + 1;
            if (body[producer] is
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var tuple, var field, var destination]
                }
                && (IsBareX0(tuple) || tuple == carrier) && field.Value >= 0 && field.Value < tupleWidth.Value
                && IsBareX0(destination))
            {
                producer++;
            }

            while (producer < end && body[producer].Definition.Opcode == Line)
            {
                producer++;
            }

            int spill = producer + 1;
            int projection = spill + 1;
            if (projection + 1 >= end
                || body[producer] is not { Definition.Opcode: LocalCall or ExternalCall, Operands: [_, _] }
                || body[spill] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var returned, { Kind: BeamOperandParameters.Y, TypeIndex: null } saved]
                }
                || !IsBareX0(returned) || saved == carrier
                || body[projection] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var loaded, var projectedField, var result]
                }
                || loaded != carrier || projectedField.Value < 0 || projectedField.Value >= tupleWidth.Value)
            {
                continue;
            }

            bool unary = IsBareX0(result);
            int consumer = projection + 1;
            if (!unary)
            {
                if (result != x1 || body[consumer] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [
                        {
                            Kind: BeamOperandParameters.Atom or BeamOperandParameters.LiteralValue
                            or BeamOperandParameters.Integer
                        }, var firstArgument]
                    }
                    || !IsBareX0(firstArgument))
                {
                    continue;
                }

                consumer++;
            }

            while (consumer < end && body[consumer].Definition.Opcode == Line)
            {
                consumer++;
            }

            if (consumer >= end || body[consumer] is not
                { Definition.Opcode: LocalCall or ExternalCall, Operands: [var arity, _] }
                || arity.Value != (unary ? 1 : 2))
            {
                continue;
            }

            flow ??= new BeamProjectionFlow(body);
            int guard = info + 2;
            if (!flow.TryGetSingleForwardTarget(guard, (int)failure.Value, out int fallback)
                || fallback <= consumer || fallback >= end
                || ReadDisjointFrameOwnership(body, entry, end) is not { } ownership
                || ownership.Uses.Count != 1
                || ownership.Owners[fallback - entry] != UnframedOwner
                || Enumerable.Range(carrierCopy, consumer - carrierCopy + 1).Any(index =>
                    ownership.Owners[index - entry] != allocation)
                || Enumerable.Range(allocation + 1, end - allocation - 1).Any(index =>
                    body[index].Operands.Any(operand => operand.Kind == BeamOperandParameters.Y)
                    && ownership.Owners[index - entry] != allocation))
            {
                continue;
            }

            yield return new CheckedEntryCallSchedule(
                end,
                allocation,
                (int)frameWidth,
                spill,
                projection,
                unary
            );
        }
    }
}
