using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        internal static bool MayPlanLiteralJoin(FunctionDefinition function) =>
            function.Clauses is [{ Guard: null, Body: Expr.Sequence { Items: [Expr.Match { Value: Expr.Case }, _] } }];

        internal bool TryEmitPlannedLiteralJoin(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || recursiveClosure is not null
                || function.Clauses is not
                [
                {
                    Guard: null, Body: Expr.Sequence
                    {
                        Items: [Expr.Match
                        {
                            Pattern: Pattern.Variable { Name: not VariableScopeNames.Wildcard } binding,
                            Value: Expr.Case { Value: Expr.Variable selector } decision
                        }, var body]
                    }
                } clause
                ]
                || !HasUnconditionalParameters(clause) || function.Arity + 1 >= FirstReservedXRegister)
            {
                return false;
            }

            string[] parameters = clause.Patterns.Cast<Pattern.Variable>().Select(pattern => pattern.Name).ToArray();
            int selection = Array.IndexOf(parameters, selector.Name);
            if (selection < 0 || parameters.Contains(binding.Name, StringComparer.Ordinal))
            {
                return false;
            }

            var inputs = new Dictionary<Atom, Term>();
            foreach (Clause arm in decision.Clauses)
            {
                if (arm is not { Guard: null, Patterns: [Pattern.Literal { Value: Atom key }] }
                    || !AttributeLiteral.TryNormalizeConstant(arm.Body, out Term constant)
                    || !inputs.TryAdd(key, constant))
                {
                    return false;
                }
            }

            string[] withJoin = [.. parameters, binding.Name];
            if (inputs.Count < 2 || LowerTerminalCaseArm(
                null,
                body,
                withJoin,
                lowerAppend: true
            ) is not
            { Calls: [{ Constructions.Length: 0 } call] } continuation
                || AllocateTerminalCaseRegisters(continuation, withJoin) is not { } plan
                || plan.EntrySpills.Contains(parameters.Length))
            {
                return false;
            }

            int joinedValue = parameters.Length;
            int[] destinations = Enumerable.Range(0, call.Arguments.Length)
                .Where(index => call.Arguments[index].Value == joinedValue).ToArray();
            int[] originalUses = ReadValues(call.Arguments).Where(value => value != joinedValue).ToArray();
            if (destinations is not [int merged] || originalUses.Contains(merged))
            {
                return false;
            }

            int joinBlock = inputs.Count + 1;
            int rejectedBlock = joinBlock + 1;
            int[] predecessors = Enumerable.Range(1, inputs.Count).ToArray();
            var blocks = new List<BeamFrameBlock>
            {
                new(
                    0,
                    [rejectedBlock, .. predecessors],
                    [],
                    RequiresFrame: false
                )
            };
            foreach (int predecessor in predecessors)
            {
                blocks.Add(new(
                    predecessor,
                    [joinBlock],
                    [],
                    RequiresFrame: false
                ));
            }

            blocks.Add(new(
                joinBlock,
                [],
                predecessors,
                continuation.RequiresFrame
            ));
            blocks.Add(new(
                rejectedBlock,
                [],
                [],
                RequiresFrame: false
            ));
            if (BeamFramePlacement.Place(0, blocks, rejectedBlock) is not { } frames
                || frames.Owners[joinBlock] != frames.Owners[0])
            {
                return false;
            }

            var target = plan.Calls[0];
            plan = plan with
            {
                FrameLive = originalUses.Concat(plan.EntrySpills).Append(selection).Max() + 1,
                Calls =
                [
                    target with
                    {
                        Entry = target.Entry with
                        {
                            Moves = target.Entry.Moves.Where(move => move.Source != joinedValue).ToList()
                        }
                    }
                ]
            };

            bool frame = frames.Owners[0] is not null;
            if (frame)
            {
                EmitTerminalFrame(plan);
            }

            var ordered = inputs.OrderBy(input => input.Key, Comparer<Atom>.Create(CompareAtoms)).ToArray();
            bool closedPair = ordered.Length == 2
                && (ReadFiniteAtomArguments(function, selection, admitGuardFacts: true)
                    ?? owner.ReadStructuralEntryAtoms((function.Name, function.Arity), selection)) is { } known
                && known.SetEquals(inputs.Keys);
            int failure = 0;
            int joined;
            if (closedPair)
            {
                int alternative = Label();
                joined = Label();
                owner.Op(ExactEqualTest, LabelTag, alternative);
                owner.Operand(XRegisterTag, selection);
                owner.LiteralOperand(ordered[1].Key);
                owner.EmitConstant(ordered[1].Value, merged);
                owner.Op(Jump, LabelTag, joined);
                Mark(alternative);
                owner.EmitConstant(ordered[0].Value, merged);
            }
            else
            {
                int[] labels = ordered.Select(_ => Label()).Reverse().ToArray();
                joined = Label();
                failure = Label();
                owner.EmitMembershipSelect(selection, failure, ordered.Select((value, index) => ((Term)value.Key, labels[index])));
                for (int index = ordered.Length - 1; index >= 0; index--)
                {
                    Mark(labels[index]);
                    owner.EmitConstant(ordered[index].Value, merged);
                    if (index != 0)
                    {
                        owner.Op(Jump, LabelTag, joined);
                    }
                }
            }

            Mark(joined);
            EmitTerminalCaseArm(plan, frame, frameAlreadyOpen: frame);
            if (!closedPair)
            {
                Mark(failure);
                BeamSourceLocation previous = location;
                owner.EmitArithmeticLine(decision.SourceLine, ref previous);
                owner.Op(CaseEnd, XRegisterTag, selection);
            }

            return true;
        }
    }
}
