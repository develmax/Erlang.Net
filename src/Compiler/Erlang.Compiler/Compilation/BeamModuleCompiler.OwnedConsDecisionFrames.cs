using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record OwnedConsLeaf(int Argument, int Release, int[] Captures);

    private sealed record OwnedConsPrefix(int Projection, int Check, int Head);

    private sealed record OwnedConsDecision(
        OwnedConsPrefix? Prefix,
        int Split,
        int Retry,
        int Reject,
        int Live,
        Dictionary<int, int> Registers,
        List<(int Source, BeamOperand Field, int Target)> Projections,
        OwnedConsLeaf Atom,
        OwnedConsLeaf Tuple
    );

    private byte[]? SplitOwnedConsDecisionFrames(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(IsNonemptyList)
            || !code.Span.Contains(IsTaggedTuple) || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        // Only instructions within the existing label boundaries change.
        return RewriteOwnedConsDecisionFrames(code, decoded.Body, decoded.StackOperands);
    }

    private byte[]? RewriteOwnedConsDecisionFrames(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Slot)> stack
    )
    {
        using var output = new MemoryStream();
        int copied = 0;
        int entry = 0;
        BeamProjectionFlow? flow = null;
        int reverse = atoms.TryGetValue("lists", out int lists)
            && atoms.TryGetValue("reverse", out int reverseAtom)
            && imports.TryGetValue((lists, reverseAtom, 1), out int importIndex)
            ? emittedImports?[importIndex] ?? importIndex
            : -1;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = end + 1;
            if (!HasUnprotectedCallKillSchedule(body, start))
            {
                continue;
            }

            for (int frame = start; frame + 17 < end; frame++)
            {
                if (body[frame].Definition.Opcode != Allocate
                    || ReadOwnedConsDecision(
                        body,
                        frame,
                        end,
                        ref flow
                    ) is not { } plan
                    || ReadDisjointFrameOwnership(
                        body,
                        start,
                        end,
                        trackSiblingTrimEpochs: true
                    ) is not { } owner
                    || owner.Owners[frame + 1 - start] != frame
                    || owner.Owners[plan.Atom.Argument - start] != frame
                    || owner.Owners[plan.Tuple.Argument - start] != frame
                    || owner.Owners[plan.Reject - start] != frame)
                {
                    continue;
                }

                WriteDecision(frame, plan);
                frame = plan.Reject + 3;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        void WriteDecision(int frame, OwnedConsDecision plan)
        {
            if (plan.Prefix is { } prefix)
            {
                int projection = prefix.Projection;
                output.Write(code.Span[copied..body[projection].Offset]);
                output.WriteByte(GetTupleElement);
                WriteRegister(output, body[projection].Operands[0]);
                WriteUnsigned(body[projection].Operands[1].Value);
                WriteX(0);
                WriteTest(IsNonemptyList, body[projection + 1].Operands[0], 0);
                output.WriteByte(GetList);
                WriteX(0);
                WriteX(prefix.Head);
                WriteX(0);
                output.Write(code.Span[body[prefix.Check].Offset..body[frame].Offset]);
            }
            else
            {
                output.Write(code.Span[copied..body[frame].Offset]);
            }

            int split = plan.Split;
            var registers = plan.Registers;
            int list = OwnedConsSlotIndex(body[split].Operands[1]);
            int head = OwnedConsSlotIndex(body[split + 1].Operands[1]);
            int tail = OwnedConsSlotIndex(body[split + 1].Operands[2]);
            BeamOperand retryLabel = body[plan.Retry].Operands[0];
            BeamOperand failLabel = body[plan.Reject].Operands[0];

            WriteTest(IsNonemptyList, failLabel, registers[list]);
            foreach (var (source, field, target) in plan.Projections)
            {
                output.WriteByte(GetTupleElement);
                WriteX(source);
                WriteUnsigned(field.Value);
                WriteX(target);
            }

            output.WriteByte(GetList);
            WriteX(registers[list]);
            WriteX(registers[head]);
            WriteX(registers[tail]);

            // Atom and tagged tuple heads cannot overlap. A tuple that fails
            // the tag or tail predicate goes straight to the original reject.
            WriteTest(IsTuple, retryLabel, registers[head]);
            // The immediately dominating type check tested this same register.
            output.WriteByte(IsTaggedTuple);
            BeamCompactWriter.Write(output, LabelTag, failLabel.Value);
            output.WriteByte(ExtendedTypedRegisterOperand);
            WriteX(registers[head]);
            WriteUnsigned(GetOperandTypeIndex((TupleTypeBits, null, null, null)));
            WriteUnsigned(body[plan.Retry + 2].Operands[2].Value);
            WriteTupleJoinOperand(output, body[plan.Retry + 2].Operands[3]);
            WriteTest(ExactEqualTest, failLabel, registers[tail]);
            WriteTupleJoinOperand(output, body[plan.Retry + 3].Operands[2]);
            WriteLeaf(plan.Tuple, registers, plan.Live);

            Copy(plan.Retry);
            WriteTest(ExactEqualTest, failLabel, registers[head]);
            WriteTupleJoinOperand(output, body[split + 2].Operands[2]);
            WriteTest(IsNil, failLabel, registers[tail]);
            WriteLeaf(plan.Atom, registers, plan.Live);

            Copy(plan.Reject);
            int returned = registers[OwnedConsSlotIndex(body[plan.Reject + 1].Operands[0])];
            if (returned != 0)
            {
                output.WriteByte(Move);
                WriteX(returned);
                WriteX(0);
            }

            Copy(plan.Reject + 3);
            copied = body[plan.Reject + 4].Offset;
        }

        void WriteLeaf(OwnedConsLeaf leaf, Dictionary<int, int> registers, int live)
        {
            int argument = registers[OwnedConsSlotIndex(body[leaf.Argument].Operands[0])];
            int roots = Math.Max(live, Math.Max(argument, leaf.Captures.Max(slot => registers[slot])) + 1);
            output.WriteByte(Allocate);
            WriteUnsigned(leaf.Captures.Length);
            WriteUnsigned(roots);
            for (int index = 0; index < leaf.Captures.Length; index++)
            {
                output.WriteByte(Move);
                WriteX(registers[leaf.Captures[index]]);
                BeamCompactWriter.Write(output, YRegisterTag, index);
            }

            if (argument != 0)
            {
                output.WriteByte(Move);
                WriteX(argument);
                WriteX(0);
            }

            int afterArgument = body[leaf.Argument + 1].Offset;
            int release = body[leaf.Release].Offset;
            int operand = FirstStackOperandAtOrAfter(stack, afterArgument);
            int typed = ReadOwnedConsReverseField(leaf);
            int from = afterArgument;
            while (operand < stack.Count && stack[operand].Start < release)
            {
                var (begin, end, slot) = stack[operand++];
                WriteRange(begin);
                BeamCompactWriter.Write(output, YRegisterTag, Array.IndexOf(leaf.Captures, slot));
                from = end;
            }

            WriteRange(release);
            output.WriteByte(Deallocate);
            WriteUnsigned(leaf.Captures.Length);
            Copy(leaf.Release + 1);

            void WriteRange(int until)
            {
                if (typed >= from && typed < until)
                {
                    output.Write(code.Span[from..typed]);
                    output.WriteByte(ExtendedTypedRegisterOperand);
                    WriteX(0);
                    WriteUnsigned(GetOperandTypeIndex((ProperListTypeBits, null, null, null)));
                    from = typed + 1;
                }

                output.Write(code.Span[from..until]);
            }
        }

        int ReadOwnedConsReverseField(OwnedConsLeaf leaf)
        {
            int site = leaf.Argument + 2;
            int update = site + 2;
            if (reverse < 0 || update >= leaf.Release || body[site] is not
                {
                    Definition.Opcode: ExternalCall,
                    Operands: [{ Value.IsOne: true }, var import]
                } || import.Value != reverse
                || body[site + 1] is not { Definition.Opcode: TestHeap, Operands: [_, { Value.IsOne: true }] }
                || body[update] is not
                {
                    Definition.Opcode: UpdateRecord,
                    Operands: [_, _, { Kind: BeamOperandParameters.Y }, _, { Value: var count }, _, var value]
                } || count != 2 || !IsBareX0(value))
            {
                return -1;
            }

            // The successful resolved reverse result is a proper list and
            // no operation has overwritten X0 before the one-field update.
            int offset = body[update + 1].Offset - 1;

            return code.Span[offset] == XRegisterTag ? offset : -1;
        }

        void WriteTest(byte opcode, BeamOperand failure, int tested)
        {
            output.WriteByte(opcode);
            BeamCompactWriter.Write(output, LabelTag, failure.Value);
            WriteX(tested);
        }

        void WriteX(int register) => BeamCompactWriter.Write(output, XRegisterTag, register);

        void WriteUnsigned(System.Numerics.BigInteger value) => BeamCompactWriter.Write(output, BeamWire.UnsignedTag, value);

        void Copy(int index) => output.Write(code.Span[body[index].Offset..body[index + 1].Offset]);
    }

    private static OwnedConsDecision? ReadOwnedConsDecision(
        IReadOnlyList<BeamInstruction> body,
        int frame,
        int end,
        ref BeamProjectionFlow? flow,
        bool allowPrefix = true
    )
    {
        if (body[frame] is not
            {
                Operands: [{ Value.Sign: > 0 } width, { Value.Sign: > 0 } live]
            } || live.Value >= FirstReservedXRegister || width.Value > BeamOperandParameters.MaximumRegister)
        {
            return null;
        }

        int roots = (int)live.Value;
        var registers = new Dictionary<int, int>();
        var fields = new List<(int Source, BeamOperand Field, int Slot)>();
        var defined = new HashSet<int>();
        int split = frame + 1;
        if (body[split].Definition.Opcode == InitYRegisters)
        {
            split++;
        }

        while (split < end)
        {
            BeamInstruction instruction = body[split];
            if (instruction is { Definition.Opcode: Move, Operands: [var source, var destination] }
                && IsBareX(source) && source.Value < roots && IsSlot(destination))
            {
                if (!defined.Add(OwnedConsSlotIndex(destination)))
                {
                    return null;
                }

                registers.Add(OwnedConsSlotIndex(destination), (int)source.Value);
            }
            else if (instruction is { Definition.Opcode: GetTupleElement, Operands: [var tuple, var field, var result] }
                && IsBareX(tuple) && tuple.Value < roots && IsSlot(result))
            {
                if (!defined.Add(OwnedConsSlotIndex(result)))
                {
                    return null;
                }

                fields.Add(((int)tuple.Value, field, OwnedConsSlotIndex(result)));
            }
            else
            {
                break;
            }

            split++;
        }

        if (split + 4 >= end || fields.Count == 0 || body[split] is not
            { Definition.Opcode: IsNonemptyList, Operands: [{ Kind: BeamOperandParameters.Label } rejected, var list] }
            || !IsSlot(list) || !registers.ContainsKey(OwnedConsSlotIndex(list)) || body[split + 1] is not
            { Definition.Opcode: GetList, Operands: [var input, var head, var tail] }
            || input != list || !IsSlot(head) || !IsSlot(tail)
            || !defined.Add(OwnedConsSlotIndex(head)) || !defined.Add(OwnedConsSlotIndex(tail)) || body[split + 2] is not
            { Definition.Opcode: ExactEqualTest, Operands: [{ Kind: BeamOperandParameters.Label } retry, var first, { Kind: BeamOperandParameters.Atom }] }
            || first != head || body[split + 3] is not
            { Definition.Opcode: IsNil, Operands: [var sameRetry, var rest] }
            || sameRetry != retry || rest != tail)
        {
            return null;
        }

        flow ??= new BeamProjectionFlow(body);
        if (!flow.TryGetExclusiveForwardTarget(
            split + 2,
            split + 3,
            (int)retry.Value,
            out int alternative
        )
            || alternative + 4 >= end || body[alternative + 1] is not
            { Definition.Opcode: GetList, Operands: [var repeatedList, var repeatedHead, var repeatedTail] }
            || repeatedList != list || repeatedHead != head || repeatedTail != tail || body[alternative + 2] is not
            { Definition.Opcode: IsTaggedTuple, Operands: [var wrongHead, var testedHead, _, { Kind: BeamOperandParameters.Atom }] }
            || wrongHead != rejected || testedHead != head || body[alternative + 3] is not
            { Definition.Opcode: ExactEqualTest, Operands: [var wrongTail, var testedTail, { Kind: BeamOperandParameters.LiteralValue }] }
            || wrongTail != rejected || testedTail != tail
            || !flow.TryGetExclusiveForwardTarget(
                split,
                alternative + 2,
                alternative + 3,
                (int)rejected.Value,
                out int rejection
            ) || rejection + 4 > end
            || body[rejection + 1] is not { Definition.Opcode: Move, Operands: [var returned, var target] }
            || !IsSlot(returned) || !registers.ContainsKey(OwnedConsSlotIndex(returned)) || !IsBareX0(target)
            || body[rejection + 2] is not { Definition.Opcode: Deallocate, Operands: [var release] } || release != width
            || body[rejection + 3].Definition.Opcode != Return
            || ReadLeaf(split + 4, alternative) is not { } atom
            || ReadLeaf(alternative + 4, rejection) is not { } tupleArm)
        {
            return null;
        }

        int listSlot = OwnedConsSlotIndex(list);
        int originalCursor = registers[listSlot];
        OwnedConsPrefix? prefix = null;
        if (allowPrefix && originalCursor != 0 && registers.Values.Count(register => register == originalCursor) == 1
            && !registers.Values.Contains(0)
            && fields.All(field => field.Source != 0 && field.Source != originalCursor)
            && ReadOwnedConsPrefix(
                body,
                frame,
                originalCursor,
                flow
            ) is { } previous
            && (fields.Any(field => field.Source == previous.Head) || registers.Values.Contains(previous.Head)))
        {
            registers[listSlot] = 0;
            prefix = previous;
        }

        if (prefix is { } checkedPrefix && fields.All(field => field.Source == checkedPrefix.Head)
            && OrderPatternCaptures(atom, checkedPrefix) is { } orderedAtom
            && OrderPatternCaptures(tupleArm, checkedPrefix) is { } orderedTuple)
        {
            atom = orderedAtom;
            tupleArm = orderedTuple;
        }

        var projections = new List<(int Source, BeamOperand Field, int Target)>();
        for (int fieldIndex = 0; fieldIndex < fields.Count; fieldIndex++)
        {
            var (source, field, slot) = fields[fieldIndex];
            int targetRegister = LowestFreeX(fields.Skip(fieldIndex + 1).Select(item => item.Source));
            registers.Add(slot, targetRegister);
            projections.Add((source, field, targetRegister));
        }

        int listRegister = registers[OwnedConsSlotIndex(list)];
        int headRegister = LowestFreeX([]);
        registers.Add(OwnedConsSlotIndex(head), headRegister);

        // The split may reuse the discriminator only if no leaf or reject
        // still requires that original value, even through another slot.
        var kept = atom.Captures.Concat(tupleArm.Captures)
            .Append(OwnedConsSlotIndex(body[atom.Argument].Operands[0]))
            .Append(OwnedConsSlotIndex(body[tupleArm.Argument].Operands[0]))
            .Append(OwnedConsSlotIndex(returned));
        bool keepsInput = kept.Any(slot => registers.TryGetValue(slot, out int x) && x == listRegister);
        int tailRegister = keepsInput ? LowestFreeX([]) : listRegister;
        registers.Add(OwnedConsSlotIndex(tail), tailRegister);

        // The old outer split initialized the former cursor inside the
        // allocation root prefix. Preserve that root initialization.
        if (prefix is not null && !registers.Values.Contains(originalCursor))
        {
            return ReadOwnedConsDecision(
                body,
                frame,
                end,
                ref flow,
                allowPrefix: false
            );
        }

        return registers.Values.Max() < FirstReservedXRegister
            ? new(
                prefix,
                split,
                alternative,
                rejection,
                roots,
                registers,
                projections,
                atom,
                tupleArm
            )
            : null;

        OwnedConsLeaf? OrderPatternCaptures(OwnedConsLeaf leaf, OwnedConsPrefix provenPrefix)
        {
            int receiver = (int)body[provenPrefix.Projection].Operands[0].Value;
            if (receiver == provenPrefix.Head)
            {
                return null;
            }

            var ranks = new SortedDictionary<(int Group, System.Numerics.BigInteger Field), int>();
            foreach (int capture in leaf.Captures)
            {
                if (!ReadsIncomingCapture(leaf, capture))
                {
                    return null;
                }

                int fieldIndex = fields.FindIndex(field => field.Slot == capture);
                (int Group, System.Numerics.BigInteger Field) rank;
                if (capture == OwnedConsSlotIndex(head))
                {
                    rank = (0, 0);
                }
                else if (fieldIndex >= 0)
                {
                    rank = (1, fields[fieldIndex].Field.Value);
                }
                else if (registers.TryGetValue(capture, out int source) && source == provenPrefix.Head)
                {
                    rank = (2, 0);
                }
                else if (registers.TryGetValue(capture, out source) && source == receiver)
                {
                    rank = (3, 0);
                }
                else
                {
                    return null;
                }

                if (!ranks.TryAdd(rank, capture))
                {
                    return null;
                }
            }

            // Core allocates newer identities for nested matches, and
            // tuple-pattern fields receive consecutive descending identities.
            // All captures cross the call, so reversing the assigned Y order
            // places the nested bindings before their containing receivers.
            return leaf with { Captures = ranks.Values.ToArray() };
        }

        bool ReadsIncomingCapture(OwnedConsLeaf leaf, int capture)
        {
            for (int index = leaf.Argument + 3; index < leaf.Release; index++)
            {
                BeamInstruction instruction = body[index];
                int destination = StackWriteOperand(instruction);
                bool writes = destination >= 0 && IsCapture(instruction.Operands[destination]);
                if (instruction.Operands.Where((_, position) => position != destination).Any(IsCapture))
                {
                    return true;
                }

                if (writes)
                {
                    return false;
                }
            }

            return false;

            bool IsCapture(BeamOperand operand) =>
                operand.Kind == BeamOperandParameters.Y && operand.Value == capture;
        }

        int LowestFreeX(IEnumerable<int> protectedSources)
        {
            var occupied = registers.Values.Concat(protectedSources).ToHashSet();
            int register = 0;
            while (occupied.Contains(register))
            {
                register++;
            }

            return register;
        }

        bool IsSlot(BeamOperand operand) =>
            operand is { Kind: BeamOperandParameters.Y, TypeIndex: null } && operand.Value < width.Value;

        OwnedConsLeaf? ReadLeaf(int argument, int after)
        {
            if (argument + 4 >= after || body[argument] is not
                { Definition.Opcode: Move, Operands: [var value, var x0] }
                || !IsSlot(value) || !defined.Contains(OwnedConsSlotIndex(value)) || !IsBareX0(x0)
                || body[argument + 1].Definition.Opcode != Line
                || !IsOwnedProjectionUnaryCall(body[argument + 2])
                || body[after - 2] is not { Definition.Opcode: Deallocate, Operands: [var size] } || size != width
                || body[after - 1].Definition.Opcode != Return)
            {
                return null;
            }

            var captures = new HashSet<int>();
            for (int index = argument + 3; index < after - 2; index++)
            {
                BeamInstruction instruction = body[index];
                if (instruction.Definition.Opcode is not (TestHeap or Move or PutList or PutTuple or UpdateRecord))
                {
                    return null;
                }

                foreach (BeamOperand operand in instruction.Operands.Where(operand => operand.Kind == BeamOperandParameters.Y))
                {
                    int slot = OwnedConsSlotIndex(operand);
                    if (!defined.Contains(slot))
                    {
                        return null;
                    }

                    captures.Add(slot);
                }
            }

            return captures.Count > 0 && captures.Count < width.Value
                ? new(argument, after - 2, captures.Order().ToArray())
                : null;
        }
    }

    private static OwnedConsPrefix? ReadOwnedConsPrefix(
        IReadOnlyList<BeamInstruction> body,
        int frame,
        int originalCursor,
        BeamProjectionFlow flow
    )
    {
        if (frame < 5)
        {
            return null;
        }

        int last = frame - 1;
        int checkedHead = body[last].Definition.Opcode == TestArity ? last - 1 : last;
        if (checkedHead < 3 || body[checkedHead] is not
            {
                Definition.Opcode: IsTuple or IsTaggedTuple,
                Operands: [{ Kind: BeamOperandParameters.Label } rejected, var head, ..]
            } || !IsBareX(head) || head.Value <= 0 || head.Value == originalCursor
            || (checkedHead == last) != (body[checkedHead].Definition.Opcode == IsTaggedTuple))
        {
            return null;
        }

        if (checkedHead != last && (body[last].Operands[0] != rejected || body[last].Operands[1] != head))
        {
            return null;
        }

        int first = checkedHead - 3;
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        if (body[first] is not { Definition.Opcode: GetTupleElement, Operands: [var tuple, _, var list] }
            || !IsBareX(tuple) || tuple == x0 || list != head
            || body[first + 1] is not { Definition.Opcode: IsNonemptyList, Operands: [var empty, var tested] }
            || empty != rejected || tested != list
            || body[first + 2] is not { Definition.Opcode: GetList, Operands: [var input, var child, var tail] }
            || input != list || child != head || !IsBareX(tail) || tail.Value != originalCursor
            || !flow.IsGuardTemporaryDeadOnFailure(first + 1, x0, preserveHeapRoots: true)
            || !flow.IsGuardTemporaryDeadOnFailure(checkedHead, x0, preserveHeapRoots: true)
            || checkedHead != last && !flow.IsGuardTemporaryDeadOnFailure(last, x0, preserveHeapRoots: true))
        {
            return null;
        }

        return new(first, checkedHead, (int)head.Value);
    }

    private static int OwnedConsSlotIndex(BeamOperand operand) => checked((int)operand.Value);
}
