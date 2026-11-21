using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? EliminateGuardedEntryInitializations(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(InitYRegisters)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var (body, _) = decoded;
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
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
            int allocation = FindEntryFrameAllocation(
                body,
                start,
                end,
                allowUnframedProjections: true
            );
            int initial = allocation + 1;
            if (allocation < 0 || initial + 1 >= end
                || body[initial].Definition.Opcode != InitYRegisters
                || ReadDisjointFrameOwnership(body, start, end) is not { } frame || frame.Uses.Count != 1
                || frame.Owners[initial - start] != allocation)
            {
                continue;
            }

            flow ??= new BeamProjectionFlow(body);
            int[] retained = body[initial].Operands.Skip(1).Where(slot => !flow.HasGuardedDefinitionBeforeScan(
                slot,
                start,
                end,
                initial + 1,
                allocation,
                frame.Owners
            )).Select(slot => (int)slot.Value).ToArray();
            if (retained.Length == body[initial].Operands.Count - 1)
            {
                continue;
            }

            output.Write(code.Span[copied..body[initial].Offset]);
            WriteFrameInitialization(output, retained);
            copied = body[initial + 1].Offset;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private sealed partial class BeamProjectionFlow
    {
        internal bool HasGuardedDefinitionBeforeScan(
            BeamOperand slot,
            int start,
            int end,
            int initial,
            int allocation,
            IReadOnlyList<int> owners
        )
        {
            // Every still uninitialized path must define this slot or release
            // its frame before a read or an instruction that can scan roots.
            var pending = new Stack<(int Index, bool Guarded)>();
            var seen = new HashSet<(int Index, bool Guarded)>();
            bool guardedDefinition = false;
            pending.Push((initial, false));
            while (pending.TryPop(out var state))
            {
                if (!seen.Add(state))
                {
                    continue;
                }

                var (index, guarded) = state;
                if (index < initial || index >= end || owners[index - start] != allocation)
                {
                    return false;
                }

                BeamInstruction instruction = body[index];
                byte opcode = instruction.Definition.Opcode;
                if (opcode == Deallocate)
                {
                    guardedDefinition |= guarded;
                    continue;
                }

                int target = -1;
                if (opcode == Jump || IsEntryInitializationPredicate(opcode))
                {
                    if (instruction.Operands[0] is not { Kind: BeamOperandParameters.Label } failure
                        || !labels.TryGetValue((int)failure.Value, out target) || target <= index)
                    {
                        return false;
                    }

                    guarded = true;
                }
                else if (opcode is not (Label or Line or InitYRegisters or Move or GetTupleElement or GetList or GetHead or GetTail))
                {
                    return false;
                }

                int destination = StackAllocationDestination(instruction);
                bool defines = false;
                for (int position = 0; position < instruction.Operands.Count; position++)
                {
                    if (!SameProjectionRegister(instruction.Operands[position], slot))
                    {
                        continue;
                    }

                    if (opcode != InitYRegisters && position != destination && !(opcode == GetList && position == 2))
                    {
                        return false;
                    }

                    defines = true;
                }

                if (defines)
                {
                    guardedDefinition |= guarded;
                    continue;
                }

                if (target >= 0)
                {
                    pending.Push((target, true));
                }

                if (opcode != Jump)
                {
                    pending.Push((index + 1, guarded));
                }
            }

            return guardedDefinition;
        }

        private static bool IsEntryInitializationPredicate(byte opcode) =>
            opcode is IsTuple or IsTaggedTuple or TestArity or IsNonemptyList or IsNil or IsAtom or IsInteger
                or IsNumber or IsFloat or IsBoolean or IsList or IsMap or IsBinary or IsBitString or IsPid
                or IsReference or IsPort or IsFunction or IsFunctionWithArity or ExactEqualTest or ExactNotEqualTest
                or NumericEqualTest or NumericNotEqualTest or IsLess or IsGreaterOrEqual;
    }
}
