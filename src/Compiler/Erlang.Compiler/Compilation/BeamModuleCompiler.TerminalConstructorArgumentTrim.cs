using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? TrimConsumedTerminalConstructorArgument(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(TestHeap)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var imported = imports.OrderBy(entry => entry.Value).Select(entry => entry.Key).ToArray();
        var names = atoms.OrderBy(entry => entry.Value).Select(entry => entry.Key).ToArray();
        byte[]? trimmed = RewriteConsumedTerminalConstructorArgument(
            code,
            decoded.Body,
            decoded.StackOperands,
            IsPureImport
        );
        if (trimmed is not null)
        {
            highestOpcode = Math.Max(highestOpcode, TrimFrame);
        }

        return trimmed;

        bool IsPureImport(int index)
        {
            var target = imported[index];

            return BeamTrimBifs.IsPure(names[target.Module - 1], names[target.Function - 1], target.Arity);
        }
    }

    private static byte[]? RewriteConsumedTerminalConstructorArgument(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Slot)> stack,
        Func<int, bool> isPureImport
    )
    {
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        var low = new BeamOperand(BeamOperandParameters.Y, 0);
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
            for (int release = start + 4; release + 1 < end; release++)
            {
                if (body[release] is not { Definition.Opcode: Deallocate, Operands: [var width] }
                    || width.Value < 2 || width.Value > BeamOperandParameters.MaximumRegister
                    || body[release + 1].Definition.Opcode != Return
                    || body[release - 1].Definition.Opcode is not (PutTuple or PutList or UpdateRecord))
                {
                    continue;
                }

                int call = release - 1;
                bool foundHeap = false;
                while (call > start && IsClosedConstructor(body[call]))
                {
                    foundHeap |= body[call].Definition.Opcode == TestHeap;
                    call--;
                }

                if (!foundHeap || body[call] is not
                    { Definition.Opcode: LocalCall or ExternalCall, Operands: [{ Value.Sign: > 0 }, _] })
                {
                    continue;
                }

                int site = call;
                while (site > start && body[site - 1].Definition.Opcode == Line)
                {
                    site--;
                }

                int load = site - 1;
                if (load <= start || body[load] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [
                        { Kind: BeamOperandParameters.Y, Value.Sign: > 0, TypeIndex: null } argument,
                        { Kind: BeamOperandParameters.X, Value.IsZero: true, TypeIndex: null }
                        ]
                    } || body[site].Offset < copied
                    || (ownership ??= ReadDisjointFrameOwnership(
                        body,
                        start,
                        end,
                        trackSiblingTrimEpochs: true
                    )) is not { } owners)
                {
                    continue;
                }

                int allocation = owners.Owners[load - start];
                if (allocation < start || body[allocation].Definition.Opcode is not (Allocate or AllocateHeap)
                    || owners.Uses[allocation].Length != width.Value
                    || Enumerable.Range(load + 1, release - load).Any(index => owners.Owners[index - start] != allocation))
                {
                    continue;
                }

                int oldArgument = (int)argument.Value;
                int frame = (int)width.Value;
                int[] lastUse = new int[frame];
                Array.Fill(lastUse, -1);
                for (int index = call + 1; index < release; index++)
                {
                    foreach (BeamOperand operand in body[index].Operands.Where(item => item.Kind == BeamOperandParameters.Y))
                    {
                        lastUse[(int)operand.Value] = index;
                    }
                }

                if (lastUse[oldArgument] >= 0 || lastUse.Where((_, slot) => slot != oldArgument).Any(position => position < 0)
                    || !HasRetainedLowRoot(
                        body,
                        call + 1,
                        release,
                        low
                    )
                    || !(flow ??= new BeamProjectionFlow(body)).IsInitialized(
                        site,
                        low,
                        allowOwnedCollectingWrite: true
                    ))
                {
                    continue;
                }

                lastUse[oldArgument] = load;
                int previousCall = load - 1;
                while (previousCall > allocation && (owners.Owners[previousCall - start] != allocation
                    || body[previousCall].Definition.Opcode is not (LocalCall or ExternalCall)))
                {
                    previousCall--;
                }

                if (previousCall <= allocation)
                {
                    continue;
                }

                var initialized = new bool[frame];
                initialized[0] = initialized[oldArgument] = true;
                if (PlanCallFrameTrim(
                    Enumerable.Range(0, frame).ToArray(),
                    lastUse,
                    initialized,
                    previousCall,
                    site,
                    body[call].Definition.Opcode == ExternalCall && isPureImport((int)body[call].Operands[1].Value),
                    [oldArgument]
                ) is not { } recipe || recipe.Removed != 1 || recipe.Kills.Length != 0
                    || recipe.Moves is not [(0, var destination)] || destination != oldArgument)
                {
                    continue;
                }

                // The call argument is already in X0. Preserve the surviving
                // low root in the exact Y slot the outgoing load has retired.
                output.Write(code.Span[copied..body[site].Offset]);
                output.WriteByte(Move);
                WriteRegister(output, low);
                WriteRegister(output, argument);
                output.WriteByte(TrimFrame);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, frame - 1);
                copied = body[site].Offset;
                int suffix = body[call + 1].Offset;
                int deallocate = body[release].Offset;
                int operandIndex = FirstStackOperandAtOrAfter(stack, suffix);
                while (operandIndex < stack.Count && stack[operandIndex].Start < deallocate)
                {
                    var (from, after, slot) = stack[operandIndex++];
                    output.Write(code.Span[copied..from]);
                    BeamCompactWriter.Write(output, YRegisterTag, slot == 0 ? destination - 1 : slot - 1);
                    copied = after;
                }

                copied = WritePostCallProjectionOperand(
                    output,
                    code,
                    deallocate,
                    copied,
                    0,
                    frame - 1
                );
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static bool IsClosedConstructor(BeamInstruction instruction) => instruction.Definition.Opcode is
        TestHeap or GetTupleElement or PutTuple or PutList or Move or UpdateRecord or Line;

    private static bool HasRetainedLowRoot(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        BeamOperand root
    )
    {
        for (int index = start; index < end; index++)
        {
            BeamInstruction instruction = body[index];
            int destination = StackAllocationDestination(instruction);
            if (instruction.Operands.Where((_, position) => position != destination)
                .Any(item => SameProjectionRegister(item, root)))
            {
                return true;
            }

            if (destination >= 0 && SameProjectionRegister(instruction.Operands[destination], root))
            {
                return false;
            }
        }

        return false;
    }
}
