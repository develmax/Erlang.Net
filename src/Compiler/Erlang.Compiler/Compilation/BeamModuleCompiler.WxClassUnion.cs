using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitWxClassIndexOverload(FunctionDefinition function)
        {
            int arity = function.Arity;
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || arity is < 2 or > 4
                || function.Clauses is not [var compound, var numeric]
                || numeric.Patterns[1] is not Pattern.Variable scalar
                || ReadWxIndexExtras(compound) is not { } compoundExtras
                || ReadWxIndexExtras(numeric) is not { } numericExtras
                || compoundExtras.Tuple != numericExtras.Tuple || compoundExtras.Mapped != numericExtras.Mapped
                || !ReadsBounds(compound.Guard, compoundExtras.Guards, compoundExtras.Mapped)
                || !ReadsBounds(numeric.Guard, [scalar.Name, .. numericExtras.Guards], numericExtras.Mapped)
                || ReadWxClassUnionObject(compound.Patterns[0]) is not { } compoundReceiver
                || ReadWxClassUnionObject(numeric.Patterns[0]) is not { } scalarReceiver
                || compound.Body is not Expr.Sequence { Items: [var compoundCheck, ..] }
                || numeric.Body is not Expr.Sequence { Items: [var scalarCheck, ..] }
                || ReadWxClassTest(compoundCheck, compoundReceiver.Type) is not { } expected
                || ReadWxClassTest(scalarCheck, scalarReceiver.Type) is not { } fallback
                || !expected.Class.Equals(fallback.Class))
            {
                return false;
            }

            int receiverType = arity;
            var union = function with { Clauses = [compound] };
            var integer = function with { Clauses = [numeric] };
            if (!TryEmitWxClassUnionCommand(union, validateOnly: true, preparedReceiver: receiverType)
                || !TryEmitWxObjectCommand(
                    integer,
                    sharedType: receiverType,
                    validateOnly: true,
                    scalarAfterUnion: true
                ))
            {
                return false;
            }

            int failure = owner.nextLabel - 2;
            int scalarEntry = Label();
            EmitDetailRecordTest(
                0,
                4,
                Term.A("wx_ref"),
                failure
            );
            owner.EmitTuplePart(0, 2, receiverType);
            EmitDetailRecordTest(
                1,
                4,
                Term.A("wx_ref"),
                scalarEntry
            );
            EmitExtraShape(scalarEntry);
            EmitExtras(compoundExtras.Tuple ? scalarEntry : failure);
            TryEmitWxClassUnionCommand(union, functionFailure: failure, preparedReceiver: receiverType);
            Mark(scalarEntry);
            EmitExtraShape(failure);
            EmitIntegerTest(1, failure);
            EmitExtras(failure);

            return TryEmitWxObjectCommand(
                integer,
                sharedType: receiverType,
                functionFailure: failure,
                scalarAfterUnion: true
            );

            void EmitExtraShape(int failedArity)
            {
                if (compoundExtras.Tuple)
                {
                    owner.Op(IsTuple, LabelTag, failure);
                    owner.Operand(XRegisterTag, 2);
                    owner.Op(TestArity, LabelTag, failedArity);
                    owner.Operand(XRegisterTag, 2);
                    owner.Operand(BeamWire.UnsignedTag, compoundExtras.Guards.Length);
                }
            }

            void EmitExtras(int rejected)
            {
                for (int index = 0; index < compoundExtras.Guards.Length; index++)
                {
                    if (compoundExtras.Tuple)
                    {
                        owner.EmitTuplePart(2, index, receiverType + 1);
                        EmitIntegerTest(receiverType + 1, rejected);
                    }
                    else if (compoundExtras.Mapped)
                    {
                        owner.Op(IsList, LabelTag, rejected);
                        owner.Operand(XRegisterTag, index + 2);
                    }
                    else
                    {
                        EmitIntegerTest(index + 2, rejected);
                    }
                }
            }

            void EmitIntegerTest(int register, int rejected)
            {
                owner.Op(IsInteger, LabelTag, rejected);
                owner.Operand(XRegisterTag, register);
            }

            bool ReadsBounds(Expr? guard, string[] bounds, bool lastList) => bounds.Length == 0 ? guard is null
                : guard is not null && bounds.Distinct(StringComparer.Ordinal).Count() == bounds.Length
                    && ReadForwardGuard(guard, bounds) is { } tests
                    && FlattenForwardConjunction(tests).SequenceEqual(
                        Enumerable.Range(0, bounds.Length).Select(index => new ForwardGuard.Test(
                            lastList && index == bounds.Length - 1 ? IsList : IsInteger,
                            index
                        ))
                    );
        }

        private static (bool Tuple, bool Mapped, string[] Guards, string[] Forwarded, string[] Bound)? ReadWxIndexExtras(
            Clause clause,
            bool includeMappedTuple = false
        )
        {
            if (clause.Patterns.Count == 2)
            {
                return (false, false, [], [], []);
            }

            if (includeMappedTuple && clause.Patterns is [_, _, AliasPattern pairWithOptions, Pattern.Variable optionsTail]
                && TryReadPatternAlias(pairWithOptions, out var whole, out var fields)
                && fields is Pattern.Tuple { Items: [Pattern.Variable row, Pattern.Variable column] }
                && DistinctBindings(
                    whole,
                    row,
                    column,
                    optionsTail
                ))
            {
                return (true, true, [row.Name, column.Name, optionsTail.Name], [whole.Name], [whole.Name, row.Name, column.Name]);
            }

            if (clause.Patterns.Count == 4 && clause.Patterns[2] is Pattern.Variable first
                && clause.Patterns[3] is Pattern.Variable second && DistinctBindings(first, second))
            {
                string[] dimensions = [first.Name, second.Name];

                return (false, false, dimensions, dimensions, dimensions);
            }

            if (clause.Patterns.Count == 3 && clause.Patterns[2] is Pattern.Variable options
                && options.Name != VariableScopeNames.Wildcard)
            {
                return (false, true, [options.Name], [], []);
            }

            if (clause.Patterns.Count == 3 && clause.Patterns[2] is AliasPattern pair
                && TryReadPatternAlias(pair, out var shape, out var tuple)
                && tuple is Pattern.Tuple { Items: [Pattern.Variable width, Pattern.Variable height] }
                && DistinctBindings(shape, width, height))
            {
                return (true, false, [width.Name, height.Name], [shape.Name], [shape.Name, width.Name, height.Name]);
            }

            return null;
        }

        private bool TryEmitWxClassUnionCommand(
            FunctionDefinition function,
            bool validateOnly = false,
            int? functionFailure = null,
            bool unaryFallback = false,
            (int Receiver, int Value)? preparedClasses = null,
            bool unaryTagValidated = false,
            int? preparedReceiver = null,
            bool checkedMappedTuple = false
        )
        {
            int arity = function.Arity;
            bool hasReceiver = arity >= 2;
            bool indexedOverload = preparedReceiver is not null || checkedMappedTuple;
            if (arity < 1 || arity > (indexedOverload ? 4 : 3) || unaryFallback && hasReceiver
                || preparedClasses is not null && arity != 2 || indexedOverload && arity < 2
                || function.Clauses is not [{ Body: Expr.Sequence sequence } clause] || sequence.Items.Count < 4)
            {
                return false;
            }

            Pattern.Variable? extra = null;
            bool mapped = false;
            var indexed = indexedOverload ? ReadWxIndexExtras(clause, includeMappedTuple: checkedMappedTuple) : null;
            if (indexedOverload && indexed is null)
            {
                return false;
            }

            if (checkedMappedTuple)
            {
                if (indexed is not { Tuple: true, Mapped: true } || clause.Patterns[^1] is not Pattern.Variable options)
                {
                    return false;
                }

                extra = options;
                mapped = true;
            }
            else if ((!indexedOverload || indexed is { Mapped: true }) && arity == 3)
            {
                if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                    || clause.Guard is not Expr.Call
                    {
                        Function: "is_integer" or "is_list", Arguments: [Expr.Variable guarded]
                    } guard || !owner.ResolvesErlangBif(guard))
                {
                    return false;
                }

                mapped = guard.Function == "is_list";
                if (clause.Patterns[mapped ? 2 : 1] is not Pattern.Variable argument || argument.Name != guarded.Name)
                {
                    return false;
                }

                extra = argument;
            }
            else if (!indexedOverload && clause.Guard is not null)
            {
                return false;
            }

            var receive = sequence.Items[^1] as Expr.Call;
            bool readsResult = receive is
            {
                Module: "wxe_util", Function: "rec", DynamicRemote: false,
                Arguments: [Expr.Literal { Value: Integer }]
            };
            int queueIndex = sequence.Items.Count - (readsResult ? 2 : 1);
            int valueRegister = mapped || indexedOverload ? 1 : arity - 1;
            if (sequence.Items[queueIndex] is not Expr.Call
                {
                    Module: "wxe_util", Function: "queue_cmd", DynamicRemote: false,
                } queue || queue.Arguments.Count != arity + 2
                || extra is not null && !readsResult
                || queue.Arguments[valueRegister] is not Expr.Call
                {
                    Module: "wx", Function: "typeCast", DynamicRemote: false,
                    Arguments: [Expr.Variable input, Expr.Variable castType]
                } cast
                || queue.Arguments[arity] is not Expr.Case environment
                || queue.Arguments[arity + 1] is not Expr.Literal { Value: Integer command }
                || readsResult && receive!.Arguments[0] is Expr.Literal { Value: Integer received } && !command.Equals(received)
                || ReadWxClassUnionObject(clause.Patterns[valueRegister]) is not { } valueObject
                || input.Name != valueObject.Object
                || !IsWxEnvironment(environment)
                || queueIndex < (mapped ? 3 : 1)
                || sequence.Items[queueIndex - (mapped ? 3 : 1)] is not Expr.Match
                {
                    Pattern: Pattern.Variable selected, Value: Expr.If
                    {
                        Clauses: [..,
                        {
                            Patterns: [], Guard: Expr.Literal { Value: Atom { Name: "true" } },
                            Body: Expr.Call
                            {
                                Function: "error", DynamicRemote: false, Arguments:
                                [Expr.Tuple { Items: [Expr.Literal { Value: Atom { Name: "badarg" } }, Expr.Variable badType] }]
                            } error
                        }]
                    } decision
                }
                || castType.Name != selected.Name || badType.Name != valueObject.Type
                || !owner.ResolvesErlangBif(error))
            {
                return false;
            }

            int prefix = hasReceiver ? 1 : 0;
            int classCount = decision.Clauses.Count - 1;
            if (classCount < 2 || extra is not null && classCount != 2
                || unaryTagValidated && (hasReceiver || classCount != 2)
                || queueIndex != prefix + classCount + (mapped ? 3 : 1))
            {
                return false;
            }

            Expr.Fun? mapper = null;
            Expr.Call? map = null;
            var bindings = new List<string> { valueObject.Object, valueObject.Type, selected.Name };
            if (indexed is { } layout)
            {
                if (!queue.Arguments.Skip(2).Take(layout.Forwarded.Length).OfType<Expr.Variable>()
                    .Select(variable => variable.Name).SequenceEqual(layout.Forwarded))
                {
                    return false;
                }

                bindings.AddRange(layout.Bound);
            }

            if (extra is not null)
            {
                bindings.Add(extra.Name);
                if (mapped)
                {
                    if (sequence.Items[queueIndex - 2] is not
                        Expr.Match { Pattern: Pattern.Variable mapperBinding, Value: Expr.Fun { SelfName: null } lambda }
                        || sequence.Items[queueIndex - 1] is not Expr.Match
                        {
                            Pattern: Pattern.Variable resultBinding,
                            Value: Expr.Call
                            {
                                Module: "lists", Function: "map", DynamicRemote: false,
                                Arguments: [Expr.Variable mapperArgument, Expr.Variable mapInput]
                            } mapCall
                        }
                        || queue.Arguments[arity - 1] is not Expr.Variable mappedArgument
                        || mapperArgument.Name != mapperBinding.Name || mapInput.Name != extra.Name
                        || mappedArgument.Name != resultBinding.Name || !ReadWxOptions(lambda.Clauses, out _, out _))
                    {
                        return false;
                    }

                    mapper = lambda;
                    map = mapCall;
                    bindings.Add(mapperBinding.Name);
                    bindings.Add(resultBinding.Name);
                }
                else if (queue.Arguments[1] is not Expr.Variable index || index.Name != extra.Name)
                {
                    return false;
                }
            }

            (Atom Class, Expr.Call Call)? receiverCheck = null;
            if (hasReceiver)
            {
                if (ReadWxClassUnionObject(clause.Patterns[0]) is not { } receiverObject
                    || queue.Arguments[0] is not Expr.Variable target || target.Name != receiverObject.Object
                    || ReadWxClassTest(sequence.Items[0], receiverObject.Type) is not { } check)
                {
                    return false;
                }

                bindings.Add(receiverObject.Object);
                bindings.Add(receiverObject.Type);
                receiverCheck = check;
            }

            var classes = new (Atom Class, Expr.Call Call)[classCount];
            for (int index = 0; index < classCount; index++)
            {
                if (ReadProtectedWxClassTest(sequence.Items[prefix + index], valueObject.Type) is not { } test
                    || decision.Clauses[index] is not
                    {
                        Patterns: [], Guard: Expr.Variable condition,
                        Body: Expr.Literal { Value: Atom selectedClass }
                    }
                    || condition.Name != test.Result || !selectedClass.Equals(test.Class))
                {
                    return false;
                }

                bindings.Add(test.Result);
                classes[index] = (test.Class, test.Call);
            }

            if (bindings.Contains(VariableScopeNames.Wildcard)
                || bindings.Distinct(StringComparer.Ordinal).Count() != bindings.Count
                || !UsesFreshWxEnvironment(environment, bindings)
                || mapper is not null && ClosureCaptures.Capture(
                    mapper,
                    bindings.ToDictionary(name => name, _ => (Term)Nil.Value)).Count != 0)
            {
                return false;
            }

            if (validateOnly)
            {
                return true;
            }

            // Each class check uses four labels. Selection uses one per class plus two error labels.
            int failure = unaryFallback ? owner.nextLabel + 5 * classCount + 2 : functionFailure ?? owner.nextLabel - 2;
            int typeSlot = classCount - 1;
            int token = classCount + arity;
            int frame = token + 1;
            int receiverSlot = token - 1;
            int valueSlot = receiverSlot - valueRegister;
            for (int register = 0; !checkedMappedTuple && !unaryTagValidated && preparedClasses is null
                && preparedReceiver is null && register < arity; register++)
            {
                if (register != 0 && register != valueRegister)
                {
                    continue;
                }

                EmitDetailRecordTest(
                    register,
                    4,
                    Term.A("wx_ref"),
                    failure
                );
            }

            if (extra is not null && !indexedOverload)
            {
                owner.Op(mapped ? IsList : IsInteger, LabelTag, failure);
                owner.Operand(XRegisterTag, mapped ? 2 : 1);
            }

            int live = preparedClasses is { } prepared ? Math.Max(prepared.Receiver, prepared.Value) + 1
                : preparedReceiver is int receiver ? receiver + 1 : arity;
            EmitFrameAllocation(live, frame);
            BeamSourceLocation previous = location;
            if (receiverCheck is { } receiverClass)
            {
                owner.instructions.WriteByte(InitYRegisters);
                owner.instructions.WriteByte(ExtendedListOperand);
                int init = preparedClasses is null ? classCount : classCount - 1;
                owner.Operand(BeamWire.UnsignedTag, init + 1);
                for (int slot = 0; slot < init; slot++)
                {
                    owner.Operand(YRegisterTag, slot);
                }

                owner.Operand(YRegisterTag, token);
                if (preparedClasses is { } classesInRegisters)
                {
                    owner.MoveRegister(
                        XRegisterTag,
                        classesInRegisters.Value,
                        YRegisterTag,
                        typeSlot
                    );
                }

                for (int register = arity - 1; register >= 0; register--)
                {
                    owner.MoveRegister(
                        XRegisterTag,
                        register,
                        YRegisterTag,
                        receiverSlot - register
                    );
                }
                if (preparedClasses is null && preparedReceiver is null)
                {
                    owner.EmitTuplePart(0, 2, 0);
                }

                int receiverType = preparedClasses?.Receiver ?? preparedReceiver ?? 0;
                int checkedReceiver = Label();
                owner.Op(ExactNotEqualTest, LabelTag, checkedReceiver);
                owner.Operand(XRegisterTag, receiverType);
                owner.LiteralOperand(receiverClass.Class);
                ApplyClass(
                    XRegisterTag,
                    receiverType,
                    receiverClass.Class,
                    receiverClass.Call
                );
                Mark(checkedReceiver);
            }
            else
            {
                EmitFrameInitialization(0, typeSlot);
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    YRegisterTag,
                    valueSlot
                );
            }

            if (preparedClasses is null)
            {
                owner.Op(GetTupleElement, hasReceiver ? YRegisterTag : XRegisterTag, hasReceiver ? valueSlot : 0);
                owner.Operand(BeamWire.UnsignedTag, 2);
                owner.Operand(YRegisterTag, typeSlot);
            }
            for (int index = 0; index < classCount; index++)
            {
                var (expected, call) = classes[index];
                EmitProtectedWxClass(
                    token,
                    YRegisterTag,
                    typeSlot,
                    typeSlot - index - 1,
                    expected,
                    call,
                    ref previous
                );
            }

            int[] nextClass = classes.SkipLast(1).Select(_ => Label()).ToArray();
            int chosen = Label();
            int missingEnvironment = Label();
            int badArgument = Label();
            for (int index = 0; index < classCount; index++)
            {
                int slot = typeSlot - index - 1;
                bool last = slot < 0;
                owner.Op(ExactEqualTest, LabelTag, last ? badArgument : nextClass[index]);
                owner.Operand(last ? XRegisterTag : YRegisterTag, last ? 0 : slot);
                owner.LiteralOperand(Term.A("true"));
                owner.EmitConstant(classes[index].Class, mapped ? token : 1, mapped ? YRegisterTag : XRegisterTag);
                if (!last)
                {
                    JumpTo(chosen);
                    Mark(nextClass[index]);
                }
            }

            Mark(chosen);
            bool relocatesDimensions = indexedOverload && arity > 2 && !mapped;
            int retained = relocatesDimensions || checkedMappedTuple ? arity - 1 : extra is null ? prefix : 2;
            int removed = frame - retained;
            int[] dimensionSlots = relocatesDimensions ? Enumerable.Range(2, arity - 2)
                .Select(register => receiverSlot - register).ToArray() : [];
            int preparedReceiverSlot = 0;
            if (mapper is not null)
            {
                owner.EmitClosureAllocation(0, 0, 1);
                EmitWxOptionMapper(mapper, 0);
                owner.MoveRegister(
                    YRegisterTag,
                    receiverSlot - (arity - 1),
                    XRegisterTag,
                    1
                );
                int removedBeforeMap = frame - arity;
                Trim(removedBeforeMap, arity);
                owner.EmitCallLine(map!, previous);
                previous = CallLocation(map!, previous);
                owner.Op(ExternalCall, BeamWire.UnsignedTag, 2);
                owner.Operand(BeamWire.UnsignedTag, owner.Import("lists", "map", 2));
                owner.MoveRegister(
                    YRegisterTag,
                    valueSlot - removedBeforeMap,
                    XRegisterTag,
                    2
                );
                owner.MoveRegister(
                    YRegisterTag,
                    arity - 1,
                    XRegisterTag,
                    1
                );
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    YRegisterTag,
                    arity - 1
                );
                if (checkedMappedTuple)
                {
                    owner.MoveRegister(
                        YRegisterTag,
                        0,
                        YRegisterTag,
                        1
                    );
                    preparedReceiverSlot = 1;
                }

                Trim(1, retained);
                owner.MoveRegister(
                    XRegisterTag,
                    2,
                    XRegisterTag,
                    0
                );
            }
            else
            {
                owner.MoveRegister(
                    YRegisterTag,
                    valueSlot,
                    XRegisterTag,
                    0
                );
                if (relocatesDimensions)
                {
                    int[] savedSlots = [receiverSlot, .. dimensionSlots];
                    var available = new Queue<int>(Enumerable.Range(removed, retained).Reverse()
                        .Where(slot => !savedSlots.Contains(slot)));
                    for (int index = dimensionSlots.Length - 1; index >= 0; index--)
                    {
                        int from = dimensionSlots[index];
                        if (from < removed)
                        {
                            int to = available.Dequeue();
                            owner.MoveRegister(
                                YRegisterTag,
                                from,
                                YRegisterTag,
                                to
                            );
                            dimensionSlots[index] = to;
                        }

                        dimensionSlots[index] -= removed;
                    }

                    preparedReceiverSlot = receiverSlot - removed;
                }
                else if (hasReceiver)
                {
                    owner.MoveRegister(
                        YRegisterTag,
                        extra is null ? receiverSlot : receiverSlot - 1,
                        YRegisterTag,
                        token
                    );
                }

                Trim(frame - retained, retained);
            }
            owner.EmitCallLine(cast, previous);
            owner.Op(ExternalCall, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, owner.Import("wx", "typeCast", 2));
            EmitWxEnvironmentCheck(1, missingEnvironment);
            owner.EmitTuplePart(1, 1, arity);
            if (relocatesDimensions)
            {
                for (int index = dimensionSlots.Length - 1; index >= 0; index--)
                {
                    owner.MoveRegister(
                        YRegisterTag,
                        dimensionSlots[index],
                        XRegisterTag,
                        index + 2
                    );
                }
            }

            if (mapped)
            {
                if (checkedMappedTuple)
                {
                    owner.MoveRegister(
                        YRegisterTag,
                        0,
                        XRegisterTag,
                        2
                    );
                }

                owner.MoveRegister(
                    YRegisterTag,
                    arity - 2,
                    XRegisterTag,
                    arity - 1
                );
            }

            if (hasReceiver)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    XRegisterTag,
                    valueRegister
                );
            }

            if (extra is not null && !mapped)
            {
                owner.MoveRegister(
                    YRegisterTag,
                    1,
                    XRegisterTag,
                    1
                );
            }

            owner.EmitConstant(command, arity + 1);
            if (hasReceiver)
            {
                owner.MoveRegister(
                    YRegisterTag,
                    preparedReceiverSlot,
                    XRegisterTag,
                    0
                );
                if (readsResult)
                {
                    Trim(retained, 0);
                }
            }

            owner.EmitCallLine(queue, CallLocation(cast, previous));
            owner.Op(readsResult ? ExternalCall : TailExternalCallWithFrame, BeamWire.UnsignedTag, arity + 2);
            owner.Operand(BeamWire.UnsignedTag, owner.Import("wxe_util", "queue_cmd", arity + 2));
            if (readsResult)
            {
                owner.EmitConstant(command, 0);
                owner.EmitCallLine(receive!, CallLocation(queue, previous));
                TailCall("wxe_util", "rec", 0);
            }
            else
            {
                owner.Operand(BeamWire.UnsignedTag, prefix);
            }

            Mark(missingEnvironment);
            owner.EmitConstant(Term.Tuple(Term.A("wx"), Term.A("unknown_env")), 0);
            if (readsResult)
            {
                owner.EmitCallLine(queue, CallLocation(receive!, previous));
            }
            TailCall(ErlangModuleNames.Module, "error", frame);
            Mark(badArgument);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.Op(PutTuple, XRegisterTag, 0);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.LiteralOperand(Term.A("badarg"));
            owner.Operand(YRegisterTag, typeSlot);
            owner.EmitCallLine(error, CallLocation(queue, previous));
            TailCall(ErlangModuleNames.Module, "error", frame);

            return true;

            void ApplyClass(
                int tag,
                int register,
                Atom expected,
                Expr.Call call
            )
            {
                owner.MoveRegister(
                    tag,
                    register,
                    XRegisterTag,
                    1
                );
                owner.EmitConstant(Term.A("parent_class"), 2);
                owner.EmitConstant(expected, 0);
                owner.EmitCallLine(call, previous);
                previous = CallLocation(call, previous);
                owner.Op(DynamicApply, BeamWire.UnsignedTag, 1);
            }

            void Trim(int count, int remaining)
            {
                owner.Op(TrimFrame, BeamWire.UnsignedTag, count);
                owner.Operand(BeamWire.UnsignedTag, remaining);
            }

            void TailCall(string module, string name, int remaining)
            {
                owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 1);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(module, name, 1));
                owner.Operand(BeamWire.UnsignedTag, remaining);
            }
        }

        private void EmitProtectedWxClass(
            int token,
            int sourceTag,
            int source,
            int resultSlot,
            Atom expected,
            Expr.Call call,
            ref BeamSourceLocation previous
        )
        {
            int invoke = Label();
            int success = Label();
            int handler = Label();
            int done = Label();
            owner.Op(TryEnter, YRegisterTag, token);
            owner.Operand(LabelTag, handler);
            owner.Op(ExactEqualTest, LabelTag, invoke);
            owner.Operand(sourceTag, source);
            owner.LiteralOperand(expected);
            owner.EmitConstant(Term.A("true"), 0);
            JumpTo(success);
            Mark(invoke);
            owner.MoveRegister(
                sourceTag,
                source,
                XRegisterTag,
                1
            );
            owner.EmitConstant(Term.A("parent_class"), 2);
            owner.EmitConstant(expected, 0);
            owner.EmitCallLine(call, previous);
            previous = CallLocation(call, previous);
            owner.Op(DynamicApply, BeamWire.UnsignedTag, 1);
            Mark(success);
            owner.Op(TryEnd, YRegisterTag, token);
            if (resultSlot >= 0)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    YRegisterTag,
                    resultSlot
                );
            }

            JumpTo(done);
            Mark(handler);
            owner.Op(TryCase, YRegisterTag, token);
            owner.EmitConstant(Term.A("false"), Math.Max(0, resultSlot), resultSlot < 0 ? XRegisterTag : YRegisterTag);
            Mark(done);
        }

        private static (string Object, string Type)? ReadWxClassUnionObject(Pattern pattern) =>
            pattern is AliasPattern alias
            && (alias.Left as Pattern.Variable ?? alias.Right as Pattern.Variable) is { } binding
            && (alias.Left is Pattern.Variable ? alias.Right : alias.Left) is Pattern.Tuple
            {
                Items: [Pattern.Literal { Value: Atom { Name: "wx_ref" } },
                Pattern.Variable { Name: VariableScopeNames.Wildcard }, Pattern.Variable type,
                Pattern.Variable { Name: VariableScopeNames.Wildcard }]
            }
                ? (binding.Name, type.Name)
                : null;

        private static (Atom Class, Expr.Call Call)? ReadWxClassTest(Expr expression, string type) =>
            expression is Expr.Binary
            {
                Operator: ErlangOperators.OrElse,
                Left: Expr.Binary
                {
                    Operator: ErlangOperators.ExactEqual,
                    Left: Expr.Variable tested, Right: Expr.Literal { Value: Atom expected }
                },
                Right: Expr.Call
                {
                    DynamicRemote: true, Arguments: [Expr.Variable dispatched,
                    Expr.Literal { Value: Atom { Name: "parent_class" } },
                    Expr.List { Items: [Expr.Literal { Value: Atom passed }], Tail: null }]
                } call
            }
            && tested.Name == type && dispatched.Name == type && expected.Equals(passed)
                ? (expected, call)
                : null;

        private static (string Result, Atom Class, Expr.Call Call)? ReadProtectedWxClassTest(Expr expression, string type) =>
            expression is Expr.Match
            {
                Pattern: Pattern.Variable result, Value: Expr.Try
                {
                    Body: var body, After: null, Clauses: [], Catches: [
                        {
                            Guard: null, Patterns:
                    [Pattern.Tuple
                    {
                        Items: [Pattern.Variable { Name: VariableScopeNames.Wildcard },
                        Pattern.Variable { Name: VariableScopeNames.Wildcard }, Pattern.Variable { Name: VariableScopeNames.Wildcard }]
                    }],
                            Body: Expr.Literal { Value: Atom { Name: "false" } }
                        }]
                }
            }
            && ReadWxClassTest(body, type) is { } check ? (result.Name, check.Class, check.Call) : null;
    }
}
