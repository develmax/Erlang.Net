using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ScheduleBoundedTailCounter(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(GcBifTwo)
            || !atoms.TryGetValue(ErlangModuleNames.Module, out int erlangAtom)
            || !atoms.TryGetValue(ErlangOperators.Plus, out int plusAtom)
            || !imports.TryGetValue((erlangAtom, plusAtom, 2), out int addition)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        BeamOperand x0 = new(BeamOperandParameters.X, 0);
        using var output = new MemoryStream(code.Length);
        int copied = 0;
        int entry = -1;
        for (int call = 0; call < body.Count; call++)
        {
            if (body[call].Definition.Opcode == FunctionInfo)
            {
                entry = call + 1;
                continue;
            }

            if (entry < 0 || body[entry] is not { Definition.Opcode: Label, Operands: [var function] }
                || body[call] is not
                {
                    Definition.Opcode: TailLocalCallWithFrame,
                    Operands: [var arity, var target, var width]
                }
                || target.Value != function.Value || arity.Value < 3
                || arity.Value > BeamOperandParameters.MaximumRegister || width.Value != arity.Value - 1)
            {
                continue;
            }

            int count = (int)arity.Value;
            int start = call - count - 1;
            var destination = new BeamOperand(BeamOperandParameters.X, count - 1);
            if (start <= entry || body[start] is not
                {
                    Definition.Opcode: GcBifTwo,
                    Operands:
                    [
                    { Kind: BeamOperandParameters.Failure, Value.IsZero: true },
                    { Value.IsZero: true },
                        var operation,
                    { Kind: BeamOperandParameters.Y, TypeIndex: int type } counter,
                    { Kind: BeamOperandParameters.Integer, Value.IsOne: true },
                        var result
                    ]
                }
                || operation.Value != addition || result != x0 || type < 0 || type >= typeBits.Count
                || typeBits[type] is not (IntegerTypeBits, long _, long _, null)
                || !IsMove(body[start + 1], x0, destination))
            {
                continue;
            }

            var byDestination = new int[count - 1];
            Array.Fill(byDestination, -1);
            var reads = new HashSet<int> { (int)counter.Value };
            int literalCount = 0;
            for (int index = start + 2; index < call; index++)
            {
                if (body[index] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [var input, { Kind: BeamOperandParameters.X, TypeIndex: null } argument]
                    }
                    || argument.Value >= count - 1 || byDestination[(int)argument.Value] >= 0)
                {
                    break;
                }

                if (input is { Kind: BeamOperandParameters.Y, TypeIndex: null } && input.Value < width.Value
                    && reads.Add((int)input.Value))
                {
                    byDestination[(int)argument.Value] = index;
                }
                else if (input is { Kind: BeamOperandParameters.Atom, TypeIndex: null } && literalCount == 0)
                {
                    byDestination[(int)argument.Value] = index;
                    literalCount++;
                }
                else
                {
                    break;
                }
            }

            if (counter.Value >= width.Value || literalCount != 1 || byDestination.Contains(-1))
            {
                continue;
            }

            // The collecting BIF reads the old frame with an empty X prefix.
            // Its result is the final tail argument, and no sibling copy reads X.
            int oldDestination = body[start + 1].Offset - 1;
            if (code.Span[oldDestination] != XRegisterTag)
            {
                continue;
            }

            int[] order = IndependentCallMoveOrder(count - 1).Select(index => byDestination[index]).ToArray();
            SortOutgoingStackLoads(body, order);
            output.Write(code.Span[copied..oldDestination]);
            WriteRegister(output, destination);
            foreach (int index in order)
            {
                output.Write(code.Span[body[index].Offset..body[index + 1].Offset]);
            }

            copied = body[call].Offset;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static byte[]? OrderIndependentStackStores(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body)
    {
        using var output = new MemoryStream();
        int copied = 0;
        for (int start = 0; start + 1 < body.Count; start++)
        {
            if (!IsStackStore(body[start]))
            {
                continue;
            }

            int end = start + 1;
            while (end + 1 < body.Count && IsStackStore(body[end]))
            {
                end++;
            }

            if (end - start > 1)
            {
                // All sources are X registers, so a later write to the same Y slot
                // makes the earlier store dead throughout this run.
                IEnumerable<int> original = Enumerable.Range(start, end - start);
                int[] order = original.GroupBy(index => body[index].Operands[1].Value)
                    .OrderBy(stores => stores.Key)
                    .Select(stores => stores.Last())
                    .ToArray();
                if (!order.SequenceEqual(original))
                {
                    output.Write(code.Span[copied..body[start].Offset]);
                    foreach (int index in order)
                    {
                        output.Write(code.Span[body[index].Offset..body[index + 1].Offset]);
                    }

                    copied = body[end].Offset;
                }
            }

            start = end - 1;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        static bool IsStackStore(BeamInstruction instruction) => instruction is
        {
            Definition.Opcode: Move,
            Operands: [{ Kind: BeamOperandParameters.X }, { Kind: BeamOperandParameters.Y }]
        };
    }

    private byte[]? OrderTrimmedCallResultArguments(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !code.Span.Contains(TrimFrame)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        return OrderIndependentCallArguments(code, decoded.Body, completeTrimsOnly: true);
    }

    private byte[]? OrderFinalSplitStackArguments(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(InitYRegisters)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        return OrderIndependentCallArguments(
            code,
            decoded.Body,
            completeTrimsOnly: false,
            splitStackOnly: true
        );
    }

    private byte[]? OrderDictionaryListArguments(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(BifOne) || !code.Span.Contains(PutList)
            || !atoms.TryGetValue(ErlangModuleNames.Module, out int erlang)
            || !atoms.TryGetValue(AutoImportNames.Get, out int get)
            || !imports.TryGetValue((erlang, get, 1), out int import)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        return OrderIndependentCallArguments(
            code,
            decoded.Body,
            completeTrimsOnly: false,
            dictionaryLookupImport: emittedImports?[import] ?? import
        );
    }

    private static int[]? ReadDictionaryListArgumentOrder(
        IReadOnlyList<BeamInstruction> body,
        int end,
        int arity,
        int lookupImport
    )
    {
        if (arity < 4 || arity >= FirstReservedXRegister)
        {
            return null;
        }

        int count = arity - 2;
        int start = end - count;
        int constructor = start - 1;
        if (constructor < 1 || body[constructor] is not
            {
                Definition.Opcode: PutList,
                Operands: [_, _, { Kind: BeamOperandParameters.X, TypeIndex: null } list]
            } || list.Value <= 0 || list.Value >= arity)
        {
            return null;
        }

        var byDestination = new Dictionary<int, int>();
        for (int index = start; index < end; index++)
        {
            if (body[index] is not
                {
                    Definition.Opcode: Move,
                    Operands:
                    [
                    {
                        Kind: BeamOperandParameters.Atom or BeamOperandParameters.Nil or BeamOperandParameters.Integer
                            or BeamOperandParameters.LiteralValue, TypeIndex: null
                    },
                    { Kind: BeamOperandParameters.X, TypeIndex: null } destination
                    ]
                } || destination.Value <= 0 || destination.Value >= arity || destination == list
                || !byDestination.TryAdd((int)destination.Value, index))
            {
                return null;
            }
        }

        int producer = constructor;
        for (; producer >= 0 && body[producer].Definition.Opcode == PutList; producer--)
        {
            if (body[producer].Operands[^1] is { Kind: BeamOperandParameters.X, Value.IsZero: true })
            {
                return null;
            }
        }

        if (producer > 0 && body[producer] is { Definition.Opcode: TestHeap, Operands: [_, { Value.IsOne: true }] })
        {
            producer--;
        }

        if (producer < 0 || !IsInfallibleDictionaryLookup(body[producer], lookupImport))
        {
            return null;
        }

        // The lookup already owns X0, and the final cons owns one other
        // argument. setup_args omits identity copies before order_moves.
        int[] present = byDestination.OrderBy(entry => entry.Key).Select(entry => entry.Value).ToArray();

        return IndependentCallMoveOrder(count).Select(index => present[index]).ToArray();
    }

    private static byte[]? OrderIndependentCallArguments(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body) =>
        OrderIndependentCallArguments(code, body, completeTrimsOnly: false);

    private static byte[]? OrderIndependentCallArguments(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        bool completeTrimsOnly,
        bool splitStackOnly = false,
        int dictionaryLookupImport = -1
    )
    {
        using var output = new MemoryStream();
        int copied = 0;
        for (int call = 0; call < body.Count; call++)
        {
            int extraArguments = body[call].Definition.Opcode switch
            {
                LocalCall or TailLocalCall or TailLocalCallWithFrame => 0,
                ExternalCall or TailExternalCall or TailExternalCallWithFrame => 0,
                DynamicApply or TailDynamicApply => 2,
                _ => -1
            };
            if (extraArguments < 0)
            {
                continue;
            }

            int end = call;
            while (end > 0 && body[end - 1].Definition.Opcode == Line)
            {
                end--;
            }

            int arity = (int)body[call].Operands[0].Value + extraArguments;
            if (dictionaryLookupImport >= 0)
            {
                if (body[call].Definition.Opcode == ExternalCall && call - end <= 1
                    && ReadDictionaryListArgumentOrder(
                        body,
                        end,
                        arity,
                        dictionaryLookupImport
                    ) is { } dictionaryOrder)
                {
                    WriteMoves(end - dictionaryOrder.Length, end, dictionaryOrder);
                }

                continue;
            }


            if (!completeTrimsOnly && extraArguments == 0 && arity == 3
                && end >= 3 && IsIndependentArithmeticResultSetup(body, end))
            {
                WriteMoves(end - 2, end, [end - 1, end - 2]);
                continue;
            }

            if (!completeTrimsOnly && body[call].Definition.Opcode is (LocalCall or ExternalCall)
                && ReadSplitStackArgumentOrder(body, end, arity) is { } splitOrder)
            {
                WriteMoves(end - arity - 1, end - 2, splitOrder);
                continue;
            }

            if (splitStackOnly && body[call].Definition.Opcode is (LocalCall or ExternalCall)
                && ReadClearedCallResultArgumentOrder(body, end, arity) is { } clearedOrder)
            {
                WriteMoves(end - arity - 1, end - 1, clearedOrder);
                continue;
            }

            if (splitStackOnly)
            {
                continue;
            }

            int discardedSlots = 0;
            if (completeTrimsOnly && extraArguments == 0 && end > 0
                && body[call].Definition.Opcode is (LocalCall or ExternalCall)
                && body[end - 1] is
                {
                    Definition.Opcode: TrimFrame,
                    Operands: [{ Value: var removed }, { Value.IsZero: true }]
                } && removed > 0)
            {
                discardedSlots = (int)removed;
                end--;
                while (end > 0 && body[end - 1].Definition.Opcode == Line)
                {
                    end--;
                }
            }

            if (completeTrimsOnly && discardedSlots == 0)
            {
                continue;
            }

            int start = end;
            while (start > 0 && body[start - 1] is
                { Definition.Opcode: Move, Operands: [_, { Kind: BeamOperandParameters.X }] })
            {
                start--;
            }

            int count = end - start;
            if (discardedSlots > 0)
            {
                if (count >= 3 && count == arity
                    && ReadCallResultArgumentMoveOrder(
                        body,
                        start,
                        call,
                        arity,
                        discardedSlots
                    ) is { } trimmedOrder)
                {
                    WriteMoves(start, end, trimmedOrder);
                }

                continue;
            }

            if (count == 2 && body[call].Definition.Opcode is (LocalCall or ExternalCall)
                && WriteProjectedCopies(start, end, arity))
            {
                continue;
            }

            if (count == 2 && arity == 2 && extraArguments == 0
                && ReadSavedCallResultArguments(
                    body,
                    start,
                    end,
                    call,
                    out var kept
                ) is { } pair
                && kept is not null)
            {
                // The isolated receiver copy preceded delayed result storage in
                // SSA. The resulting temporary feeds X0 before the literal X1.
                int receiverMove = pair[0] == start ? start + 1 : start;
                WriteMoves(start, end, [receiverMove, pair[0]]);
                continue;
            }

            if (extraArguments == 0 && count == arity && count >= 3
                && ReadSavedCallResultArguments(
                    body,
                    start,
                    end,
                    call,
                    out var receiver
                ) is { } savedOrder)
            {
                if (receiver is null)
                {
                    WriteMoves(start, end, savedOrder);
                }
                else
                {
                    var argument = new BeamOperand(BeamOperandParameters.X, 0);
                    var scratch = new BeamOperand(BeamOperandParameters.X, 1);
                    output.Write(code.Span[copied..body[start].Offset]);
                    output.WriteByte(Move);
                    WriteRegister(output, receiver);
                    WriteRegister(output, scratch);
                    foreach (int index in savedOrder[..^1])
                    {
                        output.Write(code.Span[body[index].Offset..body[index + 1].Offset]);
                    }

                    output.WriteByte(Move);
                    WriteRegister(output, scratch);
                    WriteRegister(output, argument);
                    int last = savedOrder[^1];
                    output.Write(code.Span[body[last].Offset..body[last + 1].Offset]);
                    copied = body[end].Offset;
                }

                continue;
            }

            if (extraArguments == 0 && count >= 2
                && FindOutgoingResultSave(
                    body,
                    start,
                    end - 1,
                    arity
                ) is int save)
            {
                int[] retained = Enumerable.Range(start, count - 1).Where(index => index != save).ToArray();
                WriteMoves(start, end, retained);
                continue;
            }

            if (count >= 2 && count < arity && start >= 2
                && body[call].Definition.Opcode is (TailLocalCall or TailExternalCall)
                && body[start - 1].Definition.Opcode == Label
                && body[start - 2] is { Definition.Opcode: FunctionInfo, Operands: [_, _, var incoming] }
                && incoming.Value == arity - count
                && TryReadLiteralTailMoveOrder(
                    body,
                    start,
                    end,
                    arity,
                    out int[] literalOrder
                ))
            {
                WriteMoves(start, end, literalOrder);
                continue;
            }

            if (count < 2 || count > arity
                || start > 0 && body[start - 1].Definition.Opcode == Move)
            {
                continue;
            }

            if (count >= 3 && count == arity && extraArguments == 0
                && ReadCallResultArgumentMoveOrder(
                    body,
                    start,
                    call,
                    count
                ) is { } resultOrder)
            {
                WriteMoves(start, end, resultOrder);
                continue;
            }

            if (count == 3 && arity == count
                && ReadNestedTailCallMoveOrder(body, start, call) is { } nestedOrder)
            {
                WriteMoves(start, end, nestedOrder);
                continue;
            }

            bool retainedResult = extraArguments == 0 && count == arity - 1 && start > 0
                && body[start - 1].Definition.Opcode is (LocalCall or ExternalCall)
                && !CallFollowsFrameTrim(body, start - 1);
            BeamOperand? constructed = null;
            if (count != arity && !retainedResult)
            {
                if (start == 0)
                {
                    continue;
                }

                BeamInstruction constructor = body[start - 1];
                constructed = constructor.Definition.Opcode switch
                {
                    PutTuple => constructor.Operands[0],
                    PutList or BifZero => constructor.Operands[^1],
                    _ => null
                };
                if (constructed is not { Kind: BeamOperandParameters.X } || constructed.Value >= arity)
                {
                    continue;
                }
            }

            // An instruction can already have defined one outgoing register. Order only
            // the remaining moves, provided they do not depend on their destinations.
            // An immediately preceding call can already have supplied X0.
            var byDestination = new int[arity];
            Array.Fill(byDestination, -1);
            bool hasNonStackSource = false;
            bool independent = true;
            for (int index = start; index < end; index++)
            {
                BeamOperand source = body[index].Operands[0];
                BeamOperand destination = body[index].Operands[1];
                if (destination.Value >= arity || byDestination[(int)destination.Value] >= 0
                    || source.Kind == BeamOperandParameters.X && source.Value < arity)
                {
                    independent = false;
                    break;
                }

                byDestination[(int)destination.Value] = index;
                hasNonStackSource |= source.Kind != BeamOperandParameters.Y;
            }

            if (!independent || (retainedResult ? hasNonStackSource || byDestination[0] >= 0 : !hasNonStackSource && constructed is null)
                || constructed is not null && (byDestination[(int)constructed.Value] >= 0 || byDestination[0] < 0))
            {
                continue;
            }

            int[] present = count == arity ? byDestination : byDestination.Where(index => index >= 0).ToArray();
            int[] order;
            if (retainedResult)
            {
                order = present.OrderBy(index => body[index].Operands[0].Value).ToArray();
            }
            else
            {
                order = IndependentCallMoveOrder(count);
                for (int index = 0; index < order.Length; index++)
                {
                    order[index] = present[order[index]];
                }

                SortOutgoingStackLoads(body, order);
            }

            WriteMoves(start, end, order);
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        bool WriteProjectedCopies(int start, int end, int arity)
        {
            if (start < 2 || body[start - 2] is not
                {
                    Definition.Opcode: Move,
                    Operands: [
                    { Kind: BeamOperandParameters.X, TypeIndex: null } source,
                    { Kind: BeamOperandParameters.X, TypeIndex: null } scratch
                    ]
                }
                || body[start - 1] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [
                    { Kind: BeamOperandParameters.X, TypeIndex: null } tuple,
                        _,
                    { Kind: BeamOperandParameters.Y, TypeIndex: null } field
                    ]
                }
                || source == tuple || source.Value >= arity || tuple.Value >= arity || scratch.Value < arity
                || body[start].Operands is not [var fieldInput, var fieldOutput]
                || fieldInput != field || fieldOutput != source
                || body[start + 1].Operands is not [var savedInput, var savedOutput]
                || savedInput != scratch || savedOutput != tuple)
            {
                return false;
            }

            // The projection must read the original tuple before X is reused.
            // Its write to Y leaves the original argument in X until both copies.
            output.Write(code.Span[copied..body[start - 2].Offset]);
            output.Write(code.Span[body[start - 1].Offset..body[start].Offset]);
            output.WriteByte(Move);
            BeamCompactWriter.Write(output, XRegisterTag, source.Value);
            BeamCompactWriter.Write(output, XRegisterTag, tuple.Value);
            output.Write(code.Span[body[start].Offset..body[start + 1].Offset]);
            copied = body[end].Offset;

            return true;
        }

        void WriteMoves(int start, int end, int[] order)
        {
            if (order.SequenceEqual(Enumerable.Range(start, end - start)))
            {
                return;
            }

            output.Write(code.Span[copied..body[start].Offset]);
            foreach (int index in order)
            {
                output.Write(code.Span[body[index].Offset..body[index + 1].Offset]);
            }

            copied = body[end].Offset;
        }
    }

    private static bool TryReadLiteralTailMoveOrder(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        int arity,
        out int[] order
    )
    {
        order = [];
        var moves = new int[end - start];
        Array.Fill(moves, -1);
        for (int index = start; index < end; index++)
        {
            var source = body[index].Operands[0];
            if (source.TypeIndex is not null
                || source.Kind is not (BeamOperandParameters.LiteralValue or BeamOperandParameters.Integer
                    or BeamOperandParameters.Atom or BeamOperandParameters.Nil))
            {
                return false;
            }

            int slot = (int)body[index].Operands[1].Value - (arity - moves.Length);
            if (slot < 0 || slot >= moves.Length || moves[slot] >= 0)
            {
                return false;
            }

            moves[slot] = index;
        }

        // The unchanged incoming arguments contribute no moves to OTP's call setup.
        // Apply that same chain order only to the appended independent literals.
        order = IndependentCallMoveOrder(moves.Length);
        for (int index = 0; index < order.Length; index++)
        {
            order[index] = moves[order[index]];
        }

        return true;
    }

    private static int[]? ReadSavedCallResultArguments(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        int call,
        out BeamOperand? receiver
    )
    {
        receiver = null;
        var result = new BeamOperand(BeamOperandParameters.X, 0);
        if (start < 2 || body[start - 2].Definition.Opcode is not (LocalCall or ExternalCall)
            || body[call].Definition.Opcode is not (LocalCall or ExternalCall)
            || body[start - 1] is not
            {
                Definition.Opcode: Move,
                Operands: [var returned, { Kind: BeamOperandParameters.Y, TypeIndex: null } saved]
            }
            || returned != result)
        {
            return null;
        }

        var literals = new SortedDictionary<int, int>();
        BeamOperand? first = null;
        bool copiedResult = false;
        for (int index = start; index < end; index++)
        {
            var (source, destination) = (body[index].Operands[0], body[index].Operands[1]);
            if (destination == result)
            {
                if (first is not null || source is not { Kind: BeamOperandParameters.Y, TypeIndex: null })
                {
                    return null;
                }

                first = source;
            }
            else
            {
                // X1 can hold the deferred receiver. The saved call result
                // can feed a higher argument until the receiver overwrites X0.
                bool resultArgument = source == result && !copiedResult && first is null && destination.Value > 1;
                if (destination.TypeIndex is not null || destination.Value >= end - start
                    || !IsNonRegisterValue(source) && !resultArgument || !literals.TryAdd((int)destination.Value, index))
                {
                    return null;
                }

                copiedResult |= resultArgument;
            }
        }

        if (first is null)
        {
            return null;
        }

        // prefer_xregs keeps the saved result in X0. copy_retval puts an
        // independent first argument in X1 while X0 is reserved. In either
        // case order_moves leaves the assignment to X1 until the end.
        if (first != saved)
        {
            receiver = first;
        }

        return IndependentCallMoveOrder(end - start - 1)
            .Select(index => literals[index + 1]).ToArray();
    }

    private static int? FindOutgoingResultSave(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int restore,
        int arity
    )
    {
        if (body[restore].Operands is not
            [
            { Kind: BeamOperandParameters.X, TypeIndex: null, Value: var scratch },
            { Kind: BeamOperandParameters.X, TypeIndex: null, Value.IsZero: true }
            ] || scratch.IsZero || scratch < arity)
        {
            return null;
        }

        // Ordinary calls only consume X0 through X(arity-1). No move between
        // the save and the final restore can read or overwrite either register.
        for (int index = restore - 1; index >= start; index--)
        {
            BeamOperand source = body[index].Operands[0];
            BeamOperand destination = body[index].Operands[1];
            if (source.Kind == BeamOperandParameters.X && (source.Value.IsZero || source.Value == scratch)
                || destination.Value.IsZero || destination.Value == scratch)
            {
                return source is { Kind: BeamOperandParameters.X, TypeIndex: null, Value.IsZero: true }
                    && destination.TypeIndex is null && destination.Value == scratch ? index : null;
            }
        }

        return null;
    }

    private static int[]? ReadClearedCallResultArgumentOrder(IReadOnlyList<BeamInstruction> body, int end, int arity)
    {
        int start = end - arity - 1;
        if (arity != 3 || start < 1 || body[start - 1].Definition.Opcode is not (LocalCall or ExternalCall)
            || body[start] is not
            {
                Definition.Opcode: Move,
                Operands: [
                { Kind: BeamOperandParameters.X, Value.IsZero: true, TypeIndex: null },
                { Kind: BeamOperandParameters.X, Value: var destination, TypeIndex: null }
                ]
            } || destination != 2
            || body[start + 1] is not
            {
                Definition.Opcode: Move,
                Operands: [
                { Kind: BeamOperandParameters.Y, TypeIndex: null } receiver,
                { Kind: BeamOperandParameters.X, Value.IsZero: true, TypeIndex: null }
                ]
            }
            || body[start + 2] is not
            {
                Definition.Opcode: Move,
                Operands: [
                { Kind: BeamOperandParameters.Y, TypeIndex: null } independent,
                { Kind: BeamOperandParameters.X, Value.IsOne: true, TypeIndex: null }
                ]
            } || independent == receiver
            || body[end - 1] is not
            {
                Definition.Opcode: InitYRegisters,
                Operands: [_, var firstClear, var secondClear]
            } || !(firstClear == receiver && secondClear == independent
                || firstClear == independent && secondClear == receiver))
        {
            return null;
        }

        // beam_ssa_codegen:order_moves emits the independent component
        // before preserving the previous call's X0 and then replacing it.
        // The two cleared Y sources still have their original last reads.
        int[] byDestination = [start + 1, start + 2, start];

        return IndependentCallMoveOrder(arity).Select(destination => byDestination[destination]).ToArray();
    }

    private static int[]? ReadSplitStackArgumentOrder(
        IReadOnlyList<BeamInstruction> body,
        int end,
        int arity,
        bool ascendingDestinations = false
    )
    {
        int start = end - arity - 1;
        if (arity < 3 || start < 0
            || body[end - 2].Definition.Opcode != InitYRegisters
            || body[end - 1] is not
            {
                Definition.Opcode: Move,
                Operands: [
                { Kind: BeamOperandParameters.Y, TypeIndex: null } receiver,
                { Kind: BeamOperandParameters.X, Value.IsZero: true, TypeIndex: null }
                ]
            } || body[end - 2].Operands.Skip(1).Any(slot => SameProjectionRegister(slot, receiver)))
        {
            return null;
        }

        if (start > 0 && body[start - 1] is
            {
                Definition.Opcode: Move,
                Operands: [{ Kind: BeamOperandParameters.Y }, { Kind: BeamOperandParameters.X, Value.Sign: > 0 }]
            })
        {
            return null;
        }

        var sources = new HashSet<int>();
        int[] scheduled = ascendingDestinations ? Enumerable.Range(1, arity - 1).ToArray() : IndependentCallMoveOrder(arity);
        for (int index = start; index < end - 2; index++)
        {
            if (body[index] is not
                {
                    Definition.Opcode: Move,
                    Operands: [
                    { Kind: BeamOperandParameters.Y, TypeIndex: null } source,
                    { Kind: BeamOperandParameters.X, TypeIndex: null } destination
                    ]
                } || destination.Value != scheduled[index - start]
                || !sources.Add((int)source.Value))
            {
                return null;
            }
        }

        // beam_block sorts the adjacent loads by Y. The kill and following X0
        // move are outside this block and keep their original positions.
        return Enumerable.Range(start, arity - 1).OrderBy(index => body[index].Operands[0].Value).ToArray();
    }

    private static bool IsIndependentArithmeticResultSetup(IReadOnlyList<BeamInstruction> body, int end)
    {
        // The BIF has already supplied one argument. The two remaining
        // moves are independent, and beam_ssa_codegen:order_moves
        // leaves the X0 move next to the located call.
        return body[end - 3] is
        {
            Definition.Opcode: GcBifTwo,
            Operands: [{ Value.IsZero: true }, _, _, _, _, { Kind: BeamOperandParameters.X } produced]
        }
            && produced.Value > 0 && produced.Value < 3
            && body[end - 2] is
            {
                Definition.Opcode: Move,
                Operands: [
                { Kind: BeamOperandParameters.Y, TypeIndex: null },
                { Kind: BeamOperandParameters.X, TypeIndex: null, Value.IsZero: true }
                ]
            }
            && body[end - 1] is
            {
                Definition.Opcode: Move,
                Operands: [var constant, { Kind: BeamOperandParameters.X, TypeIndex: null } outgoing]
            }
            && outgoing.Value == 3 - produced.Value && IsNonRegisterValue(constant) && constant.TypeIndex is null;
    }

    private static void SortOutgoingStackLoads(IReadOnlyList<BeamInstruction> body, int[] order)
    {
        // OTP beam_block sorts contiguous Y-to-X loads before the final X0 move.
        for (int first = 0; first < order.Length - 1;)
        {
            int last = first;
            while (last < order.Length - 1 && body[order[last]].Operands[0] is
                { Kind: BeamOperandParameters.Y, TypeIndex: null })
            {
                last++;
            }

            if (last - first > 1)
            {
                int[] sorted = order[first..last].Reverse()
                    .OrderBy(index => body[index].Operands[0].Value)
                    .ToArray();
                sorted.CopyTo(order, first);
            }

            first = Math.Max(last, first + 1);
        }
    }

    private static int[]? ReadCallResultArgumentMoveOrder(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int call,
        int arity,
        int discardedSlots = 0
    )
    {
        bool framedTail = body[call].Definition.Opcode is TailLocalCallWithFrame or TailExternalCallWithFrame;
        if (start == 0 || body[start - 1].Definition.Opcode is not (LocalCall or ExternalCall)
            || !framedTail && body[call].Definition.Opcode is not (LocalCall or ExternalCall)
            || framedTail && discardedSlots != 0)
        {
            return null;
        }

        // The pre-trim form starts with the result copy, followed only by loads
        // from the caller's frame. All outgoing X registers are written once.
        if (discardedSlots > 0 && body[start].Operands is not
            [{ Kind: BeamOperandParameters.X, Value.IsZero: true, TypeIndex: null }, _])
        {
            return null;
        }

        var byDestination = new int[arity];
        Array.Fill(byDestination, -1);
        bool copiedResult = false;
        for (int index = start; index < start + arity; index++)
        {
            BeamOperand source = body[index].Operands[0];
            BeamOperand destination = body[index].Operands[1];
            if (destination.Value >= arity || byDestination[(int)destination.Value] >= 0
                || (discardedSlots > 0 || framedTail) && destination.TypeIndex is not null)
            {
                return null;
            }

            if (source is { Kind: BeamOperandParameters.X, Value.IsZero: true, TypeIndex: null })
            {
                if (copiedResult || destination.Value.IsZero || byDestination[0] >= 0)
                {
                    return null;
                }

                copiedResult = true;
            }
            else if (discardedSlots > 0)
            {
                if (source is not { Kind: BeamOperandParameters.Y, TypeIndex: null }
                    || source.Value >= discardedSlots)
                {
                    return null;
                }
            }
            else if (!IsNonRegisterValue(source)
                && !(destination.Value.IsZero && source is { Kind: BeamOperandParameters.Y, TypeIndex: null }))
            {
                return null;
            }

            byDestination[(int)destination.Value] = index;
        }

        if (!copiedResult || framedTail && body[byDestination[0]].Operands[0] is not
            { Kind: BeamOperandParameters.Y, TypeIndex: null })
        {
            return null;
        }

        // The ordered move chain leaves the replacement of X0 last, after its
        // only read. The remaining arguments have no dependencies on X.
        int[] order = IndependentCallMoveOrder(arity);
        for (int index = 0; index < order.Length; index++)
        {
            order[index] = byDestination[order[index]];
        }

        if (discardedSlots > 0)
        {
            SortOutgoingStackLoads(body, order);
        }

        return order;
    }

    private static int[]? ReadNestedTailCallMoveOrder(IReadOnlyList<BeamInstruction> body, int start, int call)
    {
        if (start == 0 || body[start - 1].Definition.Opcode is not (LocalCall or ExternalCall)
            || body[call].Definition.Opcode is not (TailLocalCallWithFrame or TailExternalCallWithFrame)
            || body[start].Operands is not
                [{ Kind: BeamOperandParameters.X, Value.IsZero: true }, { Kind: BeamOperandParameters.X, Value: var result }] || result != 2
            || body[start + 1].Operands is not
                [{ Kind: BeamOperandParameters.Atom or BeamOperandParameters.Integer or BeamOperandParameters.LiteralValue or BeamOperandParameters.Nil },
                { Kind: BeamOperandParameters.X, Value.IsZero: true }]
            || body[start + 2].Operands is not
                [{ Kind: BeamOperandParameters.Y }, { Kind: BeamOperandParameters.X, Value: var saved }] || saved != 1)
        {
            return null;
        }

        // OTP order_moves emits the separate Y load before the chain that reads and replaces X0.
        return [start + 2, start, start + 1];
    }

    private static bool CallFollowsFrameTrim(IReadOnlyList<BeamInstruction> body, int call)
    {
        int previous = call - 1;
        while (previous >= 0 && body[previous].Definition.Opcode is (Line or Move or Swap))
        {
            previous--;
        }

        // OTP preserves argument preparation through a frame trim.
        return previous >= 0 && body[previous].Definition.Opcode == TrimFrame;
    }
}
