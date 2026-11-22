using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? SpecializeProvenListMatchFailures(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (properListSourceFunctions is null || !SsaTypeInferenceEnabled || ModuleOptimizationDisabled
            || OnLoadFunction is not null || sourceModule?.DeclaredNifs is { Count: > 0 })
        {
            return null;
        }

        // The terminal operand does not need to participate in allocation and slot compaction.
        try
        {
            var header = new BeamCodeHeader(
                BeamWire.FormatVersion,
                highestOpcode,
                labelCount,
                exports.Count + locals.Count,
                code
            );
            var body = BeamInstructionReader.Read(header, atoms.Count + 1, literalBytes.Count);
            string[] atomNames = atoms.OrderBy(entry => entry.Value).Select(entry => entry.Key).ToArray();

            return SpecializeProvenListMatchFailures(code, body, atomNames);
        }
        catch (Exception error) when (error is InvalidDataException or NotSupportedException)
        {
            return null;
        }
    }

    private byte[]? SpecializeProvenListMatchFailures(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        string[] atomNames
    )
    {
        if (properListSourceFunctions is null || !SsaTypeInferenceEnabled || ModuleOptimizationDisabled
            || OnLoadFunction is not null || sourceModule?.DeclaredNifs is { Count: > 0 })
        {
            return null;
        }

        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        (string Name, int Arity) signature = default;
        int entry = -1;
        int functionEnd = 0;
        int copied = 0;
        for (int index = 0; index + 1 < body.Count; index++)
        {
            if (body[index] is { Definition.Opcode: FunctionInfo, Operands: [_, var function, var arity] })
            {
                signature = (atomNames[(int)function.Value - 1], (int)arity.Value);
                entry = body[index + 1].Definition.Opcode == Label && !exportedFunctions.Contains(signature) ? index + 1 : -1;
                functionEnd = index + 2;
                while (functionEnd < body.Count && body[functionEnd].Definition.Opcode is not (FunctionInfo or EndCode))
                {
                    functionEnd++;
                }

                continue;
            }

            if (entry < 0 || body[index] is not
                {
                    Definition.Opcode: IsNonemptyList,
                    Operands: [{ Kind: BeamOperandParameters.Label } failure, var tested]
                } || tested.Kind is not (BeamOperandParameters.X or BeamOperandParameters.Y)
                || tested.TypeIndex is not null)
            {
                continue;
            }

            flow ??= new BeamProjectionFlow(body);
            if (!flow.TryGetSingleForwardTarget(index, (int)failure.Value, out int target) || target >= functionEnd)
            {
                continue;
            }

            int first = SkipLines(target + 1);
            if (first >= functionEnd)
            {
                continue;
            }

            if (body[first] is { Definition.Opcode: IsNil, Operands: [_, var checkedNil] }
                && checkedNil == tested && body[first].Offset >= copied
                && HasProperListOrigin())
            {
                // A proper list with no cons cell at its head can only be nil.
                output.Write(code.Span[copied..body[first].Offset]);
                copied = body[first + 1].Offset;
                continue;
            }

            int terminal = first;
            if (body[first] is { Definition.Opcode: Move, Operands: [var input, var destination] }
                && input == tested && destination is { Kind: BeamOperandParameters.X, Value.IsZero: true })
            {
                terminal = SkipLines(first + 1);
            }

            var rejected = terminal == first ? tested : body[first].Operands[1];
            if (terminal >= functionEnd || body[terminal] is not
                { Definition.Opcode: BadMatch, Operands: [var value] } || value != rejected
                || body[first].Offset < copied
                || !HasProperListOrigin())
            {
                continue;
            }

            output.Write(code.Span[copied..body[first].Offset]);
            if (terminal != first)
            {
                output.Write(code.Span[body[first + 1].Offset..body[terminal].Offset]);
            }

            output.WriteByte(BadMatch);
            BeamCompactWriter.Write(output, AtomTag, NilAtomIndex);
            copied = body[terminal + 1].Offset;

            bool HasProperListOrigin() => flow!.ReadProperListEntryOrigins(
                index,
                entry,
                tested,
                argument => HasProperListEntryFact(signature, argument)
            );
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        int SkipLines(int index)
        {
            while (index < body.Count && body[index].Definition.Opcode == Line)
            {
                index++;
            }

            return index;
        }
    }

    private sealed partial class BeamProjectionFlow
    {
        internal bool ReadProperListEntryOrigins(
            int instruction,
            int entry,
            BeamOperand register,
            Func<int, bool> isProperList
        )
        {
            if (predecessors[entry].Any(previous => previous != entry - 1))
            {
                return false;
            }

            var pending = new Stack<(int Index, BeamOperand Value)>();
            var visited = new HashSet<(int Index, BeamOperand Value)>();
            bool reachedEntry = false;
            pending.Push((instruction, register));
            while (pending.TryPop(out var state))
            {
                if (!visited.Add(state))
                {
                    continue;
                }

                if (state.Index == entry)
                {
                    if (state.Value.Kind != BeamOperandParameters.X || !isProperList((int)state.Value.Value))
                    {
                        return false;
                    }

                    reachedEntry = true;
                    continue;
                }

                if (predecessors[state.Index].Count == 0)
                {
                    return false;
                }

                foreach (int previous in predecessors[state.Index])
                {
                    if (previous < entry || previous >= state.Index)
                    {
                        return false;
                    }

                    BeamInstruction prior = body[previous];
                    BeamOperand source = state.Value;
                    if (prior is { Definition.Opcode: Move, Operands: [var input, var output] }
                        && SameProjectionRegister(output, source))
                    {
                        if (input.Kind is not (BeamOperandParameters.X or BeamOperandParameters.Y)
                            || input.TypeIndex is not null)
                        {
                            return false;
                        }

                        source = input;
                    }
                    else if (prior.Definition.Opcode is LocalCall or ExternalCall or DynamicApply or CallFun or CallFunTyped
                        || !PreservesRecordArgument(prior, source))
                    {
                        return false;
                    }

                    pending.Push((previous, source));
                }
            }

            return reachedEntry;
        }
    }
}
