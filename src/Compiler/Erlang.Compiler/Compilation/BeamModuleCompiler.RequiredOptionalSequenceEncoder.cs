using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record SequenceEncodePart(
            Expr.Call Call,
            int Entry,
            IReadOnlyList<Term> Constants,
            Term? Omits,
            Term? Omitted,
            int MatchLine = 0
        );

        private bool TryEmitRequiredOptionalSequenceEncoder(FunctionDefinition function, bool retainRecord = false)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || function.Clauses is not
                [
                {
                    Guard: null, Patterns: [Pattern.Variable input, Pattern.Variable tags],
                    Body: Expr.Sequence
                    {
                        Items:
                                [
                                    Expr.Match { Pattern: Pattern.Tuple tuple, Value: Expr.Variable source } unpack,
                                    ..,
                                    Expr.Match
                                {
                                    Pattern: Pattern.Variable parts,
                                    Value: Expr.List { Tail: null } bytes
                                },
                                    Expr.Match { Pattern: Pattern.Variable length, Value: var sum },
                                    Expr.Call
                                {
                                    Module: null, DynamicRemote: false,
                                    Arguments: [Expr.Variable emittedTags, Expr.Variable emittedParts, Expr.Variable emittedLength]
                                } finish
                                ]
                    } sequence
                }
                ]
                || !DistinctBindings(
                    input,
                    tags,
                    parts,
                    length
                ) || source.Name != input.Name
                || emittedTags.Name != tags.Name || emittedParts.Name != parts.Name || emittedLength.Name != length.Name
                || !entries.TryGetValue((finish.Function, 3), out int finishEntry))
            {
                return false;
            }

            int count = sequence.Items.Count - 4;
            if (count < 1 || count + 2 >= FirstReservedXRegister || count + 1 != tuple.Items.Count
                || count != bytes.Items.Count || tuple.Items[0] is not Pattern.Variable { Name: VariableScopeNames.Wildcard })
            {
                return false;
            }

            if (retainRecord && (owner.OnLoadFunction is not null
                || localFunctions.Keys.Any(owner.IsDeclaredNative)))
            {
                return false;
            }

            var fields = new SequenceEncodePart[count];
            var retainedPredicates = retainRecord ? new DefaultEncodeField?[count] : null;
            var lengths = new string[count];
            var names = new HashSet<string>(StringComparer.Ordinal) { input.Name, tags.Name, parts.Name, length.Name };
            int required = 0;
            DefaultEncodeField? finalDefault = null;
            DefaultEncodeField? middleDefault = null;
            DefaultEncodeField? firstDefault = null;
            RequiredPairEnumeration? requiredEnumeration = null;
            bool trailingOptionalStarted = false;
            bool allOptional = true;
            for (int index = 0; index < count; index++)
            {
                if (tuple.Items[index + 1] is not Pattern.Variable value
                    || sequence.Items[index + 1] is not Expr.Match
                    {
                        Pattern: Pattern.Tuple { Items: [Pattern.Variable encoded, Pattern.Variable measured] }, Value: var field
                    } binding
                    || bytes.Items[index] is not Expr.Variable payload || payload.Name != encoded.Name
                    || value.Name == VariableScopeNames.Wildcard || encoded.Name == VariableScopeNames.Wildcard
                    || measured.Name == VariableScopeNames.Wildcard || !names.Add(value.Name)
                    || !names.Add(encoded.Name) || !names.Add(measured.Name))
                {
                    return false;
                }

                if (retainedPredicates is not null)
                {
                    bool allowsLeadingForeign = count == 6 && index == 0;
                    bool allowsFinalForeign = count == 4 && index == count - 1
                        && fields[0] is { Omits: null, Call.Module: null, Constants: [Cons] }
                        && fields[1] is { Omits: null, Call.Module: null, Constants: [Cons] }
                        && retainedPredicates[2] is
                        {
                            Names: null, Tags: Cons, Default: TupleTerm { Items: [Nil, Integer omittedLength] }
                        } && omittedLength.Value.IsZero;
                    if (count == 6 && index == 2
                        && fields[0] is { Omits: null, Call.Module: not null, Constants: [Nil] }
                        && fields[1] is { Omits: null, Call.Module: null, Constants: [Cons] }
                        && ReadRequiredPairEnumeration(field, value.Name, names) is { } enumeration)
                    {
                        requiredEnumeration = enumeration;
                        fields[index] = new(
                            enumeration.Branches[0].Call,
                            enumeration.Entry,
                            [],
                            null,
                            null
                        );
                    }
                    else if (field is Expr.Case conditional
                        && ReadDefaultEncodeField(conditional, value.Name, allowNamedOptions: true) is { } retainedPredicate)
                    {
                        retainedPredicates[index] = retainedPredicate;
                        fields[index] = new(
                            retainedPredicate.Encode,
                            retainedPredicate.EncodeEntry,
                            retainedPredicate.Names is { } table ? [table, retainedPredicate.Tags] : [retainedPredicate.Tags],
                            Term.A(ErlangBooleanAtoms.True),
                            retainedPredicate.Default
                        );
                    }
                    else if (ReadRequiredOptionalSequenceField(
                        field,
                        value.Name,
                        allowForeign: true,
                        allowForeignOptional: count == 3 && index > 0
                    ) is { } retained
                    && (retained.Call.Module is null || allowsLeadingForeign && retained is { Omits: null, Constants: [Nil] }
                        || allowsFinalForeign && retained is { Omits: null, Constants: [Cons] }
                        || count == 3 && index > 0
                        && retained is { Omits: Atom, Constants.Count: 1 }
                        && (index == 1 || fields[0].Omits is null
                            && fields[1] is { Omits: Atom, Call.Module: not null })))
                    {
                        fields[index] = retained with { MatchLine = binding.SourceLine };
                    }
                    else
                    {
                        return false;
                    }

                    lengths[index] = measured.Name;
                    continue;
                }

                bool namedRequiredPrefix = index > 0 && HasNamedRequiredLeader(fields[0]);
                bool admitsOptionalBookends = count == 4 && index > 0 && firstDefault is null
                    && fields[0] is { Omits: Atom, Call.Module: null };
                bool followsLeadingForeignRequired = count == 4 && index == 1
                    && fields[0] is { Omits: null, Call.Module: not null, Constants: [Nil] };
                bool allowForeignOptional = firstDefault is not null && count == 4 && index == 1
                    || (allOptional || count == 2 && index == 1 || admitsOptionalBookends && index == count - 1
                        || followsLeadingForeignRequired)
                        && field is Expr.Case;
                bool followsLeadingForeignOptional = count == 2 && index == 1
                    && fields[0] is { Omits: Atom, Call.Module: not null };
                bool readsLeadingRequiredPair = count is 2 or 4 && index == 0 && field is Expr.Call
                    && sequence.Items[2] is Expr.Match { Value: Expr.Case };
                bool followsForeignSequenceOptional = count == 3 && index == 2 && allOptional
                    && fields.Take(index).Any(part => part.Call.Module is not null);
                SequenceEncodePart? plan = ReadRequiredOptionalSequenceField(
                    field,
                    value.Name,
                    allowForeign: firstDefault is not null || allowForeignOptional || followsLeadingForeignOptional || readsLeadingRequiredPair
                        || followsForeignSequenceOptional || count == 1 || namedRequiredPrefix || admitsOptionalBookends,
                    allowForeignOptional: allowForeignOptional
                );
                if (plan is null && count >= 4 && index == 0 && field is Expr.Case firstCondition
                    && ReadDefaultEncodeField(firstCondition, value.Name, allowNamedOptions: true) is { Names: { } leadingNames } leading)
                {
                    firstDefault = leading;
                    plan = new(
                        leading.Encode,
                        leading.EncodeEntry,
                        [leadingNames, leading.Tags],
                        Term.A(ErlangBooleanAtoms.True),
                        leading.Default
                    );
                }
                if (plan is null && field is Expr.Case condition
                    && (index == count - 1 || count == 3 && index == 1)
                    && ReadDefaultEncodeField(condition, value.Name) is { } predicate)
                {
                    if (index == count - 1)
                    {
                        finalDefault = predicate;
                    }
                    else
                    {
                        middleDefault = predicate;
                    }

                    plan = new(
                        predicate.Encode,
                        predicate.EncodeEntry,
                        [predicate.Tags],
                        Term.A(ErlangBooleanAtoms.True),
                        predicate.Default
                    );
                }

                if (plan is null)
                {
                    return false;
                }

                if (plan.Omits is null)
                {
                    bool finalRequired = allOptional && index == count - 1 && (count >= 4 || followsForeignSequenceOptional)
                        && plan.Call.Module is null;
                    allOptional = false;
                    bool followsForeignOptional = count == 4 && index == 2
                        && fields[1] is { Omits: not null, Call.Module: not null }
                        && plan.Call.Module is null;
                    if (firstDefault is not null && trailingOptionalStarted && !followsForeignOptional
                        || firstDefault is null && index != required && !finalRequired
                            && !followsLeadingForeignOptional && !(middleDefault is not null && index == 2)
                            && !(admitsOptionalBookends && index < count - 1 && plan.Call.Module is null))
                    {
                        return false;
                    }

                    required++;
                }
                else if (index > 0)
                {
                    trailingOptionalStarted = true;
                }

                fields[index] = plan with { MatchLine = binding.SourceLine };
                lengths[index] = measured.Name;
            }

            if (count == 1)
            {
                return !retainRecord && sum is Expr.Variable onlyLength && onlyLength.Name == lengths[0]
                    && fields[0] is { Omits: null, Call.Module: null, Constants.Count: 1 } only
                    && EmitSingletonRecordPair(unpack, only, finishEntry);
            }

            if (fields is
                [
                { Omits: null, Call.Module: null, Constants: [var namesList, _] },
                { Omits: null, Call.Module: null },
                { Omits: not null, Call.Module: null }
                ]
                && IsClosedNamedNumberList(namesList) && finalDefault is null)
            {
                // The deferred-pair emitter carries the result before the final optional in the tuple input slot.
                return false;
            }

            bool compactMixed = fields is
                [
                { Omits: not null, Call.Module: null },
                { Omits: not null, Call.Module: not null },
                { Omits: null, Call.Module: null },
                { Omits: not null, Call.Module: null }
                ];
            bool namedRequiredWithOptionalTail = HasNamedRequiredLeader(fields[0])
                && firstDefault is null && finalDefault is null && middleDefault is null
                && required < count && fields.Take(required).All(field => field.Omits is null)
                && (fields.Take(required).Any(field => field.Call.Module is not null)
                    || required == 1 && fields[^1].Constants is [var namedSuffix, _] && IsClosedNamedNumberList(namedSuffix))
                && fields.Skip(required).All(field => field is { Omits: Atom, Call.Module: null })
                && fields.Skip(1).Take(required - 1).All(field => field.Constants.Count == 1);
            bool localNamedThenUntaggedOptional = HasNamedRequiredLeader(fields[0])
                && fields is
                [
                { Constants: [_, Cons] },
                { Omits: null, Call.Module: null, Constants: [Cons] },
                { Omits: Atom firstMarker, Call.Module: null, Constants: [Cons] },
                { Omits: Atom lastMarker, Call.Module: null, Constants: [Nil] }
                ] && firstMarker.Equals(lastMarker) && firstDefault is null && finalDefault is null && middleDefault is null;
            bool checkedForeignThenNamedOptional = fields is
                [
                { Omits: null, Call.Module: not null, Constants: [Nil] },
                { Omits: Atom remoteMarker, Call.Module: not null, Constants: [Nil] },
                { Omits: Atom localMarker, Call.Module: null, Constants: [Cons] },
                { Omits: Atom finalMarker, Call.Module: null, Constants: [var positions, Cons] }
                ] && remoteMarker.Equals(localMarker) && remoteMarker.Equals(finalMarker)
                && IsClosedNamedNumberList(positions) && firstDefault is null && finalDefault is null && middleDefault is null;
            if (count == 4 && fields[0] is { Omits: null, Call.Module: not null } && !checkedForeignThenNamedOptional)
            {
                return false;
            }

            bool checkedBookends = fields is
                [
                { Omits: Atom, Call.Module: null, Constants.Count: 1 },
                { Omits: null, Call.Module: null, Constants.Count: 1 },
                { Omits: null, Call.Module: null, Constants.Count: 1 },
                { Omits: Atom, Call.Module: not null, Constants.Count: 1 }
                ] && firstDefault is null && finalDefault is null && middleDefault is null;
            bool checkedOptional = allOptional && firstDefault is null && finalDefault is null && middleDefault is null
                && fields.Any(field => field.Call.Module is not null);
            bool trimFinalRequired = (count >= 4 || count == 3 && fields.Take(count - 1).Any(field => field.Call.Module is not null))
                && firstDefault is null && finalDefault is null && middleDefault is null
                && required == 1 && fields[^1] is { Omits: null, Call.Module: null }
                && fields.Take(count - 1).All(field => field.Omits is not null);
            bool checkedPairWithOptional = fields is
                [
                { Omits: null },
                { Omits: not null }
                ] && (fields[0].Call.Module is not null || fields[1].Call.Module is not null);
            bool checkedOptionalThenRequired = fields is
                [
                { Omits: Atom, Call.Module: not null },
                { Omits: null }
                ];
            if (!retainRecord && !checkedOptional && !checkedPairWithOptional && !trimFinalRequired && !checkedOptionalThenRequired
                && !namedRequiredWithOptionalTail && !checkedBookends && !localNamedThenUntaggedOptional && !checkedForeignThenNamedOptional
                && (firstDefault is not null && (required < 3 && !compactMixed || finalDefault is not null || !trailingOptionalStarted)
                || firstDefault is null && (required != 2 || required == count || (middleDefault is null && (finalDefault is null
                    ? fields[0].Constants is not [var named, _] || !IsClosedNamedNumberList(named)
                    : count != 4 || fields[0].Constants is not [Nil] || fields[2].Omits is not Atom)))))
            {
                return false;
            }

            var additions = new Expr.Binary[count - 1];
            Expr remaining = sum;
            for (int index = count - 1; index > 0; index--)
            {
                if (remaining is not Expr.Binary
                    {
                        Operator: ErlangOperators.Plus, Right: Expr.Variable next
                    } add || next.Name != lengths[index])
                {
                    return false;
                }

                additions[index - 1] = add;
                remaining = add.Left;
            }

            if (remaining is not Expr.Variable first || first.Name != lengths[0])
            {
                return false;
            }

            if (retainedPredicates is not null)
            {
                if (count == 2)
                {
                    return retainedPredicates is [{ Names: null } predicate, null]
                        && ReadDeferredOptional(((Expr.Match)sequence.Items[2]).Value) is
                        { Second: null, Call: { Module: null, Arguments.Count: 2 } } optional
                        && fields[1].Omits is not null
                        && EmitEagerDefaultOptionalPair(
                            unpack,
                            predicate,
                            optional,
                            additions[0],
                            finishEntry
                        );
                }

                bool allConditional = fields.All(field => field.Omits is not null);
                bool leadingRequired = fields is
                [
                { Omits: null },
                { Omits: not null },
                { Omits: not null }
                ] && (retainedPredicates[1] is not null || fields[1].Call.Module is not null)
                    && retainedPredicates[2] is null;
                bool checkedFinalAfterDefault = retainedPredicates is [null, null, { Names: null }, null]
                    && fields[^1] is { Omits: null, Call.Module: not null };
                bool checkedEnumeratedPrefix = requiredEnumeration is not null && retainedPredicates.All(predicate => predicate is null)
                    && fields.Skip(3).All(field => field is { Omits: Atom, Call.Module: null, Constants: [Cons] })
                    && fields[3].Omits!.Equals(fields[4].Omits) && fields[3].Omits!.Equals(fields[5].Omits);
                if (!allConditional && !leadingRequired && !checkedFinalAfterDefault && !checkedEnumeratedPrefix)
                {
                    return false;
                }

                return EmitDefaultPrefixedSequence(
                    unpack,
                    fields,
                    null,
                    finishEntry,
                    additions,
                    retainedPredicates,
                    requiredEnumeration
                );
            }

            if (namedRequiredWithOptionalTail || checkedBookends || localNamedThenUntaggedOptional || checkedForeignThenNamedOptional)
            {
                return EmitDefaultPrefixedSequence(
                    unpack,
                    fields,
                    null,
                    finishEntry,
                    additions
                );
            }

            if (checkedOptional || checkedPairWithOptional || trimFinalRequired || checkedOptionalThenRequired)
            {
                if (fields is [var left, var right])
                {
                    var firstOptional = ReadDeferredOptional(((Expr.Match)sequence.Items[1]).Value);
                    var lastOptional = ReadDeferredOptional(((Expr.Match)sequence.Items[2]).Value);
                    if (left.Omits is not null && firstOptional is not { Second: null }
                        || right.Omits is not null && lastOptional is not { Second: null }
                        || left.Call.Arguments.Count is not (2 or 3) || firstOptional is not null && left.Call.Arguments.Count != 2
                        || right.Call.Arguments.Count != 2)
                    {
                        return false;
                    }

                    EmitCheckedOptionalPairFields(
                        unpack,
                        left,
                        right,
                        firstOptional,
                        lastOptional,
                        additions[0],
                        finishEntry
                    );

                    return true;
                }

                return EmitDefaultPrefixedSequence(
                    unpack,
                    fields,
                    null,
                    finishEntry,
                    additions
                );
            }

            if (firstDefault is { } firstPredicate)
            {
                return EmitDefaultPrefixedSequence(
                    unpack,
                    fields,
                    firstPredicate,
                    finishEntry,
                    additions
                );
            }

            if (middleDefault is { } middle)
            {
                return EmitRequiredDefaultRequired(
                    unpack,
                    fields[0],
                    middle,
                    fields[2],
                    finishEntry,
                    additions
                );
            }

            var branches = Enumerable.Range(0, count - required).Select(_ => (Present: Label(), Ready: Label())).ToArray();
            int failure = Label();
            int frame = count + 1;
            owner.EmitTupleCheck(0, count + 1, failure);
            EmitFrameAllocation(arity: 2, size: frame);
            EmitFrameInitialization(0, count - 1);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                count - 1
            );
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                count
            );
            var previous = location;

            for (int index = 0; index < count; index++)
            {
                int slot = count - 2 - index;
                int current = index > 0 && index <= required ? 1 : 0;
                var field = fields[index];
                if (index == count - 1 && finalDefault is { } predicate)
                {
                    var (encode, done) = branches[index - required];
                    owner.Op(GetTupleElement, YRegisterTag, count);
                    owner.Operand(BeamWire.UnsignedTag, index + 1);
                    owner.Operand(YRegisterTag, count);
                    owner.MoveToX0(YRegisterTag, count);
                    owner.EmitCallLine(predicate.Predicate, previous);
                    previous = CallLocation(predicate.Predicate, previous);
                    owner.EmitFieldCall(1, predicate.PredicateEntry);
                    owner.Op(ExactEqualTest, LabelTag, encode);
                    owner.Operand(XRegisterTag, 0);
                    owner.LiteralOperand(Term.A(ErlangBooleanAtoms.True));
                    owner.EmitConstant(predicate.Default, 0);
                    JumpTo(done);
                    Mark(encode);
                    owner.EmitConstant(predicate.Tags, 1);
                    owner.MoveToX0(YRegisterTag, count);
                    EmitFrameInitialization(count, count + 1);
                    owner.EmitCallLine(predicate.Encode, previous);
                    previous = CallLocation(predicate.Encode, previous);
                    owner.EmitFieldCall(2, predicate.EncodeEntry);
                    Mark(done);

                    continue;
                }

                if (index == 0)
                {
                    owner.EmitTuplePart(0, 1, 0);
                }
                else
                {
                    owner.Op(GetTupleElement, YRegisterTag, count);
                    owner.Operand(BeamWire.UnsignedTag, index + 1);
                    owner.Operand(XRegisterTag, current);
                }

                if (current == 1)
                {
                    owner.MoveRegister(
                        XRegisterTag,
                        0,
                        YRegisterTag,
                        slot + 1
                    );
                }

                if (field.Omits is null)
                {
                    EmitField(field, current);

                    continue;
                }

                var (present, ready) = branches[index - required];
                owner.Op(ExactEqualTest, LabelTag, present);
                owner.Operand(XRegisterTag, current);
                owner.LiteralOperand(field.Omits);
                bool last = index == count - 1;
                owner.instructions.WriteByte(BeamEmissionFormat.Move);
                owner.LiteralOperand(field.Omitted!);
                owner.Operand(last ? XRegisterTag : YRegisterTag, last ? 0 : slot);
                JumpTo(ready);
                Mark(present);
                if (last)
                {
                    EmitField(field, current, deadRecord: count);
                }
                else
                {
                    EmitField(field, current);
                    owner.MoveRegister(
                        XRegisterTag,
                        0,
                        YRegisterTag,
                        slot
                    );
                }

                Mark(ready);
            }

            EmitSequencePairResult(
                count,
                frame,
                finishEntry,
                additions,
                previous
            );

            Mark(failure);
            previous = location;
            owner.EmitArithmeticLine(unpack.SourceLine, ref previous);
            owner.Op(BadMatch, XRegisterTag, 0);

            return true;

            void EmitField(SequenceEncodePart field, int current, int? deadRecord = null)
            {
                if (current == 1)
                {
                    owner.MoveToX0(XRegisterTag, current);
                }

                for (int option = field.Constants.Count - 1; option >= 0; option--)
                {
                    owner.EmitConstant(field.Constants[option], option + 1);
                }

                if (deadRecord is int slot)
                {
                    EmitFrameInitialization(slot, slot + 1);
                }

                owner.EmitCallLine(field.Call, previous);
                previous = CallLocation(field.Call, previous);
                owner.Op(LocalCall, BeamWire.UnsignedTag, field.Call.Arguments.Count);
                owner.Operand(LabelTag, field.Entry);
            }
        }

        private static bool IsClosedNamedNumberList(Term value)
        {
            if (value is not Cons)
            {
                return false;
            }

            while (value is Cons pair)
            {
                if (pair.Head is not TupleTerm { Items: [Atom, Integer] })
                {
                    return false;
                }

                value = pair.Tail;
            }

            return value is Nil;
        }

        private static bool HasNamedRequiredLeader(SequenceEncodePart field) =>
            field is { Omits: null, Call.Module: null, Constants: [var names, _] } && IsClosedNamedNumberList(names);

        private bool EmitSingletonRecordPair(Expr.Match unpack, SequenceEncodePart field, int finish)
        {
            int invalid = Label();
            owner.EmitTupleCheck(0, 2, invalid);
            EmitFrameAllocation(arity: 2, size: 1);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                0
            );
            owner.EmitTuplePart(0, 1, 0);
            owner.EmitConstant(field.Constants[0], 1);
            owner.EmitCallLine(field.Call, location);
            owner.EmitFieldCall(2, field.Entry);
            owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTupleElement(0, 1);
            owner.Op(PutList, XRegisterTag, 1);
            owner.Operand(AtomTag, NilAtomIndex);
            owner.Operand(XRegisterTag, 1);
            owner.EmitTupleElement(1, 2);
            owner.MoveToX0(YRegisterTag, 0);
            EmitTailLocal(finish, 3, 1);

            Mark(invalid);
            var previous = location;
            owner.EmitArithmeticLine(unpack.SourceLine, ref previous);
            owner.Op(BadMatch, XRegisterTag, 0);

            return true;
        }

        private SequenceEncodePart? ReadRequiredOptionalSequenceField(
            Expr value,
            string name,
            bool allowForeign = false,
            bool allowForeignOptional = false
        )
        {
            Term? omits = null;
            Term? omitted = null;
            if (value is Expr.Case decision)
            {
                if (decision is not
                    {
                        Value: Expr.Variable tested,
                        Clauses:
                            [
                            { Guard: null, Patterns: [Pattern.Literal marker], Body: var empty },
                            {
                                Guard: null, Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                                Body: Expr.Call fallback
                            }
                            ]
                    }
                    || tested.Name != name || !AttributeLiteral.TryNormalizeConstant(empty, out omitted)
                    || omitted is not TupleTerm { Items: [_, Integer or FloatTerm] })
                {
                    return null;
                }

                omits = marker.Value;
                value = fallback;
            }

            if (value is not Expr.Call
                {
                    DynamicRemote: false, Arguments: [Expr.Variable input, ..]
                } call || input.Name != name || call.Arguments.Count >= FirstReservedXRegister
                || call.Module is not null && (!allowForeign || omits is not null && !allowForeignOptional || call.Module == ErlangModuleNames.Module))
            {
                return null;
            }

            int entry = 0;
            if (call.Module is null && (!entries.TryGetValue((call.Function, call.Arguments.Count), out entry)
                || !ProducesNumericPair(call) && (!allowForeign || !ReturnsTuple(
                    call,
                    2,
                    [],
                    exact: true
                ))))
            {
                return null;
            }

            var constants = new Term[call.Arguments.Count - 1];
            for (int index = 0; index < constants.Length; index++)
            {
                if (!AttributeLiteral.TryNormalizeConstant(call.Arguments[index + 1], out Term constant))
                {
                    return null;
                }

                constants[index] = constant;
            }

            return new(
                call,
                entry,
                constants,
                omits,
                omitted
            );
        }
    }
}
