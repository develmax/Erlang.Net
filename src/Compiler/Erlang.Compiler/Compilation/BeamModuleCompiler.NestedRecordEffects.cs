using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record RecordEffectArm(
            Pattern.Tuple Envelope,
            Atom EnvelopeTag,
            int PayloadField,
            int NestedField,
            Pattern.Tuple Nested,
            Atom NestedTag,
            int SelectorField,
            Atom Selector,
            string Payload,
            Pattern State,
            string StateName,
            Atom ResultTag,
            Expr Effect
        );

        private sealed record RecordEffectCase(
            RecordEffectArm Arm,
            int StateArity,
            Atom StateTag,
            RecordEffectCall[] Calls
        );

        private sealed record RecordEffectCall(
            Integer Key,
            Expr.Call Call,
            string? Module,
            int? StateField,
            Term[] Constants
        );

        private sealed record FormattedRecordEffect(
            Expr.Call Outer,
            string? OuterModule,
            Expr.Call Inner,
            string? InnerModule,
            int FirstField,
            int SecondField,
            Term Format,
            Term Options
        );

        private bool TryEmitNestedRecordEffects(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled
                || function.Arity != 2 || function.Clauses.Count < 3
                || function.Clauses.Select(ReadRecordEffectArm).ToArray() is not [var first, .. var tail]
                || first is null || tail.Any(arm => arm is null))
            {
                return false;
            }

            RecordEffectArm[] arms = [first, .. tail.Select(arm => arm!)];
            if (arms.Any(arm => arm.Envelope.Items.Count != first.Envelope.Items.Count
                || !arm.EnvelopeTag.Equals(first.EnvelopeTag) || arm.NestedField != first.NestedField
                || arm.SelectorField != first.SelectorField || !arm.ResultTag.Equals(first.ResultTag)))
            {
                return false;
            }

            var groups = arms.GroupBy(arm => arm.Nested.Items.Count).OrderBy(group => group.Key).ToArray();
            if (groups is not [var selected, var formatted] || selected.Count() < 2
                || formatted.ToArray() is not [var single]
                || ReadFormattedRecordEffect(single) is not { } nestedCall)
            {
                return false;
            }

            var cases = selected.Select(ReadRecordEffectCase).ToArray();
            var selectorArm = selected.First();
            if (cases.Any(plan => plan is null)
                || selected.Any(arm => !arm.NestedTag.Equals(selectorArm.NestedTag) || arm.PayloadField != selectorArm.PayloadField)
                || selected.Select(arm => arm.Selector).Distinct().Count() != cases.Length)
            {
                return false;
            }

            int entryFailure = owner.nextLabel - 2;
            int formatting = Label();
            int selecting = Label();
            int sharedReturn = Label();
            EmitDetailRecordTest(
                0,
                first.Envelope.Items.Count,
                first.EnvelopeTag,
                entryFailure
            );
            owner.EmitTuplePart(0, single.PayloadField, 2);
            owner.EmitTuplePart(0, first.NestedField, 3);
            owner.EmitTupleArityDispatch(entryFailure, [(selected.Key, selecting), (formatted.Key, formatting)], 3);

            Mark(formatting);
            owner.EmitTuplePart(3, 0, 4);
            owner.EmitAtomTest(4, single.NestedTag.Name, entryFailure);
            owner.EmitTuplePart(3, single.SelectorField, 4);
            owner.EmitAtomTest(4, single.Selector.Name, entryFailure);
            EmitFormattedRecordEffect(nestedCall, single.ResultTag);

            Mark(selecting);
            owner.EmitTuplePart(3, 0, 2);
            owner.EmitAtomTest(2, selectorArm.NestedTag.Name, entryFailure);
            owner.EmitTuplePart(3, selectorArm.SelectorField, 2);
            owner.EmitTuplePart(0, selectorArm.PayloadField, 3);

            var ordered = cases.Select(plan => plan!).OrderBy(plan => plan.Arm.Selector,
                Comparer<Atom>.Create((left, right) => TermOrder.Compare(left, right, exact: true))).ToArray();
            int[] selectors = ordered.Select(_ => Label()).Reverse().ToArray();
            owner.EmitMembershipSelect(2, entryFailure,
                ordered.Select((plan, index) => ((Term)plan.Arm.Selector, selectors[index])));
            for (int index = ordered.Length - 1; index >= 0; index--)
            {
                Mark(selectors[index]);
                EmitRecordEffectCase(
                    ordered[index],
                    entryFailure,
                    sharedReturn,
                    index == 0
                );
            }

            Mark(sharedReturn);
            EmitRecordEffectTuple(first.ResultTag);

            return true;
        }

        private static RecordEffectArm? ReadRecordEffectArm(Clause clause)
        {
            if (clause is not
                {
                    Guard: null,
                    Patterns: [Pattern.Tuple envelope, var state],
                    Body: Expr.Sequence
                    {
                        Items: [var effect, Expr.Tuple { Items: [Expr.Literal { Value: Atom resultTag }, Expr.Variable result] }]
                    }
                } || !HasTupleTag(envelope, out Atom envelopeTag)
                || !(state is Pattern.Variable binding && binding.Name == result.Name
                    || state is AliasPattern aliasPattern && TryReadPatternAlias(aliasPattern, out var alias, out _) && alias.Name == result.Name)
                || result.Name == VariableScopeNames.Wildcard
                || clause.Patterns.SelectMany(Semantics.Variables).GroupBy(name => name).Any(group => group.Count() > 1))
            {
                return null;
            }

            var projected = envelope.Items.Select((pattern, index) => (pattern, index)).Skip(1)
                .Where(item => !IsEffectWildcard(item.pattern)).ToArray();
            if (projected.Length != 2
                || projected.FirstOrDefault(item => item.pattern is Pattern.Variable) is not { pattern: Pattern.Variable payload, index: var payloadField }
                || projected.FirstOrDefault(item => item.pattern is Pattern.Tuple) is not { pattern: Pattern.Tuple nested, index: var nestedField }
                || !HasTupleTag(nested, out Atom nestedTag))
            {
                return null;
            }

            var selectors = nested.Items.Select((pattern, index) => (pattern, index)).Skip(1)
                .Where(item => item.pattern is Pattern.Literal).ToArray();
            if (selectors is not [{ pattern: Pattern.Literal { Value: Atom selector }, index: var selectorField }])
            {
                return null;
            }

            return new RecordEffectArm(
                envelope,
                envelopeTag,
                payloadField,
                nestedField,
                nested,
                nestedTag,
                selectorField,
                selector,
                payload.Name,
                state,
                result.Name,
                resultTag,
                effect
            );
        }

        private RecordEffectCase? ReadRecordEffectCase(RecordEffectArm arm)
        {
            if (arm.State is not AliasPattern aliasPattern || !TryReadPatternAlias(aliasPattern, out var bind, out var shape) || bind.Name != arm.StateName
                || shape is not Pattern.Tuple state || !HasTupleTag(state, out Atom stateTag)
                || state.Items.Skip(1).Any(item => !IsEffectWildcard(item))
                || arm.Nested.Items.Where((_, index) => index != 0 && index != arm.SelectorField).Any(item => !IsEffectWildcard(item))
                || arm.Effect is not Expr.Case { Value: Expr.Variable field, Clauses.Count: >= 3 } choice
                || field.Name != arm.Payload || choice.Clauses[^1] is not { Guard: null, Patterns: [var otherwise], Body: var ignored }
                || !IsEffectWildcard(otherwise) || !AttributeLiteral.TryNormalizeConstant(ignored, out _))
            {
                return null;
            }

            var calls = new List<RecordEffectCall>();
            var keys = new HashSet<Integer>();
            foreach (var branch in choice.Clauses.SkipLast(1))
            {
                if (branch is not { Guard: null, Patterns: [Pattern.Literal { Value: Integer key }], Body: Expr.Call call }
                    || key.Value < int.MinValue || key.Value > int.MaxValue
                    || !keys.Add(key) || !TryResolveOrdinaryCallTarget(call, out string? module)
                    || call.Arguments.Count >= FirstReservedXRegister)
                {
                    return null;
                }

                var arguments = call.Arguments;
                int? stateField = null;
                if (arguments is [Expr.RecordAccess { Value: Expr.Variable record } access, ..]
                    && record.Name == arm.StateName && access.Name == stateTag.Name
                    && access.Arity == state.Items.Count && access.Index > 0 && access.Index < access.Arity)
                {
                    stateField = access.Index;
                }

                var constants = new List<Term>();
                foreach (Expr argument in arguments.Skip(stateField is null ? 0 : 1))
                {
                    if (!AttributeLiteral.TryNormalizeConstant(argument, out Term constant))
                    {
                        return null;
                    }

                    constants.Add(constant);
                }

                calls.Add(new RecordEffectCall(
                    key,
                    call,
                    module,
                    stateField,
                    constants.ToArray()
                ));
            }

            return new RecordEffectCase(
                arm,
                state.Items.Count,
                stateTag,
                calls.OrderBy(call => call.Key.Value).ToArray()
            );
        }

        private FormattedRecordEffect? ReadFormattedRecordEffect(RecordEffectArm arm)
        {
            if (arm.State is not Erlang.Pattern.Variable
                || arm.Effect is not Expr.Call
                {
                    Arguments:
                    [
                        Expr.Variable receiver,
                        Expr.Call
                    {
                        Arguments:
                            [
                                var format,
                                Expr.List
                            {
                                Items: [Expr.Tuple { Items: [Expr.Variable first, Expr.Variable second] }],
                                Tail: null
                            }
                            ]
                    } inner,
                        var options
                    ]
                } outer || receiver.Name != arm.Payload || first.Name == second.Name
                || !TryResolveOrdinaryCallTarget(inner, out string? innerModule)
                || !TryResolveOrdinaryCallTarget(outer, out string? outerModule)
                || !AttributeLiteral.TryNormalizeConstant(format, out Term formatTerm)
                || !AttributeLiteral.TryNormalizeConstant(options, out Term optionsTerm))
            {
                return null;
            }

            var fields = arm.Nested.Items.Select((pattern, index) => (pattern, index))
                .Where(item => item.index != 0 && item.index != arm.SelectorField && !IsEffectWildcard(item.pattern)).ToArray();
            if (fields is not [{ pattern: Pattern.Variable left, index: var firstField }, { pattern: Pattern.Variable right, index: var secondField }]
                || !(left.Name == first.Name && right.Name == second.Name || left.Name == second.Name && right.Name == first.Name))
            {
                return null;
            }

            if (left.Name != first.Name)
            {
                (firstField, secondField) = (secondField, firstField);
            }

            return new FormattedRecordEffect(
                outer,
                outerModule,
                inner,
                innerModule,
                firstField,
                secondField,
                formatTerm,
                optionsTerm
            );
        }

        private void EmitRecordEffectCase(
            RecordEffectCase plan,
            int entryFailure,
            int sharedReturn,
            bool last
        )
        {
            EmitDetailRecordTest(
                1,
                plan.StateArity,
                plan.StateTag,
                entryFailure
            );
            EmitFrameAllocation(4, 1);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                0
            );
            int[] targets = plan.Calls.Select(_ => Label()).Reverse().ToArray();
            owner.EmitMembershipSelect(3, sharedReturn, plan.Calls.Select((arm, index) => ((Term)arm.Key, targets[index])));
            for (int index = plan.Calls.Length - 1; index >= 0; index--)
            {
                var arm = plan.Calls[index];
                Mark(targets[index]);
                if (arm.StateField is int field)
                {
                    owner.EmitTuplePart(1, field, 0);
                }

                for (int argument = arm.Constants.Length - 1; argument >= 0; argument--)
                {
                    owner.EmitConstant(arm.Constants[argument], argument + (arm.StateField is null ? 0 : 1));
                }

                owner.EmitCallLine(arm.Call, location);
                Call(arm.Module, arm.Call.Function, arm.Call.Arguments.Count);
                if (!last || index > 0)
                {
                    JumpTo(sharedReturn);
                }
            }
        }

        private void EmitFormattedRecordEffect(FormattedRecordEffect plan, Atom result)
        {
            owner.Op(AllocateHeap, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, 3 + ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 4);
            owner.MoveRegister(
                XRegisterTag,
                2,
                YRegisterTag,
                0
            );
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                1
            );
            owner.EmitTuplePart(3, plan.FirstField, 0);
            owner.EmitTuplePart(3, plan.SecondField, 1);
            owner.EmitTupleHeader(XRegisterTag, 2, 0);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            owner.Op(PutList, XRegisterTag, 0);
            owner.LiteralOperand(Nil.Value);
            owner.Operand(XRegisterTag, 1);
            owner.EmitConstant(plan.Format, 0);
            owner.EmitCallLine(plan.Inner, location);
            Call(plan.InnerModule, plan.Inner.Function, 2);

            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                1
            );
            owner.EmitConstant(plan.Options, 2);
            owner.MoveRegister(
                YRegisterTag,
                0,
                XRegisterTag,
                0
            );
            TrimDetailFrame(1, 1);
            owner.EmitCallLine(plan.Outer, CallLocation(plan.Inner, location));
            Call(plan.OuterModule, plan.Outer.Function, 3);
            EmitRecordEffectTuple(result);
        }

        private void EmitRecordEffectTuple(Atom tag)
        {
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.EmitTupleHeader(XRegisterTag, 2, 0);
            owner.LiteralOperand(tag);
            owner.Operand(YRegisterTag, 0);
            owner.EmitFrameReturn(1);
        }

        private static bool IsEffectWildcard(Pattern pattern) =>
            pattern is Erlang.Pattern.Any or Erlang.Pattern.Variable { Name: VariableScopeNames.Wildcard };
    }
}
