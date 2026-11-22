using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitPlannedSingletonListEntry(Clause single, Clause rest, int functionInfo)
        {
            int arity = single.Patterns.Count;
            if (arity + 1 >= FirstReservedXRegister)
            {
                return false;
            }

            var singletonBindings = new string[arity + 1];
            var remainingBindings = new string[arity + 1];
            int input = -1;
            for (int index = 0; index < arity; index++)
            {
                if (single.Patterns[index] is Pattern.Variable first && rest.Patterns[index] is Pattern.Variable second)
                {
                    singletonBindings[index] = first.Name;
                    remainingBindings[index] = second.Name;
                }
                else if (input < 0
                    && single.Patterns[index] is Pattern.List { Items: [Pattern.Variable head], Tail: null }
                    && rest.Patterns[index] is Pattern.List
                    {
                        Items: [Pattern.Variable other],
                        Tail: Pattern.Variable tail
                    })
                {
                    input = index;
                    singletonBindings[index] = VariableScopeNames.Wildcard;
                    remainingBindings[index] = tail.Name;
                    singletonBindings[arity] = head.Name;
                    remainingBindings[arity] = other.Name;
                }
                else
                {
                    return false;
                }
            }

            if (input < 0 || !HasDistinctTerminalBindings(singletonBindings) || !HasDistinctTerminalBindings(remainingBindings)
                || LowerTerminalCaseArm(null, single.Body, singletonBindings) is not { RequiresFrame: false } singleton
                || LowerTerminalCaseArm(null, rest.Body, remainingBindings) is not { RequiresFrame: true } remaining
                || AllocateTerminalCaseRegisters(singleton, singletonBindings) is not { } plannedSingle
                || AllocateTerminalCaseRegisters(remaining, remainingBindings) is not { } plannedRemaining)
            {
                return false;
            }

            const int dispatch = 0;
            const int emptyTail = 1;
            const int remainingTail = 2;
            const int miss = 3;
            var frames = BeamFramePlacement.Place(
                dispatch,
                [
                    new(
                        dispatch,
                        [miss, remainingTail, emptyTail],
                        [],
                        RequiresFrame: false
                    ),
                    new(
                        emptyTail,
                        [],
                        [],
                        singleton.RequiresFrame
                    ),
                    new(
                        remainingTail,
                        [],
                        [],
                        remaining.RequiresFrame
                    ),
                    new(
                        miss,
                        [],
                        [],
                        RequiresFrame: false
                    )
                ]
            );
            if (frames is null || frames.Owners[dispatch] is not null)
            {
                return false;
            }

            // After the outer cons check both source clauses consume the same cell.
            // The second clause accepts every remaining tail, including an improper one.
            int nonEmptyTail = Label();
            owner.EmitNonemptyList(input, functionInfo);
            owner.EmitListParts(
                input,
                XRegisterTag,
                arity,
                XRegisterTag,
                input
            );
            owner.Op(IsNil, LabelTag, nonEmptyTail);
            owner.Operand(XRegisterTag, input);
            EmitTerminalCaseArm(plannedSingle, frames.Owners[emptyTail] is not null);
            Mark(nonEmptyTail);
            EmitTerminalCaseArm(plannedRemaining, frames.Owners[remainingTail] is not null);

            return true;
        }
    }
}
