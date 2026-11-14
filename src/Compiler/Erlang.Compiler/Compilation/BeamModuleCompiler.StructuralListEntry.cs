using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitStructuralListEntry(FunctionDefinition function, int functionInfo)
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
                if (!prefix.Any(clause => EntryShape(((Pattern.List)clause.Patterns[input]).Items[0]) is Pattern.Tuple))
                {
                    continue;
                }

                // Empty and nonempty inputs are disjoint. All other clauses keep their relative order.
                Clause[] ordered = [.. prefix, .. function.Clauses.Take(leadingNil),
                    .. function.Clauses.Skip(leadingNil + prefix.Length)];
                if (TryEmitStructuralListBranches(function, input, ordered, prefix.Length, functionInfo))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryEmitStructuralListBranches(FunctionDefinition function, int listInput, Clause[] clauses, int consCount, int functionInfo)
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
                && clauses.Skip(consCount).All(clause => ListEntryTest(clause.Patterns[listInput]) == IsNil);
            int head = arity;
            int tail = reuseTail ? listInput : arity + 1;
            bool needsTail = clauses.Take(consCount).Any(clause => !IsDiscarded(((Pattern.List)clause.Patterns[listInput]).Tail));
            var sharedShapes = new Dictionary<int, (int End, int Arity, Atom Tag, bool SharesDiscriminator, Term[]? Keys)>();
            var checkedHeads = new Dictionary<int, bool>();
            var selectedHeads = new HashSet<int>();
            for (int start = 0; start < consCount;)
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

                if (end - start > 1)
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

                    sharedShapes.Add(start, (end, shape.Arity, shape.Tag, sharesDiscriminator, keys));
                    for (int index = start; index < end; index++)
                    {
                        checkedHeads.Add(index, sharesDiscriminator);
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
                var unusedFields = clause.Patterns.SelectMany(Semantics.Variables)
                    .GroupBy(name => name, StringComparer.Ordinal)
                    .Where(group => group.Count() == 1)
                    .Select(group => group.Key).ToHashSet(StringComparer.Ordinal);
                try
                {
                    unusedFields.ExceptWith(ClosureCaptures.ReadEntryReferences(clause, unusedFields));
                }
                catch (NotSupportedException)
                {
                    unusedFields.Clear();
                }

                int nextRegister = branch < consCount ? arity + (needsTail && !reuseTail ? 2 : 1) : arity;
                bool shapeChecked = checkedHeads.TryGetValue(branch, out bool sharesDiscriminator);
                int discriminator = sharesDiscriminator ? nextRegister++ : -1;
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
                        if (!ReadPattern(list.Items[0], head, checkedHead: shapeChecked)
                            || !ReadPattern(list.Tail ?? new Pattern.List([], null), tail))
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

                Clause guardClause = clause with
                {
                    Patterns = parameters.Select(name => (Pattern)new Pattern.Variable(name)).ToArray()
                };
                if (!CanEmitSeparateEntryGuard(guardClause, clause.Guard))
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

                bool ReadPattern(Pattern pattern, int register, bool preserveSource = false, bool checkedHead = false)
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
                                        EmitDetailRecordTest(register, tuple.Items.Count, tag, failure);
                                    }
                                    else
                                    {
                                        EmitTupleShapeTest(XRegisterTag, register, tuple.Items.Count, failure);
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

                bool DiscardTupleField(Pattern member) => IsDiscarded(member)
                    || member is Erlang.Pattern.Variable variable && unusedFields.Contains(variable.Name);

                bool ReadList(Pattern.List list, int index, int register)
                {
                    if (index == list.Items.Count)
                    {
                        return ReadPattern(list.Tail ?? new Pattern.Literal(Nil.Value), register);
                    }

                    tests.Add(failure => owner.EmitNonemptyList(register, failure));
                    bool readHead = !IsDiscarded(list.Items[index]);
                    bool readTail = index + 1 < list.Items.Count || !IsDiscarded(list.Tail);
                    if (nextRegister + (readHead ? 1 : 0) + (readTail ? 1 : 0) > FirstReservedXRegister)
                    {
                        return false;
                    }

                    int item = readHead ? nextRegister++ : -1;
                    int rest = readTail ? nextRegister++ : -1;
                    if (readHead && readTail)
                    {
                        tests.Add(_ => owner.EmitListParts(register, XRegisterTag, item, XRegisterTag, rest));
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
                owner.EmitListParts(listInput, XRegisterTag, head, XRegisterTag, tail);
            }
            else
            {
                owner.Op(GetHead, XRegisterTag, listInput);
                owner.Operand(XRegisterTag, head);
            }

            var frames = new Dictionary<int, int>();
            int sharedShapeEnd = -1;
            int sharedShapeFailure = 0;
            int selectedGroupStart = -1;
            int[] selectedLabels = [];
            for (int branch = 0; branch < branches.Count; branch++)
            {
                var (clause, tests, used, tailCall, resultRegister, constant, captureOrder) = branches[branch];
                if (branch == consCount && emptyReturn is { } emptyList)
                {
                    EmitEmptyListReturn(emptyList, nonCons, functionInfo, properList: true);
                    break;
                }

                bool fallsBackFromCons = branch + 1 == consCount;
                bool selected = selectedHeads.Contains(branch);
                int failure = fallsBackFromCons ? nonCons : branch + 1 == branches.Count ? functionInfo
                    : selected ? 0 : Label();
                if (fallsBackFromCons && emptyReturn is not null)
                {
                    // Failure to match the cons head cannot reach the empty-list clause.
                    failure = functionInfo;
                }

                if (sharedShapes.TryGetValue(branch, out var shape))
                {
                    sharedShapeEnd = shape.End;
                    sharedShapeFailure = shape.End < consCount ? Label()
                        : emptyReturn is not null ? functionInfo : nonCons;
                    EmitDetailRecordTest(head, shape.Arity, shape.Tag, sharedShapeFailure);
                    if (shape.SharesDiscriminator)
                    {
                        int register = arity + (needsTail && !reuseTail ? 2 : 1);
                        owner.EmitTuplePart(head, 1, register);
                        if (shape.Keys is { } keys)
                        {
                            selectedGroupStart = branch;
                            selectedLabels = keys.Select(_ => Label()).ToArray();
                            var comparer = Comparer<Term>.Create((left, right) => TermOrder.Compare(left, right, exact: true));
                            owner.EmitMembershipSelect(register, sharedShapeFailure,
                                keys.Select((key, index) => (key, selectedLabels[index])).OrderBy(pair => pair.key, comparer));
                        }
                    }
                }

                if (selected)
                {
                    Mark(selectedLabels[branch - selectedGroupStart]);
                    failure = sharedShapeFailure;
                }
                else if (branch + 1 == sharedShapeEnd)
                {
                    failure = sharedShapeFailure;
                }

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
                        owner.MoveRegister(XRegisterTag, resultRegister, XRegisterTag, 0);
                    }

                    owner.instructions.WriteByte(Return);
                }
                else
                {
                    var previousOrder = entryCaptureOrder;
                    entryCaptureOrder = captureOrder;
                    EmitEntryClauseBody(clause, used, branch, frames, null, null, literalDispatch: false);
                    entryCaptureOrder = previousOrder;
                }

                if (branch + 1 < branches.Count && (!selected || branch + 1 == sharedShapeEnd)
                    && !(fallsBackFromCons && emptyReturn is not null))
                {
                    Mark(failure);
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
