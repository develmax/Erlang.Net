using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? SinkFallthroughOwnedProjections(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !code.Span.Contains(GetTupleElement)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var body = decoded.Body;
        using var output = new MemoryStream();
        int copied = 0;
        BeamProjectionFlow? flow = null;
        for (int info = 0; info + 4 < body.Count; info++)
        {
            if (body[info].Definition.Opcode != FunctionInfo)
            {
                continue;
            }

            int entry = info + 1;
            int end = entry;
            while (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                end++;
            }

            (int[] Owners, Dictionary<int, bool[]> Uses)? ownership = null;
            for (int projection = entry + 1; projection + 2 < end; projection++)
            {
                if (body[projection] is not
                    {
                        Definition.Opcode: GetTupleElement,
                        Operands: [_, _, { Kind: BeamOperandParameters.Y, TypeIndex: null } slot]
                    })
                {
                    continue;
                }

                int predicate = projection + 1;
                var check = body[predicate];
                if (!IsEntryFramePredicate(check.Definition.Opcode)
                    || check.Operands[0] is not { Kind: BeamOperandParameters.Label } failure
                    || check.Operands.Any(operand => SameProjectionRegister(operand, slot))
                    || !ReadsFallthroughArgument(
                        body,
                        predicate + 1,
                        end,
                        slot
                    ))
                {
                    continue;
                }

                flow ??= new BeamProjectionFlow(body);
                ownership ??= ReadDisjointFrameOwnership(body, entry, end);
                if (ownership is not { } frame || frame.Owners[projection - entry] is not (>= 0 and var allocate)
                    || !flow.TryGetSingleForwardTarget(predicate, (int)failure.Value, out int alternate)
                    || alternate <= predicate + 1 || alternate >= end
                    || !flow.HasNoUseBeforeClosingFrame(
                        alternate,
                        slot,
                        entry,
                        allocate,
                        frame.Owners
                    ))
                {
                    continue;
                }

                // The alternate edge does not read the projected field. Keep
                // its frame slot valid for GC without running the projection.
                if (!flow.IsInitialized(projection, slot))
                {
                    int initialization = allocate + 1;
                    bool replace = body[initialization].Definition.Opcode == InitYRegisters;
                    var slots = replace
                        ? body[initialization].Operands.Skip(1).Select(operand => (int)operand.Value)
                        : Enumerable.Empty<int>();
                    output.Write(code.Span[copied..body[initialization].Offset]);
                    WriteFrameInitialization(output, slots.Append((int)slot.Value).Distinct().Order().ToArray());
                    copied = body[replace ? initialization + 1 : initialization].Offset;
                    highestOpcode = Math.Max(highestOpcode, InitYRegisters);
                }

                int first = body[projection].Offset;
                int branch = body[predicate].Offset;
                int continuation = body[predicate + 1].Offset;
                output.Write(code.Span[copied..first]);
                output.Write(code.Span[branch..continuation]);
                output.Write(code.Span[first..branch]);
                copied = continuation;
                break;
            }

            info = end - 1;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static bool ReadsFallthroughArgument(
        IReadOnlyList<BeamInstruction> body,
        int first,
        int end,
        BeamOperand slot
    )
    {
        for (int index = first; index < end; index++)
        {
            var instruction = body[index];
            if (instruction.Definition.Opcode == Line)
            {
                continue;
            }

            if (instruction is not { Definition.Opcode: Move, Operands: [var source, { Kind: BeamOperandParameters.X }] })
            {
                return false;
            }

            if (SameProjectionRegister(source, slot))
            {
                return true;
            }
        }

        return false;
    }

    private sealed partial class BeamProjectionFlow
    {
        internal bool HasNoUseBeforeClosingFrame(
            int first,
            BeamOperand slot,
            int entry,
            int allocation,
            IReadOnlyList<int> owners
        )
        {
            var work = new Stack<int>();
            var seen = new HashSet<int>();
            work.Push(first);
            while (work.TryPop(out int index))
            {
                if (!seen.Add(index))
                {
                    continue;
                }

                int offset = index - entry;
                if (offset < 0 || offset >= owners.Count || owners[offset] != allocation
                    || body[index].Operands.Any(operand => SameProjectionRegister(operand, slot)))
                {
                    return false;
                }

                if (body[index].Definition.Opcode is not (Deallocate or TailLocalCallWithFrame or TailExternalCallWithFrame))
                {
                    foreach (int target in successors[index])
                    {
                        work.Push(target);
                    }
                }
            }

            return true;
        }
    }
}
