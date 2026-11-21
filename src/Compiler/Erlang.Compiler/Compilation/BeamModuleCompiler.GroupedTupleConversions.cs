using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record TupleConversionFallback(
            Expr.Call Keep,
            Expr.Call Convert,
            Expr.Call Map,
            Expr.Call Rebuild
        );
        private sealed record TupleConstructionPart(Term? Literal, int Tag = XRegisterTag, int Register = 0);
        private sealed record TaggedConversionArm(
            int Width,
            Atom Tag,
            bool NeedsShared,
            Action<int, int, int, List<(int Label, Expr.Match Check, int Register)>> Emit
        );

        private bool TryEmitGroupedTupleConversion(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || function.Arity != 1
                || owner.OnLoadFunction is not null || function.Clauses.Count < 5
                || ReadTupleConversionFallback(function.Clauses[^3]) is not { } fallback
                || !ReadsGuardedListTail(function.Clauses[^2], fallback.Map.Function)
                || function.Clauses[^1] is not
                {
                    Guard: null, Patterns: [Pattern.Variable identity], Body: Expr.Variable unchanged
                } || !DistinctBindings(identity) || identity.Name != unchanged.Name)
            {
                return false;
            }

            var arms = new List<TaggedConversionArm>();
            foreach (var clause in function.Clauses.SkipLast(3))
            {
                if (ReadTaggedConversionArm(clause) is not { } arm)
                {
                    return false;
                }

                arms.Add(arm);
            }

            var groups = arms.GroupBy(arm => arm.Width).OrderBy(group => group.Key).ToArray();
            if (groups.Length < 2 || groups.Any(group => group.Select(arm => arm.Tag).Distinct().Count() != group.Count()
                || group.Any(arm => arm.NeedsShared) && (group.Key != 3 || group.Count() < 2)))
            {
                return false;
            }

            var labels = groups.ToDictionary(group => group.Key, _ => Label());
            int tupleFallback = Label();
            int nonTuple = Label();
            int unchangedLabel = Label();
            var errors = new List<(int Label, Expr.Match Check, int Register)>();
            owner.EmitTupleArityDispatch(tupleFallback,
                groups.Select(group => (group.Key, labels[group.Key])).ToArray(), typeFailure: nonTuple);
            foreach (var group in groups.Reverse())
            {
                Mark(labels[group.Key]);
                owner.EmitTuplePart(0, 0, 1);
                int live = 1;
                if (group.Key == 3 && group.Count() > 1)
                {
                    // Every arm needs at least one of these fields. Keep the checked
                    // tuple itself for a nested check that can fall through.
                    owner.EmitTuplePart(0, 1, 2);
                    owner.EmitTuplePart(0, 2, 3);
                    live = 4;
                }

                if (group.Count() == 1)
                {
                    var arm = group.Single();
                    owner.EmitAtomTest(1, arm.Tag.Name, tupleFallback);
                    arm.Emit(
                        live,
                        tupleFallback,
                        unchangedLabel,
                        errors
                    );
                    continue;
                }

                var branches = group.OrderBy(arm => arm.Tag, MapKeyOrder).Select(arm => (Arm: arm, Label: Label())).ToArray();
                owner.EmitMembershipSelect(1, tupleFallback, branches.Select(branch => ((Term)branch.Arm.Tag, branch.Label)));
                foreach (var branch in branches.Reverse())
                {
                    Mark(branch.Label);
                    branch.Arm.Emit(
                        live,
                        tupleFallback,
                        unchangedLabel,
                        errors
                    );
                }
            }

            int rebuild = Label();
            Mark(tupleFallback);
            EmitFrameAllocation(1, 1);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                0
            );
            owner.EmitCallLine(fallback.Keep, location);
            Call(null, fallback.Keep.Function, 1);
            owner.EmitAtomTest(0, "true", rebuild);
            owner.MoveRegister(
                YRegisterTag,
                0,
                XRegisterTag,
                0
            );
            owner.EmitFrameReturn(1);
            Mark(rebuild);
            owner.MoveRegister(
                YRegisterTag,
                0,
                XRegisterTag,
                0
            );
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.EmitCallLine(fallback.Rebuild, CallLocation(fallback.Keep, location));
            Call(ErlangModuleNames.Module, fallback.Convert.Function, 1);
            Call(null, fallback.Map.Function, 1);
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, fallback.Rebuild.Function, 1));
            owner.Operand(BeamWire.UnsignedTag, 0);

            Mark(nonTuple);
            owner.Op(IsList, LabelTag, unchangedLabel);
            owner.Operand(XRegisterTag, 0);
            owner.Op(TailLocalCall, BeamWire.UnsignedTag, 1);
            owner.Operand(LabelTag, entries[(fallback.Map.Function, 1)]);
            Mark(unchangedLabel);
            owner.instructions.WriteByte(Return);
            foreach (var (label, check, register) in errors)
            {
                Mark(label);
                owner.EmitLine(new(check.PreprocessorFile ?? location.File, check.SourceLine == 0 ? location.Line : check.SourceLine));
                owner.Op(BadMatch, XRegisterTag, register);
            }

            return true;
        }

        private TupleConversionFallback? ReadTupleConversionFallback(Clause clause)
        {
            if (clause is not
                {
                    Patterns: [Pattern.Variable input],
                    Guard: Expr.Call { Function: AutoImportNames.IsTuple, DynamicRemote: false, Arguments: [Expr.Variable tested] } guard,
                    Body: Expr.Case
                    {
                        Value: Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable checkedInput] } keep,
                        Clauses:
                        [
                        { Guard: null, Patterns: [Pattern.Literal { Value: Atom { Name: "true" } }], Body: Expr.Variable kept },
                        {
                            Guard: null, Patterns: [Pattern.Literal { Value: Atom { Name: "false" } }],
                            Body: Expr.Call
                            {
                                DynamicRemote: false, Function: AutoImportNames.ListToTuple,
                                Arguments: [Expr.Call
                                {
                                    Module: null, DynamicRemote: false, Arguments: [Expr.Call
                                    {
                                        DynamicRemote: false, Function: AutoImportNames.TupleToList, Arguments: [Expr.Variable converted]
                                    } convert]
                                } map]
                            } rebuild
                        }
                        ]
                    }
                } || !DistinctBindings(input) || tested.Name != input.Name || checkedInput.Name != input.Name
                || kept.Name != input.Name || converted.Name != input.Name
                || !owner.ResolvesErlangBif(guard) || !owner.ResolvesErlangBif(convert) || !owner.ResolvesErlangBif(rebuild)
                || !entries.ContainsKey((keep.Function, 1)) || !entries.ContainsKey((map.Function, 1))
                || owner.IsDeclaredNative((keep.Function, 1))
                || !localFunctions.TryGetValue((keep.Function, 1), out var predicate)
                || !predicate.Clauses.All(branch => IsBooleanResult(branch.Body)))
            {
                return null;
            }

            return new(
                keep,
                convert,
                map,
                rebuild
            );
        }

        private bool ReadsGuardedListTail(Clause clause, string target) => clause is
        {
            Patterns: [Pattern.Variable input],
            Guard: Expr.Call { Function: AutoImportNames.IsList, DynamicRemote: false, Arguments: [Expr.Variable guarded] } guard,
            Body: Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable forwarded] } call
        } && DistinctBindings(input) && guarded.Name == input.Name && forwarded.Name == input.Name
            && call.Function == target && owner.ResolvesErlangBif(guard);

        private TaggedConversionArm? ReadTaggedConversionArm(Clause clause)
        {
            if (clause is not { Guard: null, Patterns: [var shape] })
            {
                return null;
            }

            Pattern.Variable? alias = null;
            if (shape is AliasPattern aliased)
            {
                if (!TryReadPatternAlias(aliased, out var bound, out shape))
                {
                    return null;
                }

                alias = bound;
            }

            if (shape is not Pattern.Tuple { Items: [Pattern.Literal { Value: Atom tag }, ..] } tuple
                || tuple.Items.Count < 2 || tuple.Items.Count >= FirstReservedXRegister)
            {
                return null;
            }

            var bindings = new Dictionary<string, int>(StringComparer.Ordinal);
            if (alias is not null && alias.Name != VariableScopeNames.Wildcard)
            {
                bindings.Add(alias.Name, 0);
            }

            var nestedFields = new Dictionary<string, int>(StringComparer.Ordinal);
            Pattern.Tuple? nested = null;
            int nestedField = -1;
            for (int field = 1; field < tuple.Items.Count; field++)
            {
                Pattern pattern = tuple.Items[field];
                if (pattern is Pattern.Tuple inner)
                {
                    if (nested is not null)
                    {
                        return null;
                    }
                    nested = inner;
                    nestedField = field;
                }
                else if (pattern is not Pattern.Variable variable || !Bind(bindings, variable, field))
                {
                    return null;
                }
            }

            Expr body = clause.Body;
            Expr.Match? nestedCheck = null;
            if (body is Expr.Sequence { Items: [Expr.Match { Pattern: Pattern.Tuple explicitPattern, Value: Expr.Variable read } check, ..] } sequence
                && bindings.TryGetValue(read.Name, out int fieldIndex) && fieldIndex > 0 && nested is null)
            {
                nested = explicitPattern;
                nestedField = fieldIndex;
                nestedCheck = check;
                bindings.Remove(read.Name);
                body = new Expr.Sequence(sequence.Items.Skip(1).ToArray());
            }

            Atom? nestedTag = null;
            if (nested is not null)
            {
                if (nested.Items is not [Pattern.Literal { Value: Atom expected }, ..] || nested.Items.Count >= FirstReservedXRegister)
                {
                    return null;
                }

                nestedTag = expected;
                for (int field = 1; field < nested.Items.Count; field++)
                {
                    if (nested.Items[field] is not Pattern.Variable variable || bindings.ContainsKey(variable.Name)
                        || !Bind(nestedFields, variable, field))
                    {
                        return null;
                    }
                }
            }

            if (alias is not null && nested is null && body is Expr.Call
                {
                    DynamicRemote: false, Function: AutoImportNames.Setelement,
                    Arguments: [Expr.Literal { Value: Integer index }, Expr.Variable target, Expr.Literal { Value: Atom replacement }]
                } retag && owner.ResolvesErlangBif(retag) && index.Value.IsOne && target.Name == alias.Name
                && bindings.Count == 1)
            {
                return new(
                    tuple.Items.Count,
                    tag,
                    false,
                    (
                        live,
                        _,
                        _,
                        _
                    ) =>
                {
                    Heap(tuple.Items.Count + 1, live);
                    owner.Op(UpdateRecord, AtomTag, owner.Intern(RecordCopyHint));
                    owner.Operand(BeamWire.UnsignedTag, tuple.Items.Count);
                    owner.EmitTypedRegister(0, TupleTypeBits);
                    owner.Operand(XRegisterTag, 0);
                    owner.instructions.WriteByte(ExtendedListOperand);
                    owner.Operand(BeamWire.UnsignedTag, 2);
                    owner.Operand(BeamWire.UnsignedTag, 1);
                    owner.LiteralOperand(replacement);
                    owner.instructions.WriteByte(Return);
                }
                );
            }

            string? version = null;
            Atom? versionValue = null;
            if (alias is not null && nested is null && body is Expr.Case
                {
                    Value: Expr.Variable selector,
                    Clauses:
                    [
                    { Guard: null, Patterns: [Pattern.Literal { Value: Atom literal }], Body: var converted },
                    { Guard: null, Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }], Body: Expr.Variable original }
                    ]
                } && original.Name == alias.Name && bindings.TryGetValue(selector.Name, out int versionField) && versionField > 0)
            {
                version = selector.Name;
                versionValue = literal;
                body = converted;
            }

            if (nested is not null && body is Expr.Sequence
                {
                    Items: [Expr.Match call1, Expr.Match call2, Expr.Match
                    {
                        Pattern: Pattern.Variable temporary, Value: var built
                    }, var returned]
                }
                && temporary.Name != VariableScopeNames.Wildcard && !bindings.ContainsKey(temporary.Name)
                && !nestedFields.ContainsKey(temporary.Name) && ExpandTuple(built) is { } innerValue
                && ExpandTuple(returned) is { } outer
                && LocalFunctionEntries.Walk(outer).OfType<Expr.Variable>().Count(variable => variable.Name == temporary.Name) == 1
                && outer.Items.Any(item => item is Expr.Variable variable && variable.Name == temporary.Name)
                && call1.Pattern is Pattern.Variable priorFirst && priorFirst.Name != temporary.Name
                && call2.Pattern is Pattern.Variable priorSecond && priorSecond.Name != temporary.Name)
            {
                body = new Expr.Sequence([call1, call2, new Expr.Tuple(outer.Items.Select(item =>
                    item is Expr.Variable variable && variable.Name == temporary.Name ? innerValue : item).ToArray())]);
            }

            Expr.Call? first = null;
            Expr.Call? second = null;
            string? firstResult = null;
            string? secondResult = null;
            if (nested is not null && body is Expr.Sequence
                {
                    Items:
                    [
                        Expr.Match { Pattern: Pattern.Variable firstBinding, Value: Expr.Call { Arguments: [Expr.Variable firstInput] } convertFirst },
                        Expr.Match { Pattern: Pattern.Variable secondBinding, Value: Expr.Call { Arguments: [Expr.Variable secondInput] } convertSecond },
                        var output
                    ]
                } && DistinctBindings(firstBinding, secondBinding)
                && nestedFields.GetValueOrDefault(firstInput.Name) == 1 && nestedFields.GetValueOrDefault(secondInput.Name) == 2
                && nestedFields.Count == 2 && bindings.Count == 1 && bindings.ContainsValue(3 - nestedField)
                && firstBinding.Name != secondInput.Name && secondBinding.Name != firstInput.Name
                && !bindings.ContainsKey(firstBinding.Name) && !bindings.ContainsKey(secondBinding.Name)
                && !nestedFields.ContainsKey(firstBinding.Name) && !nestedFields.ContainsKey(secondBinding.Name)
                && TryResolveOrdinaryCallTarget(convertFirst, out string? firstModule) && firstModule is null
                && TryResolveOrdinaryCallTarget(convertSecond, out string? secondModule) && secondModule is null)
            {
                first = convertFirst;
                second = convertSecond;
                firstResult = firstBinding.Name;
                secondResult = secondBinding.Name;
                body = output;
            }

            Expr.Call? single = null;
            string? singleField = null;
            if (nested is null && ExpandTuple(body) is { } result)
            {
                Expr.Call[] calls = result.Items.OfType<Expr.Call>().ToArray();
                if (calls is [Expr.Call { Arguments: [Expr.Variable input] } convert]
                    && bindings.GetValueOrDefault(input.Name) == 2 && bindings.Count == 2
                    && TryResolveOrdinaryCallTarget(convert, out string? module) && module is null)
                {
                    single = convert;
                    singleField = input.Name;
                }
            }

            var names = new HashSet<string>(bindings.Keys, StringComparer.Ordinal);
            if (alias is not null)
            {
                names.Remove(alias.Name);
            }
            if (first is null)
            {
                names.UnionWith(nestedFields.Keys);
            }
            else if (secondResult is null)
            {
                return null;
            }

            if (firstResult is not null)
            {
                names.Add(firstResult);
            }
            if (secondResult is not null)
            {
                names.Add(secondResult);
            }
            if (ExpandTuple(body) is not { } constructed || !CheckTuple(constructed)
                || singleField is not null && LocalFunctionEntries.Walk(constructed).OfType<Expr.Variable>()
                    .Count(variable => variable.Name == singleField) != 1
                || first is not null && constructed.Items.Any(item => ExpandTuple(item) is not null)
                    && constructed.Items.Any(item => item is Expr.Variable variable && variable.Name == secondResult)
                || nestedCheck is not null && first is null
                || nested is not null && first is null && (tuple.Items.Count == 2 ? bindings.Count != 0
                    : tuple.Items.Count != 3 || nestedFields.Count != 1)
                || first is null && constructed.Items.Any(item => ExpandTuple(item) is not null)
                    && constructed.Items.Any(item => item is Expr.Variable))
            {
                return null;
            }

            return new(
                tuple.Items.Count,
                tag,
                first is not null || single is not null || nested is not null && tuple.Items.Count == 3,
                Emit
            );

            bool CheckTuple(Expr.Tuple value) => value.Items.Count > 0
                && value.Items.Count(item => ExpandTuple(item) is not null) <= 1
                && value.Items.All(item => AttributeLiteral.TryNormalizeConstant(item, out _)
                    || item is Expr.Variable variable && names.Contains(variable.Name)
                    || ReferenceEquals(item, single) || ExpandTuple(item) is { } child && CheckTuple(child));

            void Emit(
                int live,
                int fallback,
                int identity,
                List<(int Label, Expr.Match Check, int Register)> errors
            )
            {
                var values = new Dictionary<string, TupleConstructionPart>(StringComparer.Ordinal);
                int heap = ReadHeap(constructed);
                bool shared = live > 1;
                int root = 0;
                if (version is not null)
                {
                    owner.EmitTuplePart(0, bindings[version], 1);
                    owner.EmitAtomTest(1, versionValue!.Name, identity);
                    values.Add(version, new(versionValue));
                }

                if (nested is not null)
                {
                    int nestedRegister = shared ? nestedField + 1 : 1;
                    if (!shared)
                    {
                        owner.EmitTuplePart(0, nestedField, nestedRegister);
                    }
                    int reject = fallback;
                    if (nestedCheck is not null)
                    {
                        reject = Label();
                        errors.Add((reject, nestedCheck, nestedRegister));
                    }

                    owner.EmitRecordTest(
                        reject,
                        XRegisterTag,
                        nestedRegister,
                        nestedTag!,
                        nested.Items.Count
                    );
                    if (first is not null)
                    {
                        EmitFrameAllocation(live, 2);
                        var carried = bindings.Single();
                        owner.MoveRegister(
                            XRegisterTag,
                            carried.Value + 1,
                            YRegisterTag,
                            1
                        );
                        owner.EmitTuplePart(nestedRegister, 1, 0);
                        owner.Op(GetTupleElement, XRegisterTag, nestedRegister);
                        owner.Operand(BeamWire.UnsignedTag, 2);
                        owner.Operand(YRegisterTag, 0);
                        owner.EmitCallLine(first, location);
                        Call(null, first.Function, 1);
                        owner.Op(Swap, YRegisterTag, 0);
                        owner.Operand(XRegisterTag, 0);
                        owner.EmitCallLine(second!, CallLocation(first, location));
                        Call(null, second!.Function, 1);
                        values.Add(carried.Key, new(null, YRegisterTag, 1));
                        values.Add(firstResult!, new(null, YRegisterTag, 0));
                        values.Add(secondResult!, new(null));
                        Heap(heap, 1);
                        Construct(constructed, values);
                        owner.EmitFrameReturn(2);

                        return;
                    }

                    if (!shared)
                    {
                        root = nestedRegister;
                    }
                }

                if (single is not null)
                {
                    EmitFrameAllocation(live, 1);
                    owner.MoveRegister(
                        XRegisterTag,
                        2,
                        YRegisterTag,
                        0
                    );
                    owner.MoveRegister(
                        XRegisterTag,
                        3,
                        XRegisterTag,
                        0
                    );
                    owner.EmitCallLine(single, location);
                    Call(null, single.Function, 1);
                    values.Add(bindings.Single(pair => pair.Key != singleField).Key, new(null, YRegisterTag, 0));
                    Heap(heap, 1);
                    Construct(constructed, values);
                    owner.EmitFrameReturn(1);

                    return;
                }

                Heap(heap, shared ? live : root + 1);
                if (shared)
                {
                    foreach (var (name, field) in bindings)
                    {
                        if (field > 0 && name != version)
                        {
                            values.Add(name, new(null, XRegisterTag, field + 1));
                        }
                    }
                }
                else if (nested is null)
                {
                    Project(0, bindings.Where(pair => pair.Value > 0 && pair.Key != version).ToArray(), values);
                }

                if (nested is not null)
                {
                    Project(
                        shared ? nestedField + 1 : root,
                        nestedFields.ToArray(),
                        values,
                        replaceWithZero: shared
                    );
                }

                Construct(constructed, values);
                owner.instructions.WriteByte(Return);
            }

            void Project(
                int input,
                KeyValuePair<string, int>[] fields,
                Dictionary<string, TupleConstructionPart> values,
                bool replaceWithZero = false
            )
            {
                int next = input == 0 ? 1 : 0;
                for (int index = 0; index < fields.Length; index++)
                {
                    if (next == input)
                    {
                        next++;
                    }
                    int register = index == fields.Length - 1 ? replaceWithZero ? 0 : input : next++;
                    owner.EmitTuplePart(input, fields[index].Value, register);
                    values.Add(fields[index].Key, new(null, XRegisterTag, register));
                }
            }
        }

        private static Expr.Tuple? ExpandTuple(Expr expression) => expression switch
        {
            Expr.Tuple tuple => tuple,
            Expr.RecordConstruction record => RecordExpressions.Create(record),
            _ => null
        };

        private static bool Bind(Dictionary<string, int> fields, Pattern.Variable variable, int index) =>
            variable.Name == VariableScopeNames.Wildcard || fields.TryAdd(variable.Name, index);

        private static int ReadHeap(Expr.Tuple tuple) => tuple.Items.Count + 1
            + tuple.Items.Sum(item => !AttributeLiteral.TryNormalizeConstant(item, out _)
                && ExpandTuple(item) is { } nested ? ReadHeap(nested) : 0);

        private void Heap(int words, int live)
        {
            owner.Op(TestHeap, BeamWire.UnsignedTag, words);
            owner.Operand(BeamWire.UnsignedTag, live);
        }

        private void Construct(Expr.Tuple tuple, Dictionary<string, TupleConstructionPart> fields)
        {
            foreach (Expr item in tuple.Items)
            {
                if (!AttributeLiteral.TryNormalizeConstant(item, out _) && ExpandTuple(item) is { } nested)
                {
                    Construct(nested, fields);
                }
            }

            owner.EmitTupleHeader(XRegisterTag, tuple.Items.Count);
            foreach (Expr item in tuple.Items)
            {
                if (AttributeLiteral.TryNormalizeConstant(item, out Term literal))
                {
                    owner.LiteralOperand(literal);
                }
                else if (item is Expr.Variable variable)
                {
                    var field = fields[variable.Name];
                    if (field.Literal is { } constant)
                    {
                        owner.LiteralOperand(constant);
                    }
                    else
                    {
                        owner.Operand(field.Tag, field.Register);
                    }
                }
                else
                {
                    owner.Operand(XRegisterTag, 0);
                }
            }
        }
    }
}
