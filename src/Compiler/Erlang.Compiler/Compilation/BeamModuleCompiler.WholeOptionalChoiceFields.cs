using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record TaggedOptionalChoice(Integer Key, DecodeCall Call, bool Whole);

        private OptionalDecode? ReadWholeOptionalChoice(
            Expr.Case decision,
            string current,
            IReadOnlySet<string> bound
        )
        {
            if (decision.Value is not Expr.Variable selector || selector.Name != current
                || decision.Clauses.Count < 3 || decision.Clauses[^1] is not
                {
                    Guard: null, Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                    Body: Expr.Tuple
                    {
                        Items: [Expr.Literal { Value: Atom absent }, Expr.Variable unchanged]
                    }
                }
                || unchanged.Name != current)
            {
                return null;
            }

            var branches = new List<TaggedOptionalChoice>();
            var keys = new HashSet<Integer>();
            foreach (Clause clause in decision.Clauses.SkipLast(1))
            {
                if (clause is not
                    {
                        Guard: null,
                        Patterns: [Pattern.List { Items: [var tag], Tail: Pattern.Variable rest }],
                        Body: Expr.Tuple { Items: [var body, Expr.Variable forwarded] }
                    }
                    || ReadForwardedChoicePattern(tag) is not { } key || !keys.Add(key.Key)
                    || rest.Name == VariableScopeNames.Wildcard || forwarded.Name != rest.Name
                    || rest.Name == key.Binding || bound.Contains(key.Binding) || bound.Contains(rest.Name)
                    || ReadDecodeCall(body, key.Binding, allowRemote: true) is not { } call)
                {
                    return null;
                }

                branches.Add(new(key.Key, call, key.Whole));
            }

            var ordered = branches.OrderByDescending(branch => branch.Key.Value).ToArray();

            return new(
                ordered[0].Key,
                absent,
                ordered[0].Call,
                Choices: ordered
            );
        }

        private void EmitTaggedOptionalChoices(
            OptionalDecode plan,
            int saved,
            int nextTail,
            bool last,
            int missing,
            int joined,
            (TaggedOptionalChoice Choice, int Label)[] choices,
            BeamSourceLocation previous
        )
        {
            owner.Op(IsNonemptyList, LabelTag, missing);
            owner.Operand(YRegisterTag, saved);
            owner.Op(GetList, YRegisterTag, saved);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(YRegisterTag, nextTail);
            owner.EmitTupleCheck(0, 2, missing);
            owner.EmitTupleElement(0, 1);
            EmitAritySelect(1, choices.Length, missing);
            foreach (var item in choices.Reverse())
            {
                owner.LiteralOperand(item.Choice.Key);
                owner.Operand(LabelTag, item.Label);
            }

            foreach (var item in choices)
            {
                Mark(item.Label);
                if (!item.Choice.Whole)
                {
                    owner.EmitTuplePart(0, 1, 0);
                }

                owner.EmitConstant(item.Choice.Call.Tags, 1);
                owner.InitStackSlot(saved);
                EmitDecodeInvocation(item.Choice.Call, previous);
                if (last)
                {
                    owner.MoveRegister(
                        YRegisterTag,
                        nextTail,
                        XRegisterTag,
                        1
                    );
                }
                else
                {
                    owner.MoveRegister(
                        XRegisterTag,
                        0,
                        YRegisterTag,
                        saved
                    );
                }

                JumpTo(joined);
            }

            Mark(missing);
            owner.MoveRegister(
                YRegisterTag,
                saved,
                last ? XRegisterTag : YRegisterTag,
                last ? 1 : nextTail
            );
            owner.EmitConstant(plan.Absent, last ? 0 : saved, last ? XRegisterTag : YRegisterTag);
            Mark(joined);
        }
    }
}
