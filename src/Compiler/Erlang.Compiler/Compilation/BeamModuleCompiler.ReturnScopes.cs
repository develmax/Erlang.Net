using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record ReturnHandler(int Installation, int Slot, ReturnHandler? Parent)
    {
        internal static readonly ReturnHandler Unknown = new(-1, -1, null);
    }

    // A null result identifies a function without handler setup. Otherwise only the
    // returned instruction indices are proved reachable with an empty handler stack.
    private static HashSet<int>? UnprotectedReturnLabels(IReadOnlyList<BeamInstruction> body, int start, int end)
    {
        if (!body.Skip(start).Take(end - start).Any(instruction => instruction.Definition.Opcode is CatchEnter or TryEnter))
        {
            return null;
        }

        var labels = new Dictionary<int, int>();
        var handlers = new HashSet<int>();
        for (int index = start; index < end; index++)
        {
            var instruction = body[index];
            if (instruction.Definition.Opcode == Label)
            {
                labels[(int)instruction.Operands[0].Value] = index;
            }
            else if (instruction.Definition.Opcode is CatchEnter or TryEnter)
            {
                handlers.Add((int)instruction.Operands[1].Value);
            }
        }

        var states = new Dictionary<int, ReturnHandler?>();
        var work = new Queue<int>();
        Enqueue(start, null);
        while (work.TryDequeue(out int index))
        {
            var instruction = body[index];
            int opcode = instruction.Definition.Opcode;
            var scope = states[index];
            if (scope != ReturnHandler.Unknown)
            {
                scope = opcode switch
                {
                    CatchEnter or TryEnter => new(index, (int)instruction.Operands[0].Value, scope),
                    CatchEnd or TryEnd or TryCase => CloseReturnScope(body, instruction, scope),
                    TrimFrame => ShiftReturnScope(scope, (int)instruction.Operands[0].Value),
                    _ => scope
                };
            }

            // Seed the exceptional edge at installation. Any exception handled by
            // that tag arrives with that handler and its enclosing tags still live.
            if (opcode is not (LocalCall or TailLocalCall or TailLocalCallWithFrame))
            {
                foreach (var operand in instruction.Operands)
                {
                    if (operand.Kind == BeamOperandParameters.Label && labels.TryGetValue((int)operand.Value, out int target))
                    {
                        Enqueue(target, scope);
                    }
                }
            }

            if (!EndsBeamBlock(opcode))
            {
                Enqueue(index + 1, scope);
            }
        }

        return labels.Where(label => !handlers.Contains(label.Key)
                && states.TryGetValue(label.Value, out var scope) && scope is null)
            .Select(label => label.Value).ToHashSet();

        void Enqueue(int index, ReturnHandler? scope)
        {
            if (index >= end)
            {
                return;
            }

            if (states.TryGetValue(index, out var previous))
            {
                if (previous == scope || previous == ReturnHandler.Unknown)
                {
                    return;
                }

                scope = ReturnHandler.Unknown;
            }

            states[index] = scope;
            work.Enqueue(index);
        }
    }

    private static ReturnHandler? CloseReturnScope(
        IReadOnlyList<BeamInstruction> body,
        BeamInstruction instruction,
        ReturnHandler? scope
    )
    {
        if (scope is null || scope.Slot != (int)instruction.Operands[0].Value)
        {
            return ReturnHandler.Unknown;
        }

        int expected = instruction.Definition.Opcode == CatchEnd ? CatchEnter : TryEnter;

        return body[scope.Installation].Definition.Opcode == expected ? scope.Parent : ReturnHandler.Unknown;
    }

    private static ReturnHandler? ShiftReturnScope(ReturnHandler? scope, int removed)
    {
        if (scope is null)
        {
            return null;
        }

        var parent = ShiftReturnScope(scope.Parent, removed);

        return scope.Slot < removed || parent == ReturnHandler.Unknown
            ? ReturnHandler.Unknown
            : scope with { Slot = scope.Slot - removed, Parent = parent };
    }
}
