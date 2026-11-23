using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? RemoveDominatedMapProjectionTests(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(IsMap)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        BeamProjectionFlow? flow = null;
        using var output = new MemoryStream();
        int copied = 0;
        for (int index = 0; index + 1 < body.Count; index++)
        {
            if (body[index] is not
                {
                    Definition.Opcode: IsMap,
                    Operands: [
                    { Kind: BeamOperandParameters.Label, Value.Sign: > 0 } failure,
                    { Kind: BeamOperandParameters.X or BeamOperandParameters.Y, TypeIndex: null } register
                    ]
                }
                || body[index + 1] is not
                {
                    Definition.Opcode: GetMapElements,
                    Operands: [var projectionFailure, var projectionSource, _, ..]
                }
                || projectionFailure != failure || !SameProjectionRegister(projectionSource, register))
            {
                continue;
            }

            flow ??= new BeamProjectionFlow(body);
            if (!flow.HasDominatingMapCheck(index, register, allowStack: register.Kind == BeamOperandParameters.Y))
            {
                continue;
            }

            output.Write(code.Span[copied..body[index].Offset]);
            copied = body[index + 1].Offset;
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
        internal bool HasDominatingMapCheck(int index, BeamOperand register, bool allowStack = false)
        {
            var pending = new Stack<(int Index, BeamOperand Register)>();
            var visited = new HashSet<(int Index, BeamOperand Register)>();
            pending.Push((index, register));
            while (pending.TryPop(out var state))
            {
                if (!visited.Add(state))
                {
                    continue;
                }

                List<int> inputs = predecessors[state.Index];
                if (inputs.Count == 0)
                {
                    return false;
                }

                foreach (int previous in inputs)
                {
                    var instruction = body[previous];
                    var operands = instruction.Operands;
                    byte opcode = instruction.Definition.Opcode;
                    if (previous >= state.Index)
                    {
                        return false;
                    }

                    bool success = state.Index == previous + 1
                        && !operands.Any(operand => operand.Kind == BeamOperandParameters.Label
                            && labels.TryGetValue((int)operand.Value, out int landing) && landing == state.Index);
                    if (opcode == IsMap && success && SameProjectionRegister(operands[1], state.Register))
                    {
                        continue;
                    }

                    BeamOperand tracked = state.Register;
                    if (opcode == Move)
                    {
                        if (SameProjectionRegister(operands[1], tracked))
                        {
                            BeamOperand source = operands[0];
                            if (source.Kind != BeamOperandParameters.X
                                && !(allowStack && source.Kind == BeamOperandParameters.Y))
                            {
                                return false;
                            }

                            tracked = new BeamOperand(source.Kind, source.Value);
                        }
                    }
                    else if (opcode is PutMapAssoc or PutMapExact)
                    {
                        if (SameProjectionRegister(operands[2], tracked))
                        {
                            if (!success)
                            {
                                return false;
                            }

                            continue;
                        }

                        if (tracked.Kind == BeamOperandParameters.X && tracked.Value >= operands[3].Value)
                        {
                            return false;
                        }
                    }
                    else if (opcode == TestHeap)
                    {
                        if (tracked.Kind == BeamOperandParameters.X && tracked.Value >= operands[^1].Value)
                        {
                            return false;
                        }
                    }
                    else if (opcode == GetMapElements)
                    {
                        for (int operand = 4; operand < operands.Count; operand += 2)
                        {
                            if (SameProjectionRegister(operands[operand], tracked))
                            {
                                return false;
                            }
                        }
                    }
                    else if (opcode is GetTupleElement or PutTuple or PutList)
                    {
                        if (SameProjectionRegister(operands[StackWriteOperand(instruction)], tracked))
                        {
                            return false;
                        }
                    }
                    else if (opcode is not (Line or Label or Jump) && !IsEntryFramePredicate(opcode))
                    {
                        return false;
                    }

                    pending.Push((previous, tracked));
                }
            }

            return true;
        }
    }
}
