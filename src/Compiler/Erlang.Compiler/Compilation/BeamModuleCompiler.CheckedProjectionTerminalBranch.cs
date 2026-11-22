using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? LocalizeCheckedLiteralReturnFrame(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(ExactEqualTest)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        int errorImport = atoms.TryGetValue(ErlangModuleNames.Module, out int erlang)
            && atoms.TryGetValue(AutoImportNames.Error, out int error)
            && imports.TryGetValue((erlang, error, 1), out int import) ? import : -1;
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        BeamProjectionFlow? flow = null;
        using var output = new MemoryStream(code.Length);
        int copied = 0;
        int entry = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = end + 1;
            (int[] Owners, Dictionary<int, bool[]> Uses)? ownership = null;
            for (int allocation = start + 2; allocation + 7 < end; allocation++)
            {
                if (body[allocation] is not { Definition.Opcode: Allocate, Operands: [var width, var live] }
                    || width.Value <= 0 || live.Value <= 0 || live.Value >= FirstReservedXRegister
                    || body[allocation - 1] is not
                    {
                        Definition.Opcode: IsTaggedTuple,
                        Operands: [_, { Kind: BeamOperandParameters.X, TypeIndex: null } checkedTuple, var arity, _]
                    } || checkedTuple.Value >= live.Value)
                {
                    continue;
                }

                var saved = new Dictionary<int, BeamOperand>();
                int projection = allocation + 1;
                for (; projection < end; projection++)
                {
                    if (body[projection] is
                        {
                            Definition.Opcode: Move,
                            Operands:
                            [
                            { Kind: BeamOperandParameters.X, TypeIndex: null } from,
                            { Kind: BeamOperandParameters.Y, TypeIndex: null } to
                            ]
                        } && from.Value < live.Value && to.Value < width.Value)
                    {
                        saved[(int)to.Value] = from;
                    }
                    else if (body[projection].Definition.Opcode == InitYRegisters)
                    {
                        foreach (BeamOperand slot in body[projection].Operands.Skip(1))
                        {
                            saved.Remove((int)slot.Value);
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                int guard = projection + 1;
                int resultMove = guard + 1;
                if (resultMove >= end || body[projection] is not
                    {
                        Definition.Opcode: GetTupleElement,
                        Operands: [var carrier, var field, { Kind: BeamOperandParameters.X, TypeIndex: null } tested]
                    } || Resolve(carrier, saved) != checkedTuple || field.Value >= arity.Value
                    || body[guard] is not
                    {
                        Definition.Opcode: ExactEqualTest,
                        Operands:
                        [
                        { Kind: BeamOperandParameters.Label } rejected, var subject,
                        {
                            Kind: BeamOperandParameters.Atom or BeamOperandParameters.Integer
                                or BeamOperandParameters.LiteralValue or BeamOperandParameters.Nil
                        }
                        ]
                    } || subject != tested
                    || body[resultMove] is not
                    {
                        Definition.Opcode: Move, Operands: [var rawValue, var returnRegister]
                    } || returnRegister != x0 || rawValue == tested
                    || Resolve(rawValue, saved) is not { TypeIndex: null } value
                    || (value.Kind == BeamOperandParameters.X && value.Value >= live.Value)
                    || value.Kind is not (BeamOperandParameters.X or BeamOperandParameters.Atom or BeamOperandParameters.Integer
                        or BeamOperandParameters.LiteralValue or BeamOperandParameters.Nil))
                {
                    continue;
                }

                int terminal = resultMove + 1;
                while (terminal < end && body[terminal].Definition.Opcode == Line)
                {
                    terminal++;
                }

                if (terminal + 1 >= end)
                {
                    continue;
                }

                bool returns = body[terminal] is { Definition.Opcode: Deallocate, Operands: [var released] }
                    && released == width && body[terminal + 1].Definition.Opcode == Return;
                bool tailError = errorImport >= 0 && body[terminal] is
                {
                    Definition.Opcode: TailExternalCallWithFrame, Operands: [{ Value.IsOne: true }, var called, var size]
                } && called.Value == errorImport && size == width;
                bool exitingCall = errorImport >= 0 && body[terminal] is
                {
                    Definition.Opcode: ExternalCall, Operands: [{ Value.IsOne: true }, var raised]
                } && raised.Value == errorImport && body[terminal + 1].Definition.Opcode == Jump;
                if (!returns && !tailError && !exitingCall)
                {
                    continue;
                }

                int cold = terminal + (tailError ? 1 : 2);
                if (cold + 1 >= end
                    || !(flow ??= new BeamProjectionFlow(body)).TryGetSingleForwardTarget(guard, (int)rejected.Value, out int target)
                    || target != cold || (ownership ??= ReadDisjointFrameOwnership(
                        body,
                        start,
                        end,
                        trackSiblingTrimEpochs: true
                    )) is not { Owners: var owners }
                    || owners[guard - start] != allocation || owners[cold - start] != allocation)
                {
                    continue;
                }

                // Both paths observe the original formals. The matching leaf
                // needs no Y bank. The rejecting path recreates the original
                // frame before restoring any live use of the projected value.
                var scratch = new BeamOperand(BeamOperandParameters.X, live.Value);
                output.Write(code.Span[copied..body[allocation].Offset]);
                output.WriteByte(GetTupleElement);
                WriteRegister(output, checkedTuple);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, field.Value);
                WriteRegister(output, scratch);
                var args = new BeamCursor(code[(body[guard].Offset + 1)..]);
                _ = BeamCompactReader.Read(args);
                _ = BeamCompactReader.Read(args);
                output.WriteByte(ExactEqualTest);
                BeamCompactWriter.Write(output, LabelTag, rejected.Value);
                WriteRegister(output, scratch);
                output.Write(code.Span[(body[guard].Offset + 1 + args.Position)..body[resultMove].Offset]);
                if (rawValue.Kind == BeamOperandParameters.Y)
                {
                    if (value != x0)
                    {
                        output.WriteByte(Move);
                        WriteRegister(output, value);
                        WriteRegister(output, x0);
                    }
                }
                else
                {
                    output.Write(code.Span[body[resultMove].Offset..body[resultMove + 1].Offset]);
                }

                output.Write(code.Span[body[resultMove + 1].Offset..body[terminal].Offset]);
                if (returns)
                {
                    output.WriteByte(Return);
                }
                else
                {
                    output.WriteByte(TailExternalCall);
                    BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
                    BeamCompactWriter.Write(output, BeamWire.UnsignedTag, errorImport);
                }

                output.Write(code.Span[body[cold].Offset..body[cold + 1].Offset]);
                bool retainsResult = !flow.IsEntryTemporaryDead(cold + 1, tested);
                output.WriteByte(Allocate);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, width.Value);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, retainsResult ? live.Value + 1 : live.Value);
                output.Write(code.Span[body[allocation + 1].Offset..body[projection].Offset]);
                if (retainsResult && tested != scratch)
                {
                    output.WriteByte(Move);
                    WriteRegister(output, scratch);
                    WriteRegister(output, tested);
                }

                copied = body[cold + 1].Offset;
                // The remaining ownership information describes the old frame.
                break;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        static BeamOperand? Resolve(BeamOperand value, Dictionary<int, BeamOperand> saved) =>
            value.Kind == BeamOperandParameters.Y ? saved.GetValueOrDefault((int)value.Value) : value;
    }

    private static EntryFramePlacement? PlanCheckedProjectionTerminalBranch(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int allocation,
        int end,
        Func<BeamInstruction, bool>? isFramelessExternalTail
    )
    {
        int failure = EntryFrameFailureLabel(body, start);
        if (failure < 0 || allocation != start + 2 || body[start + 1] is not
            {
                Definition.Opcode: IsTaggedTuple,
                Operands: [
                { Kind: BeamOperandParameters.Label } shapeFailure,
                { Kind: BeamOperandParameters.X, TypeIndex: null } tuple,
                { Kind: BeamOperandParameters.Unsigned } arity,
                { Kind: BeamOperandParameters.Atom }
                ]
            } || shapeFailure.Value != failure || body[allocation] is not
            {
                Definition.Opcode: Allocate,
                Operands: [{ Value.Sign: > 0 } width, { Value.Sign: > 0 } live]
            } || live.Value >= FirstReservedXRegister || tuple.Value >= live.Value)
        {
            return null;
        }

        var saved = new Dictionary<int, int>();
        int first = allocation + 1;
        while (first < end)
        {
            BeamInstruction instruction = body[first];
            if (instruction is
                {
                    Definition.Opcode: Move,
                    Operands: [
                    { Kind: BeamOperandParameters.X, TypeIndex: null } source,
                    { Kind: BeamOperandParameters.Y, TypeIndex: null } destination
                    ]
                } && source.Value < live.Value && destination.Value < width.Value)
            {
                saved[(int)destination.Value] = (int)source.Value;
            }
            else if (instruction.Definition.Opcode == InitYRegisters)
            {
                foreach (var slot in instruction.Operands.Skip(1))
                {
                    saved.Remove((int)slot.Value);
                }
            }
            else
            {
                break;
            }

            first++;
        }

        int scratch = (int)live.Value;
        int guard = first + 1;
        int heap = guard + 1;
        while (heap < end && body[heap].Definition.Opcode == Line)
        {
            heap++;
        }

        if (heap + 1 >= end || body[first] is not
            {
                Definition.Opcode: GetTupleElement,
                Operands: [
                    var owner,
                { Kind: BeamOperandParameters.Unsigned } field,
                { Kind: BeamOperandParameters.Y, TypeIndex: null } projected
                ]
            } || Resolve(owner) != (int)tuple.Value || field.Value >= arity.Value
            || projected.Value >= width.Value || saved.ContainsKey((int)projected.Value)
            || body[guard] is not
            {
                Definition.Opcode: ExactEqualTest,
                Operands: [
                { Kind: BeamOperandParameters.Label, Value.Sign: > 0 } fallback,
                    var input,
                { Kind: BeamOperandParameters.Atom or BeamOperandParameters.Integer or BeamOperandParameters.LiteralValue }
                ]
            } || input != projected || fallback.Value == failure
            || body[heap] is not { Definition.Opcode: TestHeap, Operands: [_, var originalHeapLive] })
        {
            return null;
        }

        saved.Add((int)projected.Value, scratch);
        var delayed = new HashSet<int> { first, guard };
        var defined = new HashSet<int>();
        int required = 0;
        int terminal = heap + 1;
        for (; terminal < end; terminal++)
        {
            BeamInstruction instruction = body[terminal];
            if (instruction.Definition.Opcode == Line)
            {
                continue;
            }

            if (instruction is not
                {
                    Definition.Opcode: PutTuple,
                    Operands: [{ Kind: BeamOperandParameters.X, TypeIndex: null } result, _, ..]
                } || result.Value > scratch)
            {
                break;
            }

            foreach (var value in instruction.Operands.Skip(2))
            {
                if (value.Kind is not (BeamOperandParameters.X or BeamOperandParameters.Y))
                {
                    continue;
                }

                if (value.TypeIndex is not null || Resolve(value) is not int register || register > scratch)
                {
                    return null;
                }

                if (!defined.Contains(register))
                {
                    required = Math.Max(required, register + 1);
                }
            }

            defined.Add((int)result.Value);
            delayed.Add(terminal);
        }

        if (terminal >= end || !defined.Contains(0) || originalHeapLive.Value > scratch + 1)
        {
            return null;
        }

        BeamInstruction exit = body[terminal];
        int alternative;
        if (exit is
            {
                Definition.Opcode: TailExternalCallWithFrame,
                Operands: [{ Value.IsOne: true }, _, var tailWidth]
            } && tailWidth == width && isFramelessExternalTail?.Invoke(exit) == true)
        {
            alternative = terminal + 1;
        }
        else if (exit is { Definition.Opcode: Deallocate, Operands: [var releaseWidth] }
            && releaseWidth == width && terminal + 1 < end && body[terminal + 1].Definition.Opcode == Return)
        {
            alternative = terminal + 2;
        }
        else
        {
            return null;
        }

        // The only entry to the moved frame is the rejecting edge. The
        // successful block has exited before any protected fallback code.
        int restore = alternative + 1;
        if (restore >= end || body[alternative] is not { Definition.Opcode: Label, Operands: [var target] }
            || target.Value != fallback.Value || body[restore].Definition.Opcode is Label or Allocate or AllocateHeap
            || Enumerable.Range(start, end - start).Any(index =>
                body[index].Definition.Name is "debug_line" or "executable_line"
                || index != guard && body[index].Operands.Any(operand =>
                    operand.Kind == BeamOperandParameters.Label && operand.Value == fallback.Value)))
        {
            return null;
        }

        delayed.Add(heap);
        delayed.Add(terminal);

        return new EntryFramePlacement(
            first,
            saved,
            delayed,
            new Dictionary<int, int> { [restore] = scratch + 1 },
            new Dictionary<int, int> { [heap] = required },
            new Dictionary<int, (int Slot, int Register)[]> { [restore] = [((int)projected.Value, scratch)] }
        );

        int? Resolve(BeamOperand value) => value.Kind switch
        {
            BeamOperandParameters.X when value.Value < live.Value => (int)value.Value,
            BeamOperandParameters.Y when saved.TryGetValue((int)value.Value, out int register) => register,
            _ => null
        };
    }
}
