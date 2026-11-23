using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ScheduleEntryTupleFunctionCall(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !code.Span.Contains(CallFun)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        using var output = new MemoryStream(code.Length);
        int copied = 0;
        for (int info = 0; info + 8 < body.Count; info++)
        {
            int firstProjection = info + 4;
            int allocation = firstProjection + 2;
            if (body[info] is not { Definition.Opcode: FunctionInfo, Operands: [_, _, var inputArity] }
                || inputArity.Value < 2
                || body[info + 1].Definition.Opcode != Label
                || body[info + 2] is not { Definition.Opcode: IsTuple, Operands: [var failure, var tuple] }
                || !IsBareX0(tuple)
                || body[info + 3] is not
                {
                    Definition.Opcode: TestArity,
                    Operands: [var arityFailure, var checkedTuple, { Value: var tupleArity }]
                }
                || arityFailure != failure || checkedTuple != tuple || tupleArity != 2
                || body[firstProjection] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var firstSource, { Value.IsZero: true },
                    { Kind: BeamOperandParameters.X, TypeIndex: null } first]
                }
                || firstSource != tuple || first.Value != inputArity.Value
                || body[firstProjection + 1] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var secondSource, { Value.IsOne: true },
                    { Kind: BeamOperandParameters.X, TypeIndex: null } second]
                }
                || secondSource != tuple || second.Value != first.Value + 1
                || body[allocation] is not { Definition.Opcode: Allocate, Operands: [var width, var live] }
                || live.Value != second.Value + 1)
            {
                continue;
            }

            if (ReadSpilledEntryTupleFunctionCall(
                body,
                allocation,
                (int)inputArity.Value,
                first,
                second
            ) is { } spilled)
            {
                int arity = (int)inputArity.Value;
                int frame = arity - 1;
                output.Write(code.Span[copied..body[firstProjection].Offset]);
                output.WriteByte(Allocate);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, frame);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, arity);
                for (int input = arity - 1; input > 0; input--)
                {
                    WriteMove(
                        XRegisterTag,
                        input,
                        YRegisterTag,
                        frame - input
                    );
                }

                WriteField(0, 1);
                WriteField(1, 0);
                output.Write(code.Span[body[spilled.SetupEnd].Offset..body[spilled.Call + 1].Offset]);
                for (int input = arity - 1; input > 0; input--)
                {
                    WriteMove(
                        YRegisterTag,
                        frame - input,
                        XRegisterTag,
                        input
                    );
                }

                output.Write(code.Span[body[spilled.ReloadEnd].Offset..body[spilled.Tail].Offset]);
                output.WriteByte(TailLocalCallWithFrame);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, arity);
                BeamCompactWriter.Write(output, LabelTag, body[spilled.Tail].Operands[1].Value);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, frame);
                copied = body[spilled.Tail + 1].Offset;

                continue;
            }

            int move = allocation + 1;
            while (body[move] is
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.X, TypeIndex: null } savedArgument,
                    { Kind: BeamOperandParameters.Y, TypeIndex: null }]
                } && savedArgument.Value < inputArity.Value)
            {
                move++;
            }

            if (move + 2 >= body.Count
                || body[move] is not { Definition.Opcode: Move, Operands: [var high, var argument] }
                || high != second || !IsBareX0(argument)
                || body[move + 1] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var low, { Kind: BeamOperandParameters.X, TypeIndex: null, Value.IsOne: true }]
                }
                || low != first)
            {
                continue;
            }

            int call = move + 2;
            if (body[call].Definition.Opcode == Line)
            {
                call++;
            }

            if (call >= body.Count
                || body[call] is not { Definition.Opcode: CallFun, Operands: [{ Value.IsOne: true }] })
            {
                continue;
            }

            // The checked tuple is an incoming root. Field values need no
            // additional roots when projected after the argument saves.
            output.Write(code.Span[copied..body[firstProjection].Offset]);
            output.WriteByte(Allocate);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, width.Value);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, inputArity.Value);
            output.Write(code.Span[body[allocation + 1].Offset..body[move].Offset]);
            WriteField(0, 1);
            WriteField(1, 0);
            copied = body[move + 2].Offset;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        void WriteMove(
            byte sourceTag,
            int source,
            byte destinationTag,
            int destination
        )
        {
            output.WriteByte(Move);
            BeamCompactWriter.Write(output, sourceTag, source);
            BeamCompactWriter.Write(output, destinationTag, destination);
        }

        void WriteField(int index, int destination)
        {
            output.WriteByte(GetTupleElement);
            BeamCompactWriter.Write(output, XRegisterTag, 0);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, index);
            BeamCompactWriter.Write(output, XRegisterTag, destination);
        }
    }

    private static (int SetupEnd, int Call, int ReloadEnd, int Tail)? ReadSpilledEntryTupleFunctionCall(
        IReadOnlyList<BeamInstruction> body,
        int allocation,
        int arity,
        BeamOperand fun,
        BeamOperand argument
    )
    {
        int width = arity + 1;
        if (arity >= FirstReservedXRegister || body[allocation].Operands[0].Value != width)
        {
            return null;
        }

        int cursor = allocation + 1;
        if (body[cursor].Definition.Opcode == InitYRegisters)
        {
            cursor++;
        }

        if (cursor + width + arity + 4 >= body.Count)
        {
            return null;
        }

        var slots = new Dictionary<BeamOperand, BeamOperand>();
        var usedSlots = new HashSet<BeamOperand>();
        for (int index = 0; index < width; index++)
        {
            if (body[cursor++] is not
                {
                    Definition.Opcode: Move,
                    Operands: [
                    { Kind: BeamOperandParameters.X, TypeIndex: null } source,
                    { Kind: BeamOperandParameters.Y, TypeIndex: null } destination
                    ]
                }
                || source.Value <= 0 || source.Value > argument.Value || destination.Value >= width
                || !slots.TryAdd(source, destination) || !usedSlots.Add(destination))
            {
                return null;
            }
        }

        if (!slots.TryGetValue(fun, out var funSlot) || !slots.TryGetValue(argument, out var argumentSlot)
            || !ReadSlotMove(body[cursor++], 0, argumentSlot)
            || !ReadSlotMove(body[cursor++], 1, funSlot))
        {
            return null;
        }

        int setupEnd = cursor;
        if (body[cursor].Definition.Opcode == Line)
        {
            cursor++;
        }

        int call = cursor++;
        if (body[call] is not { Definition.Opcode: CallFun, Operands: [{ Value.IsOne: true }] })
        {
            return null;
        }

        var restored = new HashSet<BeamOperand>();
        for (int input = 1; input < arity; input++)
        {
            if (body[cursor++] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var source, { Kind: BeamOperandParameters.X, TypeIndex: null } destination]
                }
                || destination.Value <= 0 || destination.Value >= arity || !restored.Add(destination)
                || !slots.TryGetValue(destination, out var saved) || saved != source)
            {
                return null;
            }
        }

        int reloadEnd = cursor;
        if (body[cursor].Definition.Opcode == Line)
        {
            cursor++;
        }

        // Every slot was defined before the call. The two projections are
        // consumed by it, and the only fallthrough reads restore original
        // positions before the tail call releases this frame.
        return body[cursor] is
        {
            Definition.Opcode: TailLocalCallWithFrame,
            Operands: [var outgoing, { Kind: BeamOperandParameters.Label }, var tailFrame]
        } && outgoing.Value == arity && tailFrame.Value == width && cursor + 1 < body.Count
            ? (setupEnd, call, reloadEnd, cursor)
            : null;

        static bool ReadSlotMove(BeamInstruction instruction, int target, BeamOperand slot) =>
            instruction is
            {
                Definition.Opcode: Move,
                Operands: [var source, { Kind: BeamOperandParameters.X, TypeIndex: null } destination]
            } && source == slot && destination.Value == target;
    }
}
