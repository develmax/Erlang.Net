using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitRequiredSequenceWithForeignPair(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || recursiveClosure is not null
                || function.Clauses is not
                [
                    {
                        Guard: null, Patterns: [Pattern.Variable input, Pattern.Variable tags],
                        Body: Expr.Sequence
                        {
                            Items:
                                [
                                    Expr.Match
                                {
                                    Pattern: Pattern.Tuple
                                    {
                                        Items: [Pattern.Variable { Name: VariableScopeNames.Wildcard }, ..]
                                    } record,
                                    Value: Expr.Variable original
                                } tuple,
                                    ..,
                                    Expr.Match
                                {
                                    Pattern: Pattern.Variable values,
                                    Value: Expr.List { Tail: null } parts
                                },
                                    Expr.Match { Pattern: Pattern.Variable total, Value: Expr.Binary sum },
                                    Expr.Call
                                {
                                    Module: null, DynamicRemote: false,
                                    Arguments: [Expr.Variable usingTags, Expr.Variable usingValues, Expr.Variable usingTotal]
                                } finish
                                ]
                        } sequence
                    }
                ]
                || !DistinctBindings(
                    input,
                    tags,
                    values,
                    total
                )
                || input.Name != original.Name || tags.Name != usingTags.Name || values.Name != usingValues.Name || total.Name != usingTotal.Name
                || !entries.TryGetValue((finish.Function, 3), out int finishEntry))
            {
                return false;
            }

            int count = record.Items.Count - 1;
            if (count is not (3 or 4 or 5) || parts.Items.Count != count || sequence.Items.Count != count + 4)
            {
                return false;
            }

            var names = new HashSet<string>(StringComparer.Ordinal) { input.Name, tags.Name, values.Name, total.Name };
            var inputs = new Pattern.Variable[count];
            for (int index = 0; index < count; index++)
            {
                if (record.Items[index + 1] is not Pattern.Variable member
                    || !DistinctBindings(member) || !names.Add(member.Name))
                {
                    return false;
                }

                inputs[index] = member;
            }

            var fields = sequence.Items.Skip(1).Take(count).ToArray();
            var lengths = new string[fields.Length];
            var plans = new SequenceEncodePart?[fields.Length];
            Expr.Call? external = null;
            string? module = null;
            Term? externalOptions = null;
            int externalIndex = -1;
            for (int index = 0; index < fields.Length; index++)
            {
                if (fields[index] is not Expr.Match
                    {
                        Pattern: Pattern.Tuple { Items: [Pattern.Variable data, Pattern.Variable length] },
                        Value: Expr.Call call
                    }
                    || !DistinctBindings(data, length) || !names.Add(data.Name) || !names.Add(length.Name)
                    || parts.Items[index] is not Expr.Variable emitted || emitted.Name != data.Name)
                {
                    return false;
                }

                lengths[index] = length.Name;
                if (call.Module is not null)
                {
                    if (index == 0 || external is not null || call is not
                        {
                            Module: not null, DynamicRemote: false, Arguments: [Expr.Variable payload, var options]
                        }
                        || payload.Name != inputs[index].Name || !AttributeLiteral.TryNormalizeConstant(options, out externalOptions)
                        || !TryResolveOrdinaryCallTarget(call, out module) || module is null)
                    {
                        return false;
                    }

                    external = call;
                    externalIndex = index;
                }
                else
                {
                    // The untagged leading choice can return a proven pair with an opaque forwarded length.
                    plans[index] = ReadRequiredOptionalSequenceField(
                        call,
                        inputs[index].Name,
                        allowForeign: count == 5 && index == 0
                    );
                    if (plans[index] is null)
                    {
                        return false;
                    }
                }
            }

            bool namedLeader = plans[0]!.Constants is [var namedNumbers, _] && IsClosedNamedNumberList(namedNumbers);
            bool plainFields = externalIndex == 2 && count == 3 && externalOptions is Cons
                && plans.Take(2).All(plan => plan is { Constants: [Cons] })
                || externalIndex == 1 && plans.Skip(2).All(plan => plan is { Constants: [Cons] })
                && (count == 4 && externalOptions is Nil && plans[0]!.Constants is [Cons]
                    || count == 5 && externalOptions is Cons && plans[0]!.Constants is [Nil]);
            if (external is null || module is null || externalOptions is null || (namedLeader
                ? externalIndex != 1 || count == 5 || count == 4 && externalOptions is not Nil
                : !plainFields || module == ErlangModuleNames.Module))
            {
                return false;
            }

            var additions = new Expr.Binary[lengths.Length - 1];
            Expr remaining = sum;
            for (int index = lengths.Length - 1; index > 0; index--)
            {
                if (remaining is not Expr.Binary { Operator: ErlangOperators.Plus, Right: Expr.Variable right } add
                    || right.Name != lengths[index])
                {
                    return false;
                }

                additions[index - 1] = add;
                remaining = add.Left;
            }

            if (remaining is not Expr.Variable initial || initial.Name != lengths[0])
            {
                return false;
            }

            if (!namedLeader)
            {
                plans[externalIndex] = new SequenceEncodePart(
                    external,
                    0,
                    [externalOptions],
                    null,
                    null
                );

                return EmitRequiredSequenceWithForeignPair(
                    tuple,
                    plans,
                    externalIndex,
                    module,
                    finishEntry,
                    additions
                );
            }

            int malformedResult = Label();
            int malformedInput = Label();
            EmitTupleShapeTest(
                XRegisterTag,
                0,
                count + 1,
                malformedInput
            );
            EmitFrameAllocation(2, 4);
            EmitFrameInitialization(0, 2);
            Move(
                XRegisterTag,
                1,
                YRegisterTag,
                2
            );
            Move(
                XRegisterTag,
                0,
                YRegisterTag,
                3
            );
            owner.EmitTuplePart(0, 1, 0);
            BeamSourceLocation previous = location;
            Encode(plans[0]!);
            Project(2, 1);
            Move(
                XRegisterTag,
                0,
                YRegisterTag,
                1
            );
            owner.MoveToX0(XRegisterTag, 1);
            owner.EmitConstant(externalOptions!, 1);
            owner.EmitCallLine(external!, previous);
            Call(module, external!.Function, 2);
            previous = CallLocation(external, previous);
            Move(
                XRegisterTag,
                0,
                YRegisterTag,
                0
            );
            EmitTupleShapeTest(
                XRegisterTag,
                0,
                2,
                malformedResult
            );
            Project(3, 0);
            if (count == 3)
            {
                Encode(plans[2]!, trimSavedPair: true);
                EmitSequencePairResult(
                    count,
                    3,
                    finishEntry,
                    additions,
                    previous,
                    pairSlots: [0, 2],
                    outerTagSlot: 1,
                    joinedLengthTypes: [0, 0, 0]
                );
            }
            else
            {
                Encode(plans[2]!);
                Project(4, 1);
                Move(
                    XRegisterTag,
                    0,
                    YRegisterTag,
                    3
                );
                owner.MoveToX0(XRegisterTag, 1);
                Encode(plans[3]!);
                EmitSequencePairResult(
                    count,
                    4,
                    finishEntry,
                    additions,
                    previous,
                    pairSlots: [1, 0, 3],
                    outerTagSlot: 2,
                    secondLengthType: 0
                );
            }

            Mark(malformedResult);
            owner.EmitCallLine(external, location);
            owner.Op(BadMatch, XRegisterTag, 0);
            Mark(malformedInput);
            previous = location;
            owner.EmitArithmeticLine(tuple.SourceLine, ref previous);
            owner.Op(BadMatch, XRegisterTag, 0);

            return true;

            void Move(
                int fromTag,
                int from,
                int toTag,
                int to
            ) => owner.MoveRegister(
                fromTag,
                from,
                toTag,
                to
            );

            void Project(int field, int target)
            {
                owner.Op(GetTupleElement, YRegisterTag, 3);
                owner.Operand(BeamWire.UnsignedTag, field);
                owner.Operand(XRegisterTag, target);
            }

            void Encode(SequenceEncodePart plan, bool trimSavedPair = false)
            {
                for (int index = plan.Constants.Count - 1; index >= 0; index--)
                {
                    owner.EmitConstant(plan.Constants[index], index + 1);
                }

                if (trimSavedPair)
                {
                    Move(
                        YRegisterTag,
                        0,
                        YRegisterTag,
                        3
                    );
                    owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
                    owner.Operand(BeamWire.UnsignedTag, 3);
                }

                owner.EmitCallLine(plan.Call, previous);
                previous = CallLocation(plan.Call, previous);
                owner.Op(LocalCall, BeamWire.UnsignedTag, plan.Call.Arguments.Count);
                owner.Operand(LabelTag, plan.Entry);
            }
        }

        private bool EmitRequiredSequenceWithForeignPair(
            Expr.Match unpack,
            IReadOnlyList<SequenceEncodePart?> fields,
            int foreignIndex,
            string module,
            int finish,
            IReadOnlyList<Expr.Binary> additions
        )
        {
            int frame = fields.Count;
            int record = frame - 1;
            int tags = frame - 2;
            int[] slots = Enumerable.Range(0, frame - 1)
                .Select(index => index == frame - 2 ? record : frame - 3 - index)
                .ToArray();
            int badPair = Label();
            int badRecord = Label();
            EmitTupleShapeTest(
                XRegisterTag,
                0,
                frame + 1,
                badRecord
            );
            EmitFrameAllocation(2, frame);
            EmitFrameInitialization(0, tags);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                tags
            );
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                record
            );
            var previous = location;

            bool pending = false;
            for (int index = 0; index < fields.Count; index++)
            {
                SequenceEncodePart field = fields[index]!;
                owner.Op(GetTupleElement, index == 0 ? XRegisterTag : YRegisterTag, index == 0 ? 0 : record);
                owner.Operand(BeamWire.UnsignedTag, index + 1);
                owner.Operand(XRegisterTag, pending ? 1 : 0);
                if (pending)
                {
                    // Project from the original record before its slot is reused by the penultimate result.
                    owner.MoveRegister(
                        XRegisterTag,
                        0,
                        YRegisterTag,
                        slots[index - 1]
                    );
                    owner.MoveToX0(XRegisterTag, 1);
                }

                owner.EmitConstant(field.Constants[0], 1);
                owner.EmitCallLine(field.Call, previous);
                previous = CallLocation(field.Call, previous);
                if (index == foreignIndex)
                {
                    Call(module, field.Call.Function, 2);
                    if (index < slots.Length)
                    {
                        owner.MoveRegister(
                            XRegisterTag,
                            0,
                            YRegisterTag,
                            slots[index]
                        );
                    }

                    EmitTupleShapeTest(
                        XRegisterTag,
                        0,
                        2,
                        badPair
                    );
                    pending = false;
                }
                else
                {
                    owner.EmitFieldCall(2, field.Entry);
                    pending = true;
                }
            }

            EmitSequencePairResult(
                frame,
                frame,
                finish,
                additions,
                previous,
                pairSlots: slots,
                outerTagSlot: tags,
                joinedLengthTypes: new int[frame]
            );

            Mark(badPair);
            owner.EmitCallLine(fields[foreignIndex]!.Call, location);
            owner.Op(BadMatch, XRegisterTag, 0);
            Mark(badRecord);
            previous = location;
            owner.EmitArithmeticLine(unpack.SourceLine, ref previous);
            owner.Op(BadMatch, XRegisterTag, 0);

            return true;
        }
    }
}
