using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record NestedTaggedEntryArm(
            Atom Tag,
            int Arity,
            Atom ReturnTag,
            int ReturnField,
            NestedTaggedEntryCall? Call
        );

        private sealed record NestedTaggedEntryCall(
            Expr.Call Target,
            Pattern.Tuple State,
            Atom StateTag,
            int StateField,
            int NestedField
        );

        private bool TryEmitNestedTaggedEntry(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || function.Arity != 2
                || function.Clauses.Count < 3 || function.Clauses[0].Patterns[0] is not Pattern.Tuple envelope
                || !HasTupleTag(envelope, out Atom envelopeTag))
            {
                return false;
            }

            int envelopeField = -1;
            for (int index = 1; index < envelope.Items.Count; index++)
            {
                if (envelope.Items[index] is Pattern.Tuple)
                {
                    if (envelopeField >= 0)
                    {
                        return false;
                    }

                    envelopeField = index;
                }
            }

            if (envelopeField < 0)
            {
                return false;
            }

            var arms = new List<NestedTaggedEntryArm>();
            foreach (Clause clause in function.Clauses)
            {
                if (ReadArm(clause) is not { } arm)
                {
                    return false;
                }

                arms.Add(arm);
            }

            var grouped = arms.GroupBy(arm => arm.Arity).OrderByDescending(group => group.Key).ToArray();
            var shared = arms.Where(arm => arm.Call is null)
                .GroupBy(arm => (arm.ReturnTag, arm.ReturnField)).Where(group => group.Count() > 1).ToArray();
            if (grouped.Length < 2 || shared.Length != 1 || shared[0].Key.ReturnField < 1
                || grouped.Any(group => group.Select(arm => arm.Tag).Distinct().Count() != group.Count()))
            {
                return false;
            }

            int failure = owner.nextLabel - 2;
            int nestedRegister = function.Arity;
            int tagRegister = nestedRegister + 1;
            var groups = grouped.Select(group => (Labels: Label(), Arity: group.Key, Arms: group.ToArray())).ToArray();
            int common = Label();
            var termComparer = Comparer<Atom>.Create((left, right) => TermOrder.Compare(left, right, exact: true));
            EmitDetailRecordTest(
                0,
                envelope.Items.Count,
                envelopeTag,
                failure
            );
            owner.EmitTuplePart(0, envelopeField, nestedRegister);
            owner.EmitTupleArityDispatch(failure, groups.Reverse().Select(group => (group.Arity, group.Labels)).ToArray(), nestedRegister);

            for (int index = 0; index < groups.Length; index++)
            {
                var group = groups[index];
                Mark(group.Labels);
                owner.EmitTuplePart(nestedRegister, 0, tagRegister);
                if (group.Arms is [var single])
                {
                    owner.Op(ExactEqualTest, LabelTag, failure);
                    owner.Operand(XRegisterTag, tagRegister);
                    owner.LiteralOperand(single.Tag);
                    if (SharesReturn(single))
                    {
                        if (index + 1 < groups.Length)
                        {
                            owner.Op(Jump, LabelTag, common);
                        }
                    }
                    else
                    {
                        EmitArm(single);
                    }

                    continue;
                }

                var selected = group.Arms.OrderByDescending(arm => arm.Tag, termComparer)
                    .Select(arm => (Arm: arm, Target: SharesReturn(arm) ? common : Label())).ToArray();
                owner.EmitMembershipSelect(tagRegister, failure,
                    selected.Reverse().Select(arm => ((Term)arm.Arm.Tag, arm.Target)));
                foreach (var (candidate, target) in selected)
                {
                    if (target != common)
                    {
                        Mark(target);
                        EmitArm(candidate);
                    }
                }
            }

            Mark(common);
            EmitArm(shared[0].First());

            return true;

            bool SharesReturn(NestedTaggedEntryArm branch) => branch.Call is null
                && (branch.ReturnTag, branch.ReturnField).Equals(shared[0].Key);

            void EmitArm(NestedTaggedEntryArm branch)
            {
                bool framed = branch.Call is not null;
                if (branch.Call is { } call)
                {
                    EmitDetailRecordTest(
                        1,
                        call.State.Items.Count,
                        call.StateTag,
                        failure
                    );
                    EmitFrameAllocation(nestedRegister + 1, 1);
                    owner.MoveRegister(
                        XRegisterTag,
                        1,
                        YRegisterTag,
                        0
                    );
                    owner.EmitTuplePart(1, call.StateField, 0);
                    owner.EmitTuplePart(nestedRegister, call.NestedField, 1);
                    owner.EmitCallLine(call.Target, location);
                    owner.EmitStaticRemoteCall(call.Target);
                }

                int count = branch.ReturnField >= 1 ? 3 : 2;
                owner.Op(TestHeap, BeamWire.UnsignedTag, count + 1);
                owner.Operand(BeamWire.UnsignedTag, framed ? 0 : branch.ReturnField >= 1 ? nestedRegister + 1 : function.Arity);
                if (branch.ReturnField >= 1)
                {
                    owner.EmitTuplePart(nestedRegister, branch.ReturnField, 0);
                }

                TupleHeader(0, count);
                owner.LiteralOperand(branch.ReturnTag);
                if (branch.ReturnField >= 1)
                {
                    owner.Operand(XRegisterTag, 0);
                }

                owner.Operand(framed ? YRegisterTag : XRegisterTag, framed ? 0 : 1);
                if (framed)
                {
                    owner.EmitFrameReturn(1);
                }
                else
                {
                    owner.instructions.WriteByte(Return);
                }
            }

            NestedTaggedEntryArm? ReadArm(Clause clause)
            {
                if (clause.Guard is not null || clause.Patterns is not [Pattern.Tuple outer, var state]
                    || outer.Items.Count != envelope.Items.Count || !HasTupleTag(outer, out Atom tag)
                    || !tag.Equals(envelopeTag)
                    || outer.Items[envelopeField] is not Pattern.Tuple nested || !HasTupleTag(nested, out Atom nestedTag)
                    || outer.Items.Skip(1).Where((_, offset) => offset + 1 != envelopeField)
                        .Any(item => !IsUnconstrained(item))
                    || nested.Items.Skip(1).Any(item => !IsUnconstrained(item)))
                {
                    return null;
                }

                var names = clause.Patterns.SelectMany(Semantics.Variables)
                    .Where(name => name != VariableScopeNames.Wildcard).ToArray();
                if (names.Distinct(StringComparer.Ordinal).Count() != names.Length)
                {
                    return null;
                }

                Expr body = clause.Body;
                NestedTaggedEntryCall? call = null;
                Pattern.Variable stateName;
                if (state is Pattern.Variable original)
                {
                    stateName = original;
                }
                else if (state is AliasPattern alias && TryReadPatternAlias(alias, out var binding, out var shape)
                    && shape is Pattern.Tuple stateTuple && HasTupleTag(stateTuple, out Atom stateTag)
                    && stateTuple.Items.Skip(1).All(IsUnconstrained)
                    && body is Expr.Sequence
                    {
                        Items: [Expr.Call { Module: not null, DynamicRemote: false, Arguments: [Expr.Variable firstArg, Expr.Variable secondArg] } remote,
                            var returned]
                    } && remote.Module != ErlangModuleNames.Module
                    && Field(stateTuple, firstArg) is int stateField && stateField >= 1
                    && Field(nested, secondArg) is int nestedField && nestedField >= 1)
                {
                    stateName = binding;
                    call = new NestedTaggedEntryCall(
                        remote,
                        stateTuple,
                        stateTag,
                        stateField,
                        nestedField
                    );
                    body = returned;
                }
                else
                {
                    return null;
                }

                if (body is not Expr.Tuple result || result.Items.Count is not (2 or 3) || result.Items[0] is not Expr.Literal { Value: Atom returnTag }
                    || result.Items[^1] is not Expr.Variable returnedState || returnedState.Name != stateName.Name)
                {
                    return null;
                }

                if (result.Items.Count == 2)
                {
                    return new NestedTaggedEntryArm(
                        nestedTag,
                        nested.Items.Count,
                        returnTag,
                        -1,
                        call
                    );
                }

                if (call is null && result.Items is [_, Expr.Variable value, _]
                    && Field(nested, value) is int returnField && returnField >= 1)
                {
                    return new NestedTaggedEntryArm(
                        nestedTag,
                        nested.Items.Count,
                        returnTag,
                        returnField,
                        null
                    );
                }

                return null;
            }

            static bool IsUnconstrained(Pattern pattern) => pattern is Erlang.Pattern.Variable or Erlang.Pattern.Any;

            static int Field(Pattern.Tuple pattern, Expr.Variable variable) => pattern.Items.ToList()
                .FindIndex(item => item is Pattern.Variable field && field.Name == variable.Name);
        }
    }
}
