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

        var opaque = exports.Select(function => MapEmittedLabel(function.Entry))
            .Concat(locals.Where(function => IsDeclaredNative((atomNames[function.Atom - 1], function.Arity)))
                .Select(function => MapEmittedLabel(function.Entry))).ToHashSet();
        var closureEntries = closures.Select(closure => MapEmittedLabel(closure.Entry)).ToHashSet();
        var closureCreations = new Dictionary<int, List<int>>();
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
            else if (instruction is { Definition.Opcode: MakeFun, Operands: [var function, _, var count, ..] }
                && function.Value.Sign >= 0 && function.Value < closures.Count)
            {
                LiftedClosure closure = closures[(int)function.Value];
                int target = MapEmittedLabel(closure.Entry);
                if (closure.General is null || closure.SelfName is not null || closure.CaptureCount == 0
                    || closure.CaptureCount != count.Value || instruction.Operands.Count != 3 + closure.CaptureCount)
                {
                    opaque.Add(target);
                    continue;
                }

                if (!closureCreations.TryGetValue(target, out var sites))
                {
                    sites = [];
                    closureCreations.Add(target, sites);
                }

                sites.Add(index);
            }
            else
            {
                foreach (BeamOperand operand in instruction.Operands)
                {
                    if (operand.Kind == BeamOperandParameters.Label)
                    {
                        opaque.Add((int)operand.Value);
                    }
                }
            }
        }

        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        var removedChecks = new HashSet<int>();
        int copied = 0;
        bool privateFunctionWithCallers = false;
        for (int check = 1; check + 1 < body.Count; check++)
        {
            if (body[check].Definition.Opcode == FunctionInfo)
            {
                privateFunctionWithCallers = body[check + 1] is { Definition.Opcode: Label, Operands: [var entryLabel] }
                    && !opaque.Contains((int)entryLabel.Value)
                    && !closureEntries.Contains((int)entryLabel.Value)
                    && calls.ContainsKey((int)entryLabel.Value);
            }

            if (!privateFunctionWithCallers)
            {
                continue;
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
            if (!ProvesCheckedArgument())
            {
                continue;
            }

            removedChecks.Add(check);
            output.Write(code.Span[copied..body[check].Offset]);
            copied = body[check + 1].Offset;

            bool ProvesCheckedArgument()
            {
                // Follow the argument through private entry points and require an independent proof on every call path.
                var pending = new Stack<(int Entry, BeamOperand Register, bool Complete)>();
                var active = new HashSet<(int Entry, BeamOperand Register)>();
                var proven = new HashSet<(int Entry, BeamOperand Register)>();
                if (!ProveAt(check, argument))
                {
                    return false;
                }

                while (pending.TryPop(out var fact))
                {
                    var (entry, register, complete) = fact;
                    var key = (entry, register);
                    if (complete)
                    {
                        active.Remove(key);
                        proven.Add(key);
                        continue;
                    }

                    if (proven.Contains(key))
                    {
                        continue;
                    }

                    if (!active.Add(key)
                        || body[entry - 1] is not { Definition.Opcode: FunctionInfo, Operands: [_, _, var arity] }
                        || body[entry] is not { Definition.Opcode: Label, Operands: [var entryLabel] }
                        || register is not { Kind: BeamOperandParameters.X, TypeIndex: null }
                        || register.Value.Sign < 0 || register.Value >= arity.Value
                        || opaque.Contains((int)entryLabel.Value))
                    {
                        return false;
                    }

                    pending.Push((entry, register, true));
                    int label = (int)entryLabel.Value;
                    if (closureEntries.Contains(label))
                    {
                        // A closure caller can choose its explicit arguments but cannot replace its captured environment.
                        if (calls.ContainsKey(label) || !closureCreations.TryGetValue(label, out var creations)
                            || creations.Any(site => !ProveCapture(site, (int)register.Value)))
                        {
                            return false;
                        }
                    }
                    else if (!calls.TryGetValue(label, out var sites) || sites.Any(site => !ProveAt(site, register)))
                    {
                        return false;
                    }
                }

                return true;

                bool ProveCapture(int site, int register)
                {
                    var operands = body[site].Operands;
                    LiftedClosure closure = closures[(int)operands[0].Value];
                    int capture = register - (closure.Arity - closure.CaptureCount);
                    if (capture < 0 || capture >= closure.CaptureCount)
                    {
                        return false;
                    }

                    var value = operands[3 + capture];
                    if (value.Kind == BeamOperandParameters.LiteralValue)
                    {
                        return IsTaggedLiteral(value);
                    }

                    return value.Kind is BeamOperandParameters.X or BeamOperandParameters.Y
                        && ProveAt(site, value with { TypeIndex = null });
                }

                bool IsTaggedLiteral(BeamOperand value) => taggedTupleLiterals.TryGetValue((int)value.Value, out var literal)
                    && literal.Arity == size.Value && literal.Tag == atomNames[(int)tag.Value - 1];

                bool ProveAt(int instruction, BeamOperand register) => flow.IsTaggedTupleBefore(
                    instruction,
                    register,
                    size,
                    tag,
                    IsTaggedLiteral,
                    (caller, input) => pending.Push((caller, input, false)),
                    tested => tested != check && !removedChecks.Contains(tested)
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

    private sealed partial class BeamProjectionFlow
    {
        internal bool IsTaggedTupleBefore(
            int instructionIndex,
            BeamOperand argument,
            BeamOperand size,
            BeamOperand tag,
            Func<BeamOperand, bool> isTaggedLiteral,
            Action<int, BeamOperand> requireEntryArgument,
            Func<int, bool> isAvailableCheck
        )
        {
            var pending = new Stack<(int Next, BeamOperand Register)>();
            var visited = new HashSet<(int Next, BeamOperand Register)>();
            pending.Push((instructionIndex, argument));
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
                    if (instruction.Definition.Opcode == FunctionInfo && state.Next == previous + 1)
                    {
                        requireEntryArgument(state.Next, register);
                        continue;
                    }

                    if (instruction is { Definition.Opcode: IsTaggedTuple, Operands: [_, var tested, var arity, var atom] }
                        && isAvailableCheck(previous)
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
