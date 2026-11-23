using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitStructuralListEntry(
            FunctionDefinition function,
            int functionInfo,
            bool requireGroupedHead = false
        )
        {
            if (function.Clauses.Count < 2 || function.Arity + 2 >= FirstReservedXRegister)
            {
                return false;
            }

            for (int input = 0; input < function.Arity; input++)
            {
                int leadingNil = function.Clauses.TakeWhile(clause => ListEntryTest(clause.Patterns[input]) == IsNil).Count();
                var prefix = function.Clauses.Skip(leadingNil).TakeWhile(clause =>
                    clause.Patterns[input] is Pattern.List { Items.Count: 1 }).ToArray();
                bool flatListGenerator = !owner.ModuleOptimizationDisabled && IsFlatListGeneratorEntry(function, input);
                if (!flatListGenerator && !prefix.Any(clause => EntryShape(((Pattern.List)clause.Patterns[input]).Items[0]) is Pattern.Tuple
                    || ReadListHeadRecordGuard(clause, input) is not null))
                {
                    continue;
                }

                // Empty and nonempty inputs are disjoint. All other clauses keep their relative order.
                Clause[] ordered = [.. prefix, .. function.Clauses.Take(leadingNil),
                    .. function.Clauses.Skip(leadingNil + prefix.Length)];
                if (TryEmitStructuralListBranches(
                    function,
                    input,
                    ordered,
                    prefix.Length,
                    functionInfo,
                    flatListGenerator,
                    requireGroupedHead
                ))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsFlatListGeneratorEntry(FunctionDefinition function, int listInput)
        {
            if (listInput != 0 || function.Clauses is not
                [
                {
                    Guard: null,
                    Body: Expr.List { Items.Count: 1, Tail: Expr.Call accept },
                    Patterns: [Pattern.List
                    {
                        Items: [Pattern.List { Items.Count: > 0, Tail: null } elements],
                        Tail: Pattern.Variable acceptedTail
                    }, ..]
                } first,
                {
                    Guard: null,
                    Body: Expr.Call skip,
                    Patterns: [Pattern.List
                    {
                        Items: [Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                        Tail: Pattern.Variable skippedTail
                    }, ..]
                } second,
                { Guard: null, Body: Expr.Literal { Value: Nil } } empty,
                {
                    Guard: null,
                    Patterns: [Pattern.Variable { Name: not VariableScopeNames.Wildcard } invalid, ..],
                    Body: Expr.Call
                    {
                        Module: ErlangModuleNames.Module,
                        Function: ErlangModuleNames.Error,
                        DynamicRemote: false,
                        Arguments:
                            [
                                Expr.Tuple
                            {
                                Items: [Expr.Literal { Value: Atom { Name: ErlangErrorReasons.BadGenerator } },
                                    Expr.Variable rejected]
                            }
                            ]
                    }
                } failure
                ]
                || acceptedTail.Name != skippedTail.Name || rejected.Name != invalid.Name
                || ListEntryTest(empty.Patterns[0]) != IsNil
                || !empty.Patterns.Skip(1).Concat(failure.Patterns.Skip(1)).All(IsWildcard))
            {
                return false;
            }

            Pattern[] captures = first.Patterns.Skip(1).ToArray();
            if (captures.Concat(elements.Items)
                    .Any(pattern => pattern is not Pattern.Variable { Name: not VariableScopeNames.Wildcard })
                || !second.Patterns.Skip(1).SequenceEqual(captures))
            {
                return false;
            }

            string[] arguments = [acceptedTail.Name, .. captures.Cast<Pattern.Variable>().Select(capture => capture.Name)];
            var localBindings = arguments.Concat(elements.Items.Cast<Pattern.Variable>().Select(element => element.Name));
            if (arguments.Contains(VariableScopeNames.Wildcard)
                || localBindings.Distinct(StringComparer.Ordinal).Count() != arguments.Length + elements.Items.Count)
            {
                return false;
            }

            return IsRetry(accept) && IsRetry(skip);

            bool IsRetry(Expr.Call call) =>
                call is { Module: null, DynamicRemote: false } && call.Function == function.Name
                && call.Arguments.Count == arguments.Length
                && call.Arguments.Select((value, index) =>
                    value is Expr.Variable variable && variable.Name == arguments[index]).All(matches => matches);

            static bool IsWildcard(Pattern pattern) => pattern is Pattern.Variable { Name: VariableScopeNames.Wildcard };
        }

        private (Atom Name, int Arity)? ReadListHeadRecordGuard(Clause clause, int input)
        {
            if (!owner.ModuleOptimizationDisabled && clause.Patterns[input] is Erlang.Pattern.List
                {
                    Items: [Erlang.Pattern.Variable { Name: not VariableScopeNames.Wildcard } head],
                    Tail: Erlang.Pattern.Variable
                } && clause.Guard is { } guard
                && owner.ReadRecordTest(guard) is { Value: Expr.Variable operand } record
                && head.Name == operand.Name)
            {
                return (record.Name, record.Arity);
            }

            return null;
        }

        private bool TryEmitStructuralListBranches(
            FunctionDefinition function,
            int listInput,
            Clause[] clauses,
            int consCount,
            int functionInfo,
            bool flatListGenerator,
            bool requireGroupedHead
        )
        {
            int arity = function.Arity;
            var emptyReturn = ReadEntryListReturn(clauses);
            if (consCount != 1 || clauses.Length != 2 || emptyReturn?.Input != listInput
                || !HasProperListEntry(function, listInput))
            {
                emptyReturn = null;
            }

            // The empty-list branch is reached before extraction. A catch-all cons
            // clause covers all failures of the earlier head tests.
            var lastCons = clauses[consCount - 1];
            var lastPattern = (Pattern.List)lastCons.Patterns[listInput];
            bool reuseTail = lastCons.Guard is null
                && lastPattern is { Items: [Erlang.Pattern.Variable], Tail: Erlang.Pattern.Variable rest }
                && HasUnconditionalParameters(lastCons with
                {
                    Patterns = [.. lastCons.Patterns.Take(listInput), lastPattern.Items[0], rest,
                        .. lastCons.Patterns.Skip(listInput + 1)]
                })
                && (flatListGenerator || clauses.Skip(consCount).All(clause => ListEntryTest(clause.Patterns[listInput]) == IsNil));
            int head = arity;
            int tail = reuseTail ? listInput : arity + 1;
            bool needsTail = clauses.Take(consCount).Any(clause => !IsDiscarded(((Pattern.List)clause.Patterns[listInput]).Tail));
            int firstConsScratch = arity + (needsTail && !reuseTail ? 2 : 1);
            var unusedFieldsByBranch = clauses.Select(ReadStructuralEntryUnusedFields).ToArray();
            var consHeadGroups = requireGroupedHead ? ReadConsHeadTupleGroups(
                clauses,
                listInput,
                consCount,
                reuseTail,
                unusedFieldsByBranch
            ) : null;
            if (requireGroupedHead && consHeadGroups is null)
            {
                return false;
            }

            var consHeadBranches = consHeadGroups?.SelectMany(group => group.Rows.Select(row => (group, row)))
                .ToDictionary(entry => entry.row.Branch);
            var arityGroups = clauses.Take(consCount).Select((clause, index) => (Clause: clause, Shape: ReadTaggedHead(index)))
                .TakeWhile(entry => entry.Shape.HasValue)
                .GroupBy(entry => entry.Shape!.Value.Arity)
                .OrderByDescending(group => group.Key)
                .ToArray();
            int arityDispatchEnd = 0;
            if (consHeadGroups is null && reuseTail && arityGroups.Length > 1
                && arityGroups.All(group => group.All(entry => entry.Shape!.Value.Tag.Equals(group.First().Shape!.Value.Tag))))
            {
                var sorted = arityGroups.SelectMany(group => group.Select(entry => entry.Clause)).ToArray();
                arityDispatchEnd = sorted.Length;
                clauses = [.. sorted, .. clauses.Skip(arityDispatchEnd)];
                unusedFieldsByBranch = clauses.Select(ReadStructuralEntryUnusedFields).ToArray();
            }

            var sharedShapes = new Dictionary<int, (int End, int Arity, Atom Tag, bool SharesDiscriminator, Term[]? Keys, int Payload)>();
            var checkedHeads = new Dictionary<int, bool>();
            var selectedHeads = new HashSet<int>();
            var sharedPayloads = new Dictionary<int, int>();
            for (int start = consHeadBranches?.Count ?? 0; start < consCount;)
            {
                if (ReadTaggedHead(start) is not { } shape)
                {
                    start++;
                    continue;
                }

                int end = start + 1;
                while (end < consCount && ReadTaggedHead(end) is { } next
                    && next.Arity == shape.Arity && next.Tag.Equals(shape.Tag))
                {
                    end++;
                }

                if (end - start > 1 || start < arityDispatchEnd)
                {
                    bool sharesDiscriminator = shape.Arity > 1 && Enumerable.Range(start, end - start)
                        .All(index => ((Erlang.Pattern.Tuple)((Pattern.List)clauses[index].Patterns[listInput]).Items[0])
                            .Items[1] is Erlang.Pattern.Literal);
                    Term[]? keys = null;
                    if (sharesDiscriminator)
                    {
                        var literals = Enumerable.Range(start, end - start)
                            .Select(index => ((Erlang.Pattern.Literal)((Erlang.Pattern.Tuple)
                                ((Pattern.List)clauses[index].Patterns[listInput]).Items[0]).Items[1]).Value).ToArray();
                        if ((literals.All(key => key is Atom) || literals.All(key => key is Integer))
                            && literals.Distinct().Count() == literals.Length)
                        {
                            keys = literals;
                            selectedHeads.UnionWith(Enumerable.Range(start, end - start));
                        }
                    }

                    int payload = -1;
                    if (keys is not null && shape.Arity >= 3
                        && Enumerable.Range(start, end - start).All(index =>
                            !IsDiscarded(((Erlang.Pattern.Tuple)((Pattern.List)clauses[index].Patterns[listInput]).Items[0]).Items[2])))
                    {
                        // A failed selection can reach later clauses, which may still need the head.
                        bool headDead = shape.Arity == 3 && clauses.Skip(end).Take(consCount - end)
                            .All(later => IsDiscarded(((Pattern.List)later.Patterns[listInput]).Items[0]));
                        payload = reuseTail && headDead ? head : firstConsScratch + 1;
                        if (payload >= FirstReservedXRegister)
                        {
                            return false;
                        }
                    }

                    sharedShapes.Add(start, (end, shape.Arity, shape.Tag, sharesDiscriminator, keys, payload));
                    for (int index = start; index < end; index++)
                    {
                        checkedHeads.Add(index, sharesDiscriminator);
                        if (payload >= 0)
                        {
                            sharedPayloads.Add(index, payload);
                        }
                    }
                }

                start = end;
            }

            (int Arity, Atom Tag)? ReadTaggedHead(int branch)
            {
                var list = (Pattern.List)clauses[branch].Patterns[listInput];

                return list.Items[0] is Erlang.Pattern.Tuple tuple && HasTupleTag(tuple, out Atom tag)
                    ? (tuple.Items.Count, tag) : null;
            }

            var branches = new List<(Clause Clause, Action<int>[] Tests, (Pattern.Variable Variable, int Register)[] Used,
                EntryCall? Tail, int ResultRegister, Term? Constant, Dictionary<string, int> CaptureOrder)>();

            for (int branch = 0; branch < clauses.Length; branch++)
            {
                Clause clause = clauses[branch];
                var tests = new List<Action<int>>();
                var bindings = new Dictionary<string, int>(StringComparer.Ordinal);
                var reusedBindingPositions = new Dictionary<string, int>(StringComparer.Ordinal);
                var unusedFields = unusedFieldsByBranch[branch];
                var groupedHead = consHeadBranches is not null && consHeadBranches.TryGetValue(branch, out var selectedHead)
                    ? selectedHead
                    : default;

                int nextRegister = branch < consCount ? firstConsScratch : arity;
                if (groupedHead.group is { } sharedHead)
                {
                    nextRegister += sharedHead.Fields.Length;
                }

                bool shapeChecked = checkedHeads.TryGetValue(branch, out bool sharesDiscriminator);
                int discriminator = sharesDiscriminator ? nextRegister : -1;
                int payload = sharedPayloads.GetValueOrDefault(branch, -1);
                if (payload >= nextRegister)
                {
                    nextRegister = payload + 1;
                }

                if (sharesDiscriminator && !selectedHeads.Contains(branch))
                {
                    nextRegister++;
                }
                if (nextRegister > FirstReservedXRegister)
                {
                    return false;
                }

                int firstScratchRegister = nextRegister;
                bool canOverwriteHead = reuseTail && branch < consCount
                    && clauses.Skip(branch + 1).Take(consCount - branch - 1)
                        .All(later => IsDiscarded(((Pattern.List)later.Patterns[listInput]).Items[0]));

                for (int index = 0; index < arity; index++)
                {
                    if (branch < consCount && index == listInput)
                    {
                        var list = (Pattern.List)clause.Patterns[index];
                        bool matchedHead = groupedHead.group is { } group
                            ? ReadGroupedHead(group, groupedHead.row)
                            : ReadPattern(list.Items[0], head, checkedHead: shapeChecked);
                        if (!matchedHead || !ReadPattern(list.Tail ?? new Pattern.List([], null), tail))
                        {
                            return false;
                        }
                    }
                    else if (!ReadPattern(clause.Patterns[index], index))
                    {
                        return false;
                    }
                }

                var parameters = Enumerable.Repeat(VariableScopeNames.Wildcard, nextRegister).ToArray();
                foreach (var (name, register) in bindings)
                {
                    if (parameters[register] != VariableScopeNames.Wildcard)
                    {
                        // A register can represent multiple names in a nested alias. Leave those aliases to the general emitter.
                        return false;
                    }

                    parameters[register] = name;
                }

                var headRecord = branch < consCount ? ReadListHeadRecordGuard(clause, listInput) : null;
                if (headRecord is { } record)
                {
                    // erl_expand_records incorporates the required guard into the cons head.
                    tests.Add(failure => EmitDetailRecordTest(
                        head,
                        record.Arity,
                        record.Name,
                        failure
                    ));
                }

                Clause guardClause = clause with
                {
                    Patterns = parameters.Select(name => (Pattern)new Pattern.Variable(name)).ToArray(),
                    Guard = headRecord is null ? clause.Guard : null
                };
                if (!CanEmitSeparateEntryGuard(guardClause, guardClause.Guard))
                {
                    return false;
                }

                var used = ReadUsedEntryParameters(guardClause);
                var bodyInputs = bindings.Where(binding => used is null || used.Contains(binding.Key))
                    .Select(binding => (new Pattern.Variable(binding.Key), binding.Value)).ToArray();
                EntryCall? tailCall = clause.Body is Expr.Call call ? ReadEntryCall(call, parameters, allowControlTransfer: true) : null;
                int resultRegister = clause.Body is Expr.Variable returned ? bindings.GetValueOrDefault(returned.Name, -1) : -1;
                Term? constant = AttributeLiteral.TryNormalizeConstant(clause.Body, out Term value) ? value : null;
                // Keep the binding's place in the capture order when a projection uses
                // an older register instead of consuming the next scratch register.
                bool reusedListTail = reuseTail && branch < consCount;
                var captureOrder = bodyInputs.OrderBy(binding =>
                {
                    string name = binding.Item1.Name;
                    int register = binding.Value;
                    bool reused = reusedBindingPositions.TryGetValue(name, out int position);
                    if (reused)
                    {
                        register = position;
                    }

                    int order = reusedListTail && register == tail ? arity + 1
                        : register + (reusedListTail && register > head ? 1 : 0);

                    return (order, reused ? 0 : 1);
                }).Select((binding, order) => (binding.Item1.Name, order))
                    .ToDictionary(binding => binding.Name, binding => binding.order, StringComparer.Ordinal);
                branches.Add((guardClause, tests.ToArray(), bodyInputs, tailCall, resultRegister, constant, captureOrder));

                bool ReadPattern(
                    Pattern pattern,
                    int register,
                    bool preserveSource = false,
                    bool checkedHead = false
                )
                {
                    switch (pattern)
                    {
                        case Pattern.Any { } or Pattern.Variable { Name: VariableScopeNames.Wildcard }:
                            return true;
                        case Pattern.Variable variable:
                            if (!bindings.TryAdd(variable.Name, register) && bindings[variable.Name] != register)
                            {
                                int bound = bindings[variable.Name];
                                tests.Add(failure =>
                                {
                                    owner.Op(ExactEqualTest, LabelTag, failure);
                                    owner.Operand(XRegisterTag, register);
                                    owner.Operand(XRegisterTag, bound);
                                });
                            }

                            return true;
                        case Pattern.Literal { Value: Nil }:
                            tests.Add(failure => owner.EmitNilTest(XRegisterTag, register, failure));

                            return true;
                        case Pattern.Literal literal:
                            tests.Add(failure =>
                            {
                                owner.Op(ExactEqualTest, LabelTag, failure);
                                owner.Operand(XRegisterTag, register);
                                owner.LiteralOperand(literal.Value);
                            });

                            return true;
                        case AliasPattern alias:
                            return ReadPattern(alias.Left, register, preserveSource: true)
                                && ReadPattern(alias.Right, register, preserveSource: true);
                        case Pattern.Tuple tuple:
                            bool tagged = HasTupleTag(tuple, out Atom tag);
                            if (!checkedHead)
                            {
                                tests.Add(failure =>
                                {
                                    if (tagged)
                                    {
                                        EmitDetailRecordTest(
                                            register,
                                            tuple.Items.Count,
                                            tag,
                                            failure
                                        );
                                    }
                                    else
                                    {
                                        EmitTupleShapeTest(
                                            XRegisterTag,
                                            register,
                                            tuple.Items.Count,
                                            failure
                                        );
                                    }
                                });
                            }
                            int firstField = tagged ? 1 : 0;
                            int lastField = tuple.Items.Count - 1;
                            while (lastField >= firstField && DiscardTupleField(tuple.Items[lastField]))
                            {
                                lastField--;
                            }

                            for (int field = firstField; field <= lastField; field++)
                            {
                                Pattern member = tuple.Items[field];
                                if (checkedHead && field == 1 && discriminator >= 0)
                                {
                                    if (!selectedHeads.Contains(branch) && !ReadPattern(member, discriminator))
                                    {
                                        return false;
                                    }

                                    continue;
                                }

                                if (checkedHead && field == 2 && payload >= 0)
                                {
                                    if (!ReadPattern(member, payload, preserveSource: true))
                                    {
                                        return false;
                                    }

                                    continue;
                                }

                                if (DiscardTupleField(member))
                                {
                                    continue;
                                }

                                // A scratch tuple can be replaced by its last projected binding.
                                bool reuseSource = !preserveSource && field == lastField
                                    && (register >= firstScratchRegister && member is Erlang.Pattern.Variable
                                        || canOverwriteHead && register == head && member is Erlang.Pattern.Tuple)
                                    && !bindings.ContainsValue(register);
                                if (!reuseSource && nextRegister == FirstReservedXRegister)
                                {
                                    return false;
                                }

                                int destination = reuseSource ? register : nextRegister++;
                                if (reuseSource && member is Erlang.Pattern.Variable projected)
                                {
                                    reusedBindingPositions.TryAdd(projected.Name, nextRegister);
                                }

                                int position = field;
                                tests.Add(_ => owner.EmitTuplePart(register, position, destination));
                                if (!ReadPattern(member, destination))
                                {
                                    return false;
                                }

                                if (member is Erlang.Pattern.Literal)
                                {
                                    nextRegister--;
                                }
                            }

                            return true;
                        case Pattern.List list:
                            return ReadList(list, 0, register);
                        default:
                            return false;
                    }
                }

                bool ReadGroupedHead(ConsHeadTupleGroup group, ConsHeadTupleRow row)
                {
                    if (row.TagBinding is { } alias && !ReadPattern(alias, firstConsScratch))
                    {
                        return false;
                    }

                    for (int column = 1; column < group.Fields.Length; column++)
                    {
                        Pattern member = row.Tuple.Items[group.Fields[column]];
                        if (!DiscardTupleField(member) && !ReadPattern(member, firstConsScratch + column))
                        {
                            return false;
                        }
                    }

                    return true;
                }

                bool DiscardTupleField(Pattern member) => DiscardStructuralTupleField(member, unusedFields);

                bool ReadList(Pattern.List list, int index, int register)
                {
                    if (index == list.Items.Count)
                    {
                        return ReadPattern(list.Tail ?? new Pattern.Literal(Nil.Value), register);
                    }

                    tests.Add(failure => owner.EmitNonemptyList(register, failure));
                    bool readHead = !IsDiscarded(list.Items[index]);
                    bool readTail = index + 1 < list.Items.Count || !IsDiscarded(list.Tail);
                    // A flat, closed head binds only its fields. The current cons is dead after each read.
                    bool reuseInnerHead = flatListGenerator && branch == 0 && register == head
                        && ReferenceEquals(list, ((Pattern.List)clause.Patterns[listInput]).Items[0]);
                    if (nextRegister + (readHead ? 1 : 0) + (readTail && !reuseInnerHead ? 1 : 0) > FirstReservedXRegister)
                    {
                        return false;
                    }

                    int item = readHead ? nextRegister++ : -1;
                    int rest = readTail ? reuseInnerHead ? register : nextRegister++ : -1;
                    if (readHead && readTail)
                    {
                        tests.Add(_ => owner.EmitListParts(
                            register,
                            XRegisterTag,
                            item,
                            XRegisterTag,
                            rest
                        ));
                    }
                    else if (readHead || readTail)
                    {
                        tests.Add(_ =>
                        {
                            owner.Op(readHead ? GetHead : GetTail, XRegisterTag, register);
                            owner.Operand(XRegisterTag, readHead ? item : rest);
                        });
                    }

                    return (!readHead || ReadPattern(list.Items[index], item)) && (!readTail || ReadList(list, index + 1, rest));
                }
            }

            int nonCons = consCount < branches.Count ? Label() : functionInfo;
            owner.EmitNonemptyList(listInput, nonCons);
            if (needsTail)
            {
                owner.EmitListParts(
                    listInput,
                    XRegisterTag,
                    head,
                    XRegisterTag,
                    tail
                );
            }
            else
            {
                owner.Op(GetHead, XRegisterTag, listInput);
                owner.Operand(XRegisterTag, head);
            }

            var frames = new Dictionary<int, int>();
            var arityLabels = new Dictionary<int, int>();
            int arityFailure = 0;
            if (arityDispatchEnd != 0)
            {
                var destinations = new List<(int Arity, int Label)>();
                for (int start = 0; start < arityDispatchEnd; start = sharedShapes[start].End)
                {
                    int label = Label();
                    arityLabels.Add(start, label);
                    destinations.Add((sharedShapes[start].Arity, label));
                }

                arityFailure = Label();
                owner.EmitTupleArityDispatch(arityFailure, destinations.AsEnumerable().Reverse().ToArray(), head);
            }

            bool disjointGuardedNilFallback = emptyReturn is null && consCount < clauses.Length
                && ReadListHeadRecordGuard(clauses[consCount - 1], listInput) is not null
                && clauses.Skip(consCount).All(clause => ListEntryTest(clause.Patterns[listInput]) == IsNil);
            int firstBranch = 0;
            if (consHeadGroups is not null)
            {
                // Every tagged row has a distinct key. Failed nested matches and guards
                // can only reach the unconditional cons row, while the non-cons edge
                // still sees the original input and can reach the proper nil clause.
                int consFallback = Label();
                EmitConsHeadTupleGroups(
                    consHeadGroups,
                    head,
                    firstConsScratch,
                    consFallback,
                    EmitBranch
                );
                Mark(consFallback);
                firstBranch = consCount - 1;
            }

            int sharedShapeEnd = -1;
            int sharedShapeFailure = 0;
            for (int branch = firstBranch; branch < branches.Count; branch++)
            {
                if (branch == consCount && emptyReturn is { } emptyList)
                {
                    EmitEmptyListReturn(
                        emptyList,
                        nonCons,
                        functionInfo,
                        properList: true
                    );
                    break;
                }

                if (branch == consCount && disjointGuardedNilFallback)
                {
                    Mark(nonCons);
                }

                if (branch == arityDispatchEnd && arityDispatchEnd != 0)
                {
                    Mark(arityFailure);
                }

                bool fallsBackFromCons = branch + 1 == consCount;
                bool selected = selectedHeads.Contains(branch);
                int failure = fallsBackFromCons ? nonCons : branch + 1 == branches.Count ? functionInfo
                    : selected ? 0 : Label();
                bool rejectsConsDirectly = fallsBackFromCons && (emptyReturn is not null || disjointGuardedNilFallback);
                if (rejectsConsDirectly)
                {
                    // Failure to match the cons head cannot reach the empty-list clause.
                    failure = functionInfo;
                }

                if (sharedShapes.TryGetValue(branch, out var shape))
                {
                    sharedShapeEnd = shape.End;
                    if (arityLabels.TryGetValue(branch, out int arityLabel))
                    {
                        Mark(arityLabel);
                        sharedShapeFailure = arityFailure;
                        owner.EmitTuplePart(head, 0, firstConsScratch);
                        owner.Op(ExactEqualTest, LabelTag, arityFailure);
                        owner.Operand(XRegisterTag, firstConsScratch);
                        owner.LiteralOperand(shape.Tag);
                    }
                    else
                    {
                        sharedShapeFailure = shape.End < consCount ? Label()
                            : emptyReturn is not null ? functionInfo : nonCons;
                        EmitDetailRecordTest(
                            head,
                            shape.Arity,
                            shape.Tag,
                            sharedShapeFailure
                        );
                    }
                    if (shape.SharesDiscriminator)
                    {
                        int register = firstConsScratch;
                        owner.EmitTuplePart(head, 1, register);
                        if (shape.Keys is { } keys)
                        {
                            if (shape.Payload >= 0)
                            {
                                owner.EmitTuplePart(head, 2, shape.Payload);
                            }

                            var comparer = Comparer<Term>.Create((left, right) => TermOrder.Compare(left, right, exact: true));
                            int groupStart = branch;
                            var ordered = keys.Select((key, index) => (Key: key, Branch: groupStart + index))
                                .OrderByDescending(arm => arm.Key, comparer)
                                .Select(arm => (arm.Key, arm.Branch, Label: Label())).ToArray();
                            owner.EmitMembershipSelect(
                                register,
                                sharedShapeFailure,
                                ordered.Reverse().Select(arm => (arm.Key, arm.Label))
                            );
                            foreach (var arm in ordered)
                            {
                                Mark(arm.Label);
                                EmitBranch(arm.Branch, sharedShapeFailure);
                            }

                            branch = shape.End - 1;
                            if (sharedShapeFailure != arityFailure && shape.End < branches.Count
                                && !(shape.End == consCount && emptyReturn is not null))
                            {
                                Mark(sharedShapeFailure);
                            }

                            continue;
                        }
                    }
                }

                if (branch + 1 == sharedShapeEnd)
                {
                    failure = sharedShapeFailure;
                }

                EmitBranch(branch, failure);
                if (failure != arityFailure && branch + 1 < branches.Count && !rejectsConsDirectly)
                {
                    Mark(failure);
                }
            }

            void EmitBranch(int branch, int failure)
            {
                var (clause, tests, used, tailCall, resultRegister, constant, captureOrder) = branches[branch];
                foreach (var test in tests)
                {
                    test(failure);
                }

                EmitEntryGuardPrefix(clause, clause.Guard, failure);
                if (tailCall is not null)
                {
                    EmitRegisterTailCall(tailCall);
                }
                else if (resultRegister >= 0 || constant is not null)
                {
                    if (constant is not null)
                    {
                        owner.EmitConstant(constant, 0);
                    }
                    else if (resultRegister != 0)
                    {
                        owner.MoveRegister(
                            XRegisterTag,
                            resultRegister,
                            XRegisterTag,
                            0
                        );
                    }

                    owner.instructions.WriteByte(Return);
                }
                else
                {
                    var previousOrder = entryCaptureOrder;
                    entryCaptureOrder = captureOrder;
                    EmitEntryClauseBody(
                        clause,
                        used,
                        branch,
                        frames,
                        null,
                        null,
                        literalDispatch: false
                    );
                    entryCaptureOrder = previousOrder;
                }

            }

            if (frames.Count != 0)
            {
                TupleFrames = frames;
            }

            return true;

            static bool IsDiscarded(Pattern? pattern) =>
                pattern is Pattern.Any { } or Pattern.Variable { Name: VariableScopeNames.Wildcard };
        }
    }
}
