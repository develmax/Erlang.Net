using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ReallocateGuardedReturningTailFrames(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled
            || !code.Span.Contains(SelectValue) || !code.Span.Contains(TailExternalCallWithFrame)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        var entries = new HashSet<int>();
        using var output = new MemoryStream();
        int copied = 0;
        int start = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int entry = start;
            start = end + 1;
            if (FindGuardedReturningSelector(body, entry, end) is not int call)
            {
                continue;
            }

            for (int tail = call + 6; tail < end; tail++)
            {
                if (PlanGuardedReturningArgumentOrder(body, call, tail) is not { } plan)
                {
                    continue;
                }

                entries.Add((int)body[entry].Operands[0].Value);
                int first = plan.Min();
                output.Write(code.Span[copied..body[first].Offset]);
                foreach (int index in plan)
                {
                    output.Write(code.Span[body[index].Offset..body[index + 1].Offset]);
                }

                copied = body[plan.Max() + 1].Offset;
            }
        }

        if (entries.Count == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);
        byte[] scheduled = output.ToArray();
        if (DecodeLateAtomicProjectionSlots(scheduled, labelCount) is not { } decoded)
        {
            return null;
        }

        string[] atomNames = atoms.OrderBy(entry => entry.Value).Select(entry => entry.Key).ToArray();
        var indexedImports = new (int Module, int Function, int Arity)[imports.Count];
        foreach (var (import, index) in imports)
        {
            int physical = emittedImports?[index] ?? index;
            if (physical >= 0)
            {
                indexedImports[physical] = import;
            }
        }

        return ReallocateLeafFrameRegistersCore(
            scheduled,
            decoded.Body,
            decoded.StackOperands,
            afterCanonicalization: true,
            isArithmetic: instruction => IsLeafArithmeticGcBif(instruction, atomNames, indexedImports),
            canDiscardExternalTailFrame: null,
            ownedBranchesOnly: false,
            isBodyProjection: instruction => IsGuardValueForReturningFrame(instruction, atomNames, indexedImports),
            isColdMapErrorTail: null,
            ownedMapIteratorsOnly: false,
            guardedReturningEntries: entries
        );
    }

    private static bool IsGuardValueForReturningFrame(
        BeamInstruction instruction,
        string[] atomNames,
        (int Module, int Function, int Arity)[] importsByIndex
    )
    {
        if (instruction is not
            {
                Definition.Opcode: BifTwo,
                Operands: [{ Kind: BeamOperandParameters.Failure, Value.IsZero: true }, var index, var left, var right, _]
            })
        {
            return false;
        }

        var imported = importsByIndex[(int)index.Value];
        if (imported.Arity != 2 || atomNames[imported.Module - 1] != ErlangModuleNames.Module)
        {
            return false;
        }

        string function = atomNames[imported.Function - 1];

        return function == ErlangOperators.ExactEqual
            || function == ErlangOperators.And && left.TypeIndex is not null && right.TypeIndex is not null;
    }

    private static int? FindGuardedReturningSelector(IReadOnlyList<BeamInstruction> body, int start, int end)
    {
        if (start + 8 >= end || body[start].Definition.Opcode != Label
            || body[start + 1].Definition.Opcode is not (Allocate or AllocateHeap)
            || body[start + 1].Operands[0].Value.Sign <= 0)
        {
            return null;
        }

        int call = start + 2;
        while (call + 5 < end && body[call].Definition.Opcode is InitYRegisters or Move or PutList or PutTuple or Line)
        {
            call++;
        }

        if (call + 5 >= end || body[call].Definition.Opcode is not (LocalCall or ExternalCall)
            || body[call + 1].Definition.Opcode != IsTuple || body[call + 2].Definition.Opcode != TestArity
            || body[call + 3].Definition.Opcode != GetTupleElement || body[call + 4].Definition.Opcode != GetTupleElement
            || body[call + 5].Definition.Opcode != SelectValue)
        {
            return null;
        }

        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        if (body[call + 1].Operands[1] != x0 || body[call + 2].Operands[1] != x0
            || body[call + 2].Operands[2].Value != 2 || body[call + 3].Operands[0] != x0
            || body[call + 3].Operands[1].Value != 0 || body[call + 4].Operands[0] != x0
            || body[call + 4].Operands[1].Value != 1
            || body[call + 5].Operands[0] != body[call + 3].Operands[2])
        {
            return null;
        }

        // The one call is the only register-clobbering operation before the
        // closed result branches. Existing frame ownership is verified first.
        if (Enumerable.Range(call + 6, end - call - 6).Any(index => body[index].Definition.Opcode is
                LocalCall or ExternalCall or TrimFrame or Wait or WaitTimeout)
            || !HasUnprotectedCallKillSchedule(body, start)
            || ReadDisjointFrameOwnership(body, start, end) is not { } frame
            || frame.Uses.Count != 1 || !frame.Uses.ContainsKey(start + 1))
        {
            return null;
        }

        return call;
    }

    private static int[]? PlanGuardedReturningArgumentOrder(
        IReadOnlyList<BeamInstruction> body,
        int call,
        int tail
    )
    {
        if (body[tail] is not
            {
                Definition.Opcode: TailExternalCallWithFrame,
                Operands: [{ Value.Sign: > 0 } arity, _, _]
            })
        {
            return null;
        }

        int last = tail - 1;
        while (last > call && body[last].Definition.Opcode == Line)
        {
            last--;
        }

        if (arity.Value < 3 || arity.Value >= FirstReservedXRegister)
        {
            return null;
        }

        int first = last + 1 - (int)arity.Value;
        if (first <= call + 5 || body[first - 1].Definition.Opcode is Move or GetTupleElement)
        {
            return null;
        }

        var destinations = new HashSet<int>();
        var reads = new List<(int Index, int Slot)>();
        var projections = new List<int>();
        var literals = new List<int>();
        BeamOperand? tuple = null;
        for (int index = first; index <= last; index++)
        {
            BeamInstruction instruction = body[index];
            if (instruction.Definition.Opcode is not (Move or GetTupleElement)
                || instruction.Operands[^1] is not { Kind: BeamOperandParameters.X, TypeIndex: null } destination
                || destination.Value >= arity.Value || !destinations.Add((int)destination.Value))
            {
                return null;
            }

            BeamOperand source = instruction.Operands[0];
            if (source is { Kind: BeamOperandParameters.Y, TypeIndex: null })
            {
                if (instruction.Definition.Opcode == Move)
                {
                    reads.Add((index, (int)source.Value));
                }
                else if (tuple is null || tuple == source)
                {
                    tuple = source;
                    projections.Add(index);
                }
                else
                {
                    return null;
                }
            }
            else if (instruction.Definition.Opcode == Move && source.Kind is
                BeamOperandParameters.Atom or BeamOperandParameters.Integer or BeamOperandParameters.Nil
                or BeamOperandParameters.LiteralValue)
            {
                literals.Add(index);
            }
            else
            {
                return null;
            }
        }

        if (reads.Count < 2 || projections.Count < 2)
        {
            return null;
        }

        // All outgoing definitions have distinct X destinations and read
        // only saved frame values or fixed terms. Read saved arguments
        // before occupying their preferred low X registers with constants.
        return [.. reads.OrderBy(read => read.Slot).Select(read => read.Index), .. projections, .. literals];
    }
}
