using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? AnnotateSavedConsRecordFields(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(UpdateRecord)
            || !code.Span.Contains(PutList) || !atoms.TryGetValue(CopyFreshRecordUpdateHint, out int copy)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        BeamProjectionFlow? flow = null;
        using var output = new MemoryStream();
        int copied = 0;
        int entry = -1;
        bool? unprotected = null;
        for (int index = 0; index + 2 < body.Count; index++)
        {
            BeamInstruction instruction = body[index];
            if (instruction.Definition.Opcode == FunctionInfo)
            {
                entry = index + 1;
                unprotected = null;
            }

            if (entry < 0 || instruction is not
                { Definition.Opcode: UpdateRecord, Operands: [{ Kind: BeamOperandParameters.Atom } hint, ..] }
                || hint.Value != copy || body[index + 1].Definition.Opcode != Deallocate
                || body[index + 2].Definition.Opcode != Return
                || !(unprotected ??= HasUnprotectedCallKillSchedule(body, entry)))
            {
                continue;
            }

            for (int field = FirstRecordUpdateValueOperand; field < instruction.Operands.Count;
                field += RecordUpdateFieldPairWidth)
            {
                if (instruction.Operands[field] is
                    { Kind: BeamOperandParameters.Y, TypeIndex: null } saved
                    && (flow ??= new BeamProjectionFlow(body)).ProvesSavedCons(
                        entry,
                        index,
                        saved,
                        IsConsLiteral
                    ))
                {
                    copied = AnnotateRegisterInput(
                        code,
                        output,
                        copied,
                        instruction,
                        field,
                        ConsTypeBits
                    );
                }
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        bool IsConsLiteral(BeamOperand operand) =>
            operand is { Kind: BeamOperandParameters.LiteralValue }
            && ExternalTermFormat.Decode(literalBytes[(int)operand.Value]) is Cons;
    }

    private sealed partial class BeamProjectionFlow
    {
        internal bool ProvesSavedCons(
            int entry,
            int use,
            BeamOperand value,
            Func<BeamOperand, bool> isConsLiteral
        )
        {
            var pending = new Stack<(int Next, BeamOperand Value)>();
            var seen = new HashSet<(int Next, BeamOperand Value)>();
            pending.Push((use, value));
            while (pending.TryPop(out var current))
            {
                if (!seen.Add(current))
                {
                    continue;
                }

                var (next, register) = current;
                if (predecessors[next].Count == 0)
                {
                    return false;
                }

                foreach (int previous in predecessors[next])
                {
                    if (previous < entry || previous >= next)
                    {
                        return false;
                    }

                    BeamInstruction step = body[previous];
                    var operands = step.Operands;
                    if (step.Definition.Opcode == PutList && SameProjectionRegister(operands[2], register))
                    {
                        continue;
                    }

                    BeamOperand input = register;
                    if (step.Definition.Opcode == Move && SameProjectionRegister(operands[1], register))
                    {
                        if (isConsLiteral(operands[0]))
                        {
                            continue;
                        }

                        input = operands[0] with { TypeIndex = null };
                        if (!IsBareRecordValueRegister(input))
                        {
                            return false;
                        }
                    }
                    else if (step.Definition.Opcode == TrimFrame && register.Kind == BeamOperandParameters.Y)
                    {
                        if (register.Value >= operands[1].Value)
                        {
                            return false;
                        }

                        // A retained physical Y index is rebased by the removed prefix.
                        input = register with { Value = register.Value + operands[0].Value };
                    }
                    else if (!PreservesConsRegister(step, register))
                    {
                        return false;
                    }

                    pending.Push((previous, input));
                }
            }

            return true;
        }

        private static bool PreservesConsRegister(BeamInstruction instruction, BeamOperand value)
        {
            var operands = instruction.Operands;
            bool saved = value.Kind == BeamOperandParameters.Y;

            return instruction.Definition.Opcode switch
            {
                Label or Line or Jump or SelectValue or SelectTupleArity or TrimFrame => true,
                IsTuple or TestArity or IsTaggedTuple or IsNonemptyList or IsNil or ExactEqualTest or ExactNotEqualTest => true,
                Move => true,
                PutTuple => !SameProjectionRegister(operands[0], value),
                PutList or GetTupleElement => !SameProjectionRegister(operands[2], value),
                GetHead or GetTail => !SameProjectionRegister(operands[1], value),
                GetList => !SameProjectionRegister(operands[1], value) && !SameProjectionRegister(operands[2], value),
                LocalCall or ExternalCall => saved,
                TestHeap => saved || value.Value < operands[1].Value,
                Allocate or AllocateHeap => !saved && value.Value < operands[^1].Value,
                InitYRegisters => !operands.Skip(1).Any(slot => SameProjectionRegister(slot, value)),
                GcBifOne or GcBifTwo => operands[0].Value.IsZero && (saved || value.Value < operands[1].Value)
                    && !SameProjectionRegister(operands[^1], value),
                _ => false
            };
        }
    }
}
