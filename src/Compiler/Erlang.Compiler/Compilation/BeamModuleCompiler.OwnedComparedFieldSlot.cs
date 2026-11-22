using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ReserveComparedFieldSlot(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(TrimFrame)
            || !atoms.TryGetValue(ErlangModuleNames.Module, out int erlang)
            || !atoms.TryGetValue(AutoImportNames.ListToAtom, out int conversion)
            || !imports.TryGetValue((erlang, conversion, 1), out int import)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        int atomConversion = emittedImports?[import] ?? import;
        var (body, stack) = decoded;
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var x1 = new BeamOperand(BeamOperandParameters.X, 1);
        using var output = new MemoryStream();
        int copied = 0;
        int entry = 0;
        BeamProjectionFlow? flow = null;
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

            (int[] Owners, Dictionary<int, bool[]> Uses)? slots = null;
            for (int name = start + 1; name + 6 < end; name++)
            {
                if (body[name - 1] is not
                    {
                        Definition.Opcode: ExternalCall,
                        Operands: [{ Value.IsOne: true }, var target]
                    } || target.Value != atomConversion
                    || body[name] is not
                    {
                        Definition.Opcode: GetTupleElement,
                        Operands: [{ Kind: BeamOperandParameters.Y, Value.Sign: > 0, TypeIndex: null } holder, var field, var compared]
                    } || compared != x1
                    || body[name + 1] is not
                    {
                        Definition.Opcode: ExactEqualTest,
                        Operands: [{ Kind: BeamOperandParameters.Label, Value.Sign: > 0 } failure, var left, var right]
                    } || !SameProjectionRegister(left, x0) || right != x1
                    || body[name + 2] is not
                    {
                        Definition.Opcode: GetTupleElement,
                        Operands: [var carried, var payloadField, var payload]
                    } || carried != holder || payload != x0 || payloadField == field
                    || !IsMove(body[name + 3], x1, holder)
                    || body[name + 4] is not
                    { Definition.Opcode: TrimFrame, Operands: [{ Value.IsOne: true }, var hotWidth] }
                    || !IsOwnedProjectionUnaryCall(body[SkipComparedFieldLines(body, name + 5, end)])
                    || !(flow ??= new BeamProjectionFlow(body)).TryGetSingleForwardTarget(
                        name + 1,
                        (int)failure.Value,
                        out int cold
                    ) || cold <= name + 5 || cold + 4 >= end
                    || (slots ??= ReadDisjointFrameOwnership(
                        body,
                        start,
                        end,
                        trackSiblingTrimEpochs: true
                    )) is not { Owners: var owners, Uses: var uses }
                    || !uses.TryGetValue(owners[name - start], out bool[]? live)
                    || live.Length != hotWidth.Value + 1 || live.Length >= BeamOperandParameters.MaximumRegister)
                {
                    continue;
                }

                int allocation = owners[name - start];
                int width = live.Length;
                if (allocation < start || body[allocation].Definition.Opcode != Allocate || body[allocation].Offset < copied
                    || !HasClosedComparedFieldEntry(body, allocation, name)
                    || !HasComparedFieldExit(
                        body,
                        cold,
                        end,
                        width
                    )
                    || owners[cold - start] != allocation || owners[cold + 2 - start] != allocation
                    || Array.LastIndexOf(owners, allocation) != cold + 2 - start)
                {
                    continue;
                }

                // The child name is used in both arms: the success arm stores
                // it in the retired child carrier, and failure passes it to
                // the first binary effect. Its dedicated slot dies at both trims.
                int hotTrim = name + 4;
                ComparedCaptureOrder? order = ReadComparedCaptureOrder(
                    body,
                    start,
                    allocation,
                    name,
                    cold,
                    width,
                    flow
                );
                int at = FirstStackOperandAtOrAfter(stack, body[allocation].Offset);
                for (int index = allocation; index <= cold + 2; index++)
                {
                    int owner = owners[index - start];
                    bool hot = order is not null && owner == hotTrim;
                    if (index != allocation && owner != allocation && !hot)
                    {
                        continue;
                    }

                    int offset = body[index].Offset;
                    int next = body[index + 1].Offset;
                    if (index == allocation)
                    {
                        output.Write(code.Span[copied..offset]);
                        output.WriteByte(Allocate);
                        UInt(width + 1);
                        UInt(body[index].Operands[1].Value);
                        WriteFrameInitialization(output, [0]);
                        copied = next;
                        continue;
                    }

                    if (order is not null && index > allocation && index < allocation + width)
                    {
                        if (index == allocation + 1)
                        {
                            output.Write(code.Span[copied..offset]);
                            for (int capture = 0; capture < order.Sources.Length; capture++)
                            {
                                output.WriteByte(Move);
                                WriteRegister(output, order.Sources[capture]);
                                Y(capture + 2);
                            }

                            copied = body[allocation + width].Offset;
                        }

                        continue;
                    }

                    if (index == name || index == name + 1 || index == name + 3)
                    {
                        output.Write(code.Span[copied..offset]);
                        output.WriteByte(body[index].Definition.Opcode);
                        if (index == name)
                        {
                            Y(Map((int)holder.Value));
                            UInt(field.Value);
                            Y(0);
                        }
                        else if (index == name + 1)
                        {
                            BeamCompactWriter.Write(output, LabelTag, failure.Value);
                            output.WriteByte(ExtendedTypedRegisterOperand);
                            WriteRegister(output, x0);
                            UInt(GetOperandTypeIndex((AtomTypeBits, null, null, null)));
                            Y(0);
                        }
                        else
                        {
                            Y(0);
                            Y(Map((int)holder.Value));
                        }

                        copied = next;
                        continue;
                    }

                    if (index == cold + 1)
                    {
                        output.Write(code.Span[copied..offset]);
                        output.WriteByte(Move);
                        Y(0);
                        WriteRegister(output, x1);
                        output.WriteByte(Move);
                        Y(1);
                        Y(width);
                        copied = next;
                        continue;
                    }

                    if (index == name + 4 || index == cold + 2)
                    {
                        output.Write(code.Span[copied..offset]);
                        output.WriteByte(TrimFrame);
                        UInt(body[index].Operands[0].Value + 1);
                        UInt(body[index].Operands[1].Value);
                        copied = next;
                        continue;
                    }

                    while (at < stack.Count && stack[at].Start < offset)
                    {
                        at++;
                    }

                    while (at < stack.Count && stack[at].Start < next)
                    {
                        var (from, after, slot) = stack[at++];
                        output.Write(code.Span[copied..from]);
                        Y(hot ? Map(slot + 1) - 2 : Map(slot));
                        copied = after;
                    }
                }

                name = cold + 2;

                int Map(int slot) => order is null ? slot + 1 : order.Slots[slot];
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        void UInt(System.Numerics.BigInteger value) => BeamCompactWriter.Write(output, BeamWire.UnsignedTag, value);

        void Y(int slot) => BeamCompactWriter.Write(output, YRegisterTag, slot);
    }

    private static int SkipComparedFieldLines(IReadOnlyList<BeamInstruction> body, int index, int end)
    {
        while (index < end && body[index].Definition.Opcode == Line)
        {
            index++;
        }

        return index;
    }

    private static bool HasClosedComparedFieldEntry(IReadOnlyList<BeamInstruction> body, int allocation, int name) =>
        allocation < name - 1 && Enumerable.Range(allocation + 1, name - allocation - 2).All(index =>
            body[index].Definition.Opcode is Move or GetTupleElement or Line);

    private static bool HasComparedFieldExit(
        IReadOnlyList<BeamInstruction> body,
        int cold,
        int end,
        int width
    )
    {
        var local = new BeamOperand(BeamOperandParameters.Y, 0);
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        int call = SkipComparedFieldLines(body, cold + 4, end);

        return call < end
            && IsMove(body[cold + 1], local, new(BeamOperandParameters.Y, width - 1))
            && body[cold + 2] is { Definition.Opcode: TrimFrame, Operands: [var removed, { Value.IsOne: true }] }
            && removed.Value == width - 1
            && body[cold + 3] is { Definition.Opcode: Move, Operands: [var literal, var argument] }
            && IsNonRegisterValue(literal) && argument == x0
            && body[call] is { Definition.Opcode: LocalCall or ExternalCall, Operands: [var arity, _] }
            && arity.Value == 2;
    }
}
