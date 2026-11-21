using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record SelectedEntryEffectArm(
            Term Key,
            Expr.Call Start,
            Expr[] Effects,
            Expr Result
        );
        private sealed record SelectedEntryRecord(
            string Name,
            int Input,
            string Tag,
            int Arity
        );

        private bool TryEmitSelectedEntryEffectResult(
            Clause clause,
            Dictionary<string, (int Input, int? Field)> bindings,
            List<EntryEffectFailure> failures
        )
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || clause.Body is not Expr.Sequence
                {
                    Items:
                    [
                        Expr.Match
                    {
                        Pattern: Pattern.Variable result,
                        Value: Expr.Case { Value: Expr.Variable selector, Clauses.Count: >= 2 } decision
                    },
                        Expr.Case { Value: Expr.Variable tested } cleanup,
                        Expr.Tuple { Items: [_, .., Expr.RecordUpdate outer] } returned
                    ]
                }
                || result.Name == VariableScopeNames.Wildcard || bindings.ContainsKey(result.Name)
                || !bindings.TryGetValue(selector.Name, out var input) || input.Input == 0 || input.Field is not int selectorField
                || outer.Fields is not [{ Value: Expr.RecordUpdate inner }]
                || inner.Fields is not [{ Value: Expr.Variable chosen }] || chosen.Name != result.Name
                || returned.Items.SkipLast(1).Any(value => !AttributeLiteral.TryNormalizeConstant(value, out _))
                || !ReadSelectedEntryRecord(
                    outer,
                    clause,
                    bindings,
                    out var state
                )
                || !ReadSelectedEntryRecord(
                    inner,
                    clause,
                    bindings,
                    out var data
                ) || state.Name == data.Name
                || !bindings.TryGetValue(tested.Name, out var field) || field.Input != data.Input || field.Field is not int cleanupField
                || cleanup.Clauses is not
                [
                { Guard: null, Patterns: [Pattern.Literal { Value: Atom absent }], Body: Expr.Literal },
                { Guard: null, Patterns: [var otherwise], Body: Expr.Call { Arguments: [Expr.Variable actionInput] } action }
                ]
                || !IsSharedEntryWildcard(otherwise) || actionInput.Name != tested.Name
                || !TryResolveFiniteEffectTarget(action, out string? actionModule))
            {
                return false;
            }

            string? commonInput = null;
            var arms = new List<SelectedEntryEffectArm>();
            var keys = new HashSet<Term>();
            foreach (Clause branch in decision.Clauses)
            {
                if (branch is not
                    {
                        Guard: null,
                        Patterns: [Pattern.Literal { Value: Atom or Integer } key],
                        Body: Expr.Sequence { Items: [Expr.Call start, .., var last] } body
                    }
                    || !keys.Add(key.Value) || !TryResolveFiniteEffectTarget(start, out _)
                    || start.Arguments is not [Expr.Variable inputVariable, ..]
                    || commonInput is not null && commonInput != inputVariable.Name
                    || start.Arguments.Count >= FirstReservedXRegister
                    || start.Arguments.Skip(1).Any(arg => !AttributeLiteral.TryNormalizeConstant(arg, out _)))
                {
                    return false;
                }

                var effects = new List<Expr>();
                foreach (Expr effect in body.Items.Skip(1).SkipLast(1))
                {
                    if (effect is not Expr.Call effectCall || Rewrite(effectCall) is not { } rewritten)
                    {
                        return false;
                    }

                    effects.Add(rewritten);
                }

                if (last is Expr.Call produce && Rewrite(produce) is { } producer)
                {
                    last = producer;
                }
                else if (!AttributeLiteral.TryNormalizeConstant(last, out _))
                {
                    return false;
                }

                commonInput = inputVariable.Name;
                arms.Add(new SelectedEntryEffectArm(
                    key.Value,
                    start,
                    effects.ToArray(),
                    last
                ));
            }

            if (commonInput is null || !bindings.TryGetValue(commonInput, out var common)
                || common.Field is not int commonField)
            {
                return false;
            }

            const int resultSlot = 0;
            const int dataSlot = 1;
            const int stateSlot = 2;
            const int slots = stateSlot + 1;
            const int choiceRegister = 1;
            int reject = Label();
            int join = Label();
            failures.Add(new EntryEffectFailure(reject, choiceRegister, CaseSourceLocation(decision)));
            var savedVariables = variables;
            var savedRecords = checkedRecordBindings;
            int savedSlot = nextSlot;
            variables = new(StringComparer.Ordinal)
            {
                [result.Name] = resultSlot,
                [data.Name] = dataSlot,
                [state.Name] = stateSlot
            };
            checkedRecordBindings = new(savedRecords, StringComparer.Ordinal);
            RememberRecordShape(data.Name, data.Tag, data.Arity);
            RememberRecordShape(state.Name, state.Tag, state.Arity);
            nextSlot = slots;

            int live = new[] { input.Input, common.Input, data.Input, state.Input }.Max() + 1;
            EmitFrameAllocation(live, slots);
            owner.InitStackSlot(resultSlot);
            SaveRoot(data.Input, dataSlot);
            SaveRoot(state.Input, stateSlot);
            owner.EmitTuplePart(common.Input, commonField, 0);
            owner.EmitTuplePart(input.Input, selectorField, choiceRegister);
            arms.Sort((left, right) => TermOrder.Compare(left.Key, right.Key, exact: true));
            int[] targets = arms.Select(_ => Label()).Reverse().ToArray();
            owner.EmitMembershipSelect(choiceRegister, reject, arms.Select((arm, index) => (arm.Key, targets[index])));
            for (int index = arms.Count - 1; index >= 0; index--)
            {
                Mark(targets[index]);
                var arm = arms[index];
                var start = arm.Start;
                int arity = start.Arguments.Count;
                for (int position = arity - 1; position > 0; position--)
                {
                    AttributeLiteral.TryNormalizeConstant(start.Arguments[position], out var literal);
                    owner.EmitConstant(literal, position);
                }

                TryResolveFiniteEffectTarget(start, out string? module);
                owner.EmitCallLine(start, location);
                Call(module, start.Function, arity);
                foreach (Expr effect in arm.Effects)
                {
                    Expression(effect);
                }

                if (AttributeLiteral.TryNormalizeConstant(arm.Result, out var constant))
                {
                    owner.instructions.WriteByte(Move);
                    owner.LiteralOperand(constant);
                    owner.Operand(YRegisterTag, resultSlot);
                }
                else
                {
                    Expression(arm.Result);
                    owner.MoveRegister(
                        XRegisterTag,
                        0,
                        YRegisterTag,
                        resultSlot
                    );
                }

                if (index != 0)
                {
                    JumpTo(join);
                }
            }

            Mark(join);
            owner.EmitTuplePart(
                dataSlot,
                cleanupField,
                0,
                YRegisterTag
            );
            int skip = Label();
            owner.Op(ExactNotEqualTest, LabelTag, skip);
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(absent);
            owner.EmitCallLine(action, location);
            Call(actionModule, action.Function, 1);
            Mark(skip);
            Expression(returned);
            owner.EmitFrameReturn(slots);

            variables = savedVariables;
            checkedRecordBindings = savedRecords;
            nextSlot = savedSlot;

            return true;

            void SaveRoot(int register, int slot) => owner.MoveRegister(
                XRegisterTag,
                register,
                YRegisterTag,
                slot
            );

            Expr.Call? Rewrite(Expr.Call call)
            {
                if (call.Arguments.Count >= FirstReservedXRegister || !TryResolveFiniteEffectTarget(call, out _)
                    && call is not
                    {
                        DynamicRemote: false,
                        Module: ErlangModuleNames.Module,
                        Function: EntryTimerStart,
                        Arguments.Count: 3 or 4
                    })
                {
                    return null;
                }

                var arguments = new List<Expr>();
                int computed = 0;
                foreach (Expr argument in call.Arguments)
                {
                    if (argument is Expr.Variable value && bindings.TryGetValue(value.Name, out var bound))
                    {
                        SelectedEntryRecord? record = bound.Input == data.Input ? data : bound.Input == state.Input ? state : null;
                        if (record is null)
                        {
                            return null;
                        }

                        if (bound.Field is int index)
                        {
                            arguments.Add(new Expr.RecordAccess(
                                new Expr.Variable(record.Name),
                                record.Tag,
                                record.Arity,
                                index
                            ));
                            computed++;
                            continue;
                        }

                        if (value.Name != record.Name)
                        {
                            return null;
                        }
                    }
                    else if (argument is Expr.Call
                    {
                        Function: AutoImportNames.Self,
                        Arguments.Count: 0,
                        DynamicRemote: false
                    } self && owner.ResolvesErlangBif(self))
                    {
                        computed++;
                    }
                    else if (!AttributeLiteral.TryNormalizeConstant(argument, out _))
                    {
                        return null;
                    }

                    arguments.Add(argument);
                }

                return computed <= 1 ? call with { Arguments = arguments } : null;
            }
        }

        private const string EntryTimerStart = "send_after";

        private static bool ReadSelectedEntryRecord(
            Expr.RecordUpdate update,
            Clause clause,
            Dictionary<string, (int Input, int? Field)> bindings,
            out SelectedEntryRecord record
        )
        {
            if (update.Value is Expr.Variable variable && bindings.TryGetValue(variable.Name, out var binding)
                && binding.Field is null && clause.Patterns.Any(HasAlias))
            {
                record = new SelectedEntryRecord(
                    variable.Name,
                    binding.Input,
                    update.Name,
                    update.Arity
                );

                return true;
            }

            record = null!;

            return false;

            bool HasAlias(Pattern pattern)
            {
                if (pattern is AliasPattern alias && TryReadPatternAlias(alias, out var name, out var target))
                {
                    if (name.Name == ((Expr.Variable)update.Value).Name && target is Pattern.Tuple tuple
                        && HasTupleTag(tuple, out Atom tag) && tag.Name == update.Name && tuple.Items.Count == update.Arity)
                    {
                        return true;
                    }

                    return HasAlias(target);
                }

                return pattern is Pattern.Tuple shape && shape.Items.Any(HasAlias);
            }
        }
    }
}
