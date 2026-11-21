using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryLoadOrderedAppendPrerequisites(Expr.Binary append)
        {
            if (owner.receiveScratchRegisters is not null || append.Operator != ErlangOperators.Append
                || append.Left is not Expr.Call left || !IsOrdinaryPrerequisite(left)
                || append.Right is not Expr.List { Tail: null, Items: [Expr.Tuple] })
            {
                return false;
            }

            var prerequisites = new List<Expr>();
            // Require a nested constructor the eager path would build before another prerequisite.
            bool completedNestedConstruction = false;
            bool defersNestedConstruction = false;
            Expr? right = ReadConstruction(append.Right, 0);
            if (right is null || !defersNestedConstruction)
            {
                return false;
            }

            const string leftBinding = "";
            var saved = new Dictionary<string, int>(variables, StringComparer.Ordinal)
            {
                [leftBinding] = DecoderSlot(nextSlot)
            };
            for (int index = 0; index < prerequisites.Count - 1; index++)
            {
                saved.Add(PrerequisiteBinding(index), DecoderSlot(nextSlot + index + 1));
            }

            var finalInput = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                [PrerequisiteBinding(prerequisites.Count - 1)] = 0
            };
            if (ReadCallArgumentConstructions(
                [new Expr.Variable(leftBinding), right],
                -1,
                saved,
                inputs: finalInput
            ) is not { } plan)
            {
                return false;
            }

            // Reserve the saved results before evaluating operands that can require scratch slots.
            int leftSlot = Slot();
            var savedSlots = new int[prerequisites.Count - 1];
            for (int index = 0; index < savedSlots.Length; index++)
            {
                savedSlots[index] = Slot();
            }

            int scratchBase = nextSlot;
            Evaluate(left, leftSlot);
            for (int index = 0; index < savedSlots.Length; index++)
            {
                Evaluate(prerequisites[index], savedSlots[index]);
            }

            Expression(prerequisites[^1]);
            EmitCallArgumentConstructions(plan, -1);

            return true;

            void Evaluate(Expr value, int slot)
            {
                Expression(value);
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    YRegisterTag,
                    slot
                );
                ReleaseOperandSlots(scratchBase);
            }

            Expr? ReadConstruction(Expr expression, int depth)
            {
                if (AttributeLiteral.TryNormalizeConstant(expression, out _))
                {
                    return expression;
                }

                if (expression is Expr.Variable value)
                {
                    return IsSavedOperand(value) ? value : null;
                }

                if (expression is Expr.Call call && IsOrdinaryPrerequisite(call, allowLeadingProducer: true))
                {
                    defersNestedConstruction |= completedNestedConstruction;
                    var binding = new Expr.Variable(PrerequisiteBinding(prerequisites.Count));
                    prerequisites.Add(expression);

                    return binding;
                }

                IReadOnlyList<Expr>? items = expression switch
                {
                    Expr.Tuple tuple => tuple.Items,
                    Expr.List { Tail: null } list => list.Items,
                    _ => null
                };
                if (items is null)
                {
                    return null;
                }

                int previousPrerequisites = prerequisites.Count;
                var rewritten = new Expr[items.Count];
                for (int index = 0; index < items.Count; index++)
                {
                    if (ReadConstruction(items[index], depth + 1) is not { } item)
                    {
                        return null;
                    }

                    rewritten[index] = item;
                }

                completedNestedConstruction |= depth >= 2 && prerequisites.Count > previousPrerequisites;

                return expression is Expr.Tuple ? new Expr.Tuple(rewritten) : new Expr.List(rewritten);
            }
        }

        private bool IsOrdinaryPrerequisite(Expr.Call call, bool allowLeadingProducer = false)
        {
            if (ReferenceEquals(call, preparedReceiveCall) || owner.ResolvesErlangBif(call)
                || !TryResolveOrdinaryCallTarget(call, out _))
            {
                return false;
            }

            // Lifted comprehensions pass their generator result as the first helper argument.
            if (allowLeadingProducer && call.Arguments is [Expr.Call producer, ..])
            {
                return IsOrdinaryPrerequisite(producer) && call.Arguments.Skip(1).All(IsPreparedPrerequisiteOperand);
            }

            return call.Arguments.All(IsPreparedPrerequisiteOperand);
        }

        private bool IsPreparedPrerequisiteOperand(Expr argument) =>
            argument is Expr.Variable variable && IsSavedOperand(variable)
            || AttributeLiteral.TryNormalizeConstant(argument, out _);

        private bool IsSavedOperand(Expr.Variable variable) =>
            variables.TryGetValue(variable.Name, out int slot) && slot >= 0
            && !deferredPairMembers.ContainsKey(variable.Name);

        // Source variables cannot contain a colon.
        private static string PrerequisiteBinding(int ordinal) =>
            ":" + ordinal.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
