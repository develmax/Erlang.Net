using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? RemoveProvenLocalRecordChecks(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body, string[] atomNames)
    {
        if (ModuleOptimizationDisabled || OnLoadFunction is not null)
        {
            return null;
        }

        var indirect = exports.Select(function => MapEmittedLabel(function.Entry))
            .Concat(closures.Select(closure => MapEmittedLabel(closure.Entry)))
            .Concat(locals.Where(function => IsDeclaredNative((atomNames[function.Atom - 1], function.Arity)))
                .Select(function => MapEmittedLabel(function.Entry))).ToHashSet();
        var calls = new Dictionary<int, List<int>>();
        for (int index = 0; index < body.Count; index++)
        {
            var instruction = body[index];
            if (instruction.Definition.Opcode is LocalCall or TailLocalCall or TailLocalCallWithFrame)
            {
                int target = (int)instruction.Operands[1].Value;
                if (!calls.TryGetValue(target, out var sites))
                {
                    sites = [];
                    calls.Add(target, sites);
                }

                sites.Add(index);
            }
            else
            {
                foreach (BeamOperand operand in instruction.Operands)
                {
                    if (operand.Kind == BeamOperandParameters.Label)
                    {
                        indirect.Add((int)operand.Value);
                    }
                }
            }
        }

        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        for (int index = 1; index + 2 < body.Count; index++)
        {
            if (body[index - 1].Definition.Opcode != FunctionInfo
                || body[index] is not { Definition.Opcode: Label, Operands: [var label] }
                || indirect.Contains((int)label.Value) || !calls.TryGetValue((int)label.Value, out var sites))
            {
                continue;
            }

            int check = index + 1;
            while (body[check].Definition.Opcode == Line)
            {
                check++;
            }

            if (body[check] is not
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [_, var argument, var size, { Kind: BeamOperandParameters.Atom } tag]
                }
                || argument is not { Kind: BeamOperandParameters.X, TypeIndex: null })
            {
                continue;
            }

            flow ??= new BeamProjectionFlow(body);
            if (sites.Any(site => !flow.IsTaggedTupleAtCall(
                site,
                argument,
                size,
                tag,
                value => taggedTupleLiterals.TryGetValue((int)value.Value, out var literal)
                    && literal.Arity == size.Value && literal.Tag == atomNames[(int)tag.Value - 1]
            )))
            {
                continue;
            }

            output.Write(code.Span[copied..body[check].Offset]);
            copied = body[check + 1].Offset;
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
        internal bool IsTaggedTupleAtCall(
            int site,
            BeamOperand argument,
            BeamOperand size,
            BeamOperand tag,
            Func<BeamOperand, bool> isTaggedLiteral
        )
        {
            var pending = new Stack<(int Next, BeamOperand Register)>();
            var visited = new HashSet<(int Next, BeamOperand Register)>();
            pending.Push((site, argument));
            while (pending.TryPop(out var state))
            {
                if (!visited.Add(state))
                {
                    continue;
                }

                if (predecessors[state.Next].Count == 0)
                {
                    return false;
                }

                foreach (int previous in predecessors[state.Next])
                {
                    if (previous >= state.Next)
                    {
                        return false;
                    }

                    BeamInstruction instruction = body[previous];
                    var register = state.Register;
                    if (instruction is { Definition.Opcode: IsTaggedTuple, Operands: [_, var tested, var arity, var atom] }
                        && successors[previous] is [int failure, int success]
                        && success == state.Next && state.Next == previous + 1 && failure != success
                        && SameProjectionRegister(register, tested)
                        && arity == size && atom == tag)
                    {
                        continue;
                    }

                    if (instruction is { Definition.Opcode: PutTuple, Operands: [var tuple, var count, var first, ..] }
                        && SameProjectionRegister(register, tuple) && count.Value == size.Value && first == tag)
                    {
                        continue;
                    }

                    if (ProvesLiteralRecordOnEdge(
                        previous,
                        state.Next,
                        register,
                        isTaggedLiteral
                    ))
                    {
                        continue;
                    }

                    BeamOperand? origin = instruction switch
                    {
                        { Definition.Opcode: Move, Operands: [var input, var destination] }
                            when SameProjectionRegister(destination, register) => input,
                        { Definition.Opcode: UpdateRecord, Operands: [_, var arityAfterUpdate, var input, var destination, _, ..] }
                            when SameProjectionRegister(destination, register) && arityAfterUpdate == size
                                && !UpdatesRecordTag(instruction.Operands) => input,
                        _ => null
                    };
                    if (origin is { } value)
                    {
                        if (value.Kind == BeamOperandParameters.LiteralValue && isTaggedLiteral(value))
                        {
                            continue;
                        }

                        if (value.Kind is not (BeamOperandParameters.X or BeamOperandParameters.Y))
                        {
                            return false;
                        }

                        register = value with { TypeIndex = null };
                    }
                    else if (instruction is { Definition.Opcode: Swap, Operands: [var left, var right] })
                    {
                        if (SameProjectionRegister(register, left))
                        {
                            register = right with { TypeIndex = null };
                        }
                        else if (SameProjectionRegister(register, right))
                        {
                            register = left with { TypeIndex = null };
                        }
                    }
                    else if (instruction is { Definition.Opcode: TrimFrame, Operands: [var removed, var remaining] })
                    {
                        if (register.Kind == BeamOperandParameters.Y)
                        {
                            if (register.Value >= remaining.Value)
                            {
                                return false;
                            }

                            register = register with { Value = register.Value + removed.Value };
                        }
                    }
                    else if (!PreservesRecordArgument(instruction, register))
                    {
                        return false;
                    }

                    pending.Push((previous, register));
                }
            }

            return true;
        }

        private bool ProvesLiteralRecordOnEdge(
            int previous,
            int next,
            BeamOperand register,
            Func<BeamOperand, bool> isTaggedLiteral
        )
        {
            BeamInstruction instruction = body[previous];
            byte opcode = instruction.Definition.Opcode;
            if (opcode is ExactEqualTest or ExactNotEqualTest
                && instruction.Operands is [_, var left, var right]
                && successors[previous] is [int failure, int success] && failure != success
                && next == (opcode == ExactEqualTest ? success : failure))
            {
                return SameProjectionRegister(left, register) && MatchesLiteral(right)
                    || SameProjectionRegister(right, register) && MatchesLiteral(left);
            }

            if (instruction is not { Definition.Opcode: SelectValue, Operands: [var input, var fallback, _, ..] }
                || !SameProjectionRegister(input, register)
                || body[next] is not { Definition.Opcode: Label, Operands: [var target] }
                || fallback.Value == target.Value)
            {
                return false;
            }

            bool matched = false;
            for (int key = 3; key + 1 < instruction.Operands.Count; key += 2)
            {
                if (instruction.Operands[key + 1].Value != target.Value)
                {
                    continue;
                }

                if (!MatchesLiteral(instruction.Operands[key]))
                {
                    return false;
                }

                matched = true;
            }

            return matched;

            bool MatchesLiteral(BeamOperand value) =>
                value.Kind == BeamOperandParameters.LiteralValue && isTaggedLiteral(value);
        }

        private static bool UpdatesRecordTag(IReadOnlyList<BeamOperand> operands)
        {
            // The list contains one-based field indexes paired with values.
            for (int field = 5; field < operands.Count; field += 2)
            {
                if (operands[field].Value.IsOne)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool PreservesRecordArgument(BeamInstruction instruction, BeamOperand register)
        {
            byte opcode = instruction.Definition.Opcode;
            if (opcode is Label or Line or Jump or SelectValue or SelectTupleArity || IsEntryFramePredicate(opcode))
            {
                return true;
            }

            if (opcode is Allocate or AllocateHeap or TestHeap)
            {
                return register.Kind == BeamOperandParameters.X
                    ? register.Value < instruction.Operands[^1].Value
                    : opcode == TestHeap;
            }

            if (opcode is LocalCall or ExternalCall or DynamicApply or CallFun or CallFunTyped)
            {
                return register.Kind == BeamOperandParameters.Y;
            }

            if (opcode == InitYRegisters)
            {
                return instruction.Operands.All(operand => !SameProjectionRegister(register, operand));
            }

            int write = StackWriteOperand(instruction);

            return opcode is Move or GetTupleElement or GetHead or GetTail or GetList or PutTuple or PutList or MakeFun
                && !SameProjectionRegister(register, instruction.Operands[write])
                && (opcode != GetList || !SameProjectionRegister(register, instruction.Operands[2]));
        }
    }
}
