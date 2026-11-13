using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record TupleProjectionStep(int Arity, Atom? Tag, int Field, (int Field, int Input)[] Checks);

        private bool TryEmitCallTupleCaseProjection(Clause clause)
        {
            if (clause.Guard is not null || !HasUnconditionalParameters(clause)
                || clause.Body is not Expr.Case { Value: Expr.Call producer, Clauses.Count: > 0 } decision)
            {
                return false;
            }

            var parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            var paths = new List<TupleProjectionStep[]>();
            (Expr.Tuple Tuple, string Payload)? tupleReturn = null;
            CaseTuplePlan? tupleResult = null;
            (Expr.Call Call, (int Field, string Name)[] Fields)? tupleTail = null;
            EntryCall? projectedCall = null;
            (int Field, int Register)[]? projectedFields = null;
            Term? fallback = null;
            bool returnSelector = false;
            Atom? fallbackAtom = null;
            EntryCall? fallbackCall = null;
            Expr.Call? savedTail = null;
            int fallbackInput = -1;
            for (int index = 0; index < decision.Clauses.Count; index++)
            {
                Clause arm = decision.Clauses[index];
                if (arm is { Guard: null, Patterns: [Pattern.Tuple pattern], Body: Expr.Variable returned }
                    && ReadTupleProjection(pattern, returned.Name, parameters) is { } path)
                {
                    paths.Add(path);
                }
                else if (decision.Clauses.Count == 2
                    && arm is { Guard: null, Patterns: [Pattern.Tuple tuplePattern], Body: Expr.Tuple tuple })
                {
                    var projected = Semantics.Variables(tuplePattern).Where(name => !parameters.Contains(name))
                        .Intersect(LocalFunctionEntries.Walk(tuple).OfType<Expr.Variable>().Select(variable => variable.Name))
                        .ToArray();
                    if (projected is not [var payload] || ReadTupleProjection(tuplePattern, payload, parameters) is not { } tuplePath)
                    {
                        return false;
                    }

                    paths.Add(tuplePath);
                    tupleReturn = (tuple, payload);
                }
                else if (decision.Clauses.Count == 2
                    && arm is { Guard: null, Patterns: [Pattern.Tuple tailPattern], Body: Expr.Call tailCall })
                {
                    var projected = Semantics.Variables(tailPattern).Where(name => !parameters.Contains(name))
                        .Intersect(tailCall.Arguments.OfType<Expr.Variable>().Select(variable => variable.Name))
                        .ToArray();
                    var projections = projected.Select(name => (Name: name, Path: ReadTupleProjection(tailPattern, name, parameters)))
                        .ToArray();
                    if (projections.FirstOrDefault().Path is not { } tailPath
                        || projections.Any(projection => projection.Path is not { } candidate
                            || candidate.Length != tailPath.Length
                            || !candidate.SkipLast(1).Select(step => step.Field).SequenceEqual(
                                tailPath.SkipLast(1).Select(step => step.Field))))
                    {
                        return false;
                    }

                    paths.Add(tailPath);
                    tupleTail = (tailCall, projections.Select(projection => (projection.Path![^1].Field, projection.Name))
                        .OrderBy(field => field.Field).ToArray());
                }
                else if (arm is { Guard: null, Patterns: [AliasPattern alias], Body: Expr.Variable whole }
                    && TryReadPatternAlias(alias, out var tupleAlias, out var shape)
                    && tupleAlias.Name == whole.Name && !Semantics.Variables(shape).Contains(tupleAlias.Name)
                    && shape is Pattern.Tuple rootPattern
                    && ReadTupleProjection(rootPattern, whole.Name, parameters, wholeTuple: true) is { } wholePath)
                {
                    paths.Add(wholePath);
                }
                else if (index == decision.Clauses.Count - 1 && arm.Guard is null
                    && (arm.Patterns is [Erlang.Pattern.Any]
                        || arm.Patterns is [Pattern.Variable variable] && !parameters.Contains(variable.Name)))
                {
                    if (arm.Patterns is [Pattern.Variable binding] && arm.Body is Expr.Variable result
                        && binding.Name != VariableScopeNames.Wildcard && binding.Name == result.Name)
                    {
                        returnSelector = true;
                    }
                    else if (AttributeLiteral.TryNormalizeConstant(arm.Body, out Term value))
                    {
                        fallback = value;
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (decision.Clauses.Count == 2 && arm is
                    { Guard: null, Patterns: [Pattern.Literal { Value: Atom atom }] })
                {
                    fallbackAtom = atom;
                    if (arm.Body is Expr.Call tail && ReadEntryCall(tail, []) is { } target)
                    {
                        fallbackCall = target;
                    }
                    else if (arm.Body is Expr.Call tailWithInputs)
                    {
                        savedTail = tailWithInputs;
                    }
                    else if (AttributeLiteral.TryNormalizeConstant(arm.Body, out Term value))
                    {
                        fallback = value;
                    }
                    else if (arm.Body is not Expr.Variable { Name: not VariableScopeNames.Wildcard } saved
                        || (fallbackInput = Array.IndexOf(parameters, saved.Name)) < 0)
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            if (paths.Count == 0 || fallbackAtom is not null && paths.Count != 1 || (tupleReturn is not null || tupleTail is not null) && fallbackAtom is null)
            {
                return false;
            }

            var root = paths[0][0];
            if (paths.Count > 1 && (paths.Any(path => path[0].Arity != root.Arity
                    || path[0].Field < 0 || path[0].Tag is null)
                || paths.Select(path => path[0].Tag).Distinct().Count() != paths.Count))
            {
                return false;
            }

            int[] inputs = paths.SelectMany(path => path).SelectMany(step => step.Checks)
                .Select(check => check.Input)
                .Concat(tupleReturn is { } construction
                    ? LocalFunctionEntries.Walk(construction.Tuple).OfType<Expr.Variable>()
                        .Select(variable => Array.IndexOf(parameters, variable.Name)).Where(index => index >= 0)
                    : [])
                .Concat(tupleTail is { } projectedTail
                    ? projectedTail.Call.Arguments.OfType<Expr.Variable>()
                        .Select(variable => Array.IndexOf(parameters, variable.Name)).Where(index => index >= 0)
                    : [])
                .Concat(fallbackInput >= 0 ? [fallbackInput] : [])
                .Concat(savedTail is null ? [] : LocalFunctionEntries.Walk(savedTail).OfType<Expr.Variable>()
                    .Select(variable => Array.IndexOf(parameters, variable.Name)).Where(index => index >= 0))
                .Distinct().OrderDescending().ToArray();
            if (ReadEntrySelector(producer, parameters, inputs, trimFrame: false) is not { } selector)
            {
                return false;
            }

            if (tupleReturn is { } returnedTuple)
            {
                var saved = selector.Saved.Select((index, slot) => (parameters[index], slot))
                    .ToDictionary(binding => binding.Item1, binding => binding.slot, StringComparer.Ordinal);
                tupleResult = ReadCaseTuple(returnedTuple.Tuple, returnedTuple.Payload, saved);
                if (tupleResult is null)
                {
                    return false;
                }
            }

            if (savedTail is not null)
            {
                fallbackCall = ReadEntryCall(savedTail, [], selector.StackInputs, allowControlTransfer: true)
                    ?? ReadStraightConstructedCall(
                        savedTail,
                        null,
                        selector.Saved.Select((input, slot) => (Name: parameters[input], Slot: slot))
                            .ToDictionary(binding => binding.Name, binding => binding.Slot, StringComparer.Ordinal),
                        allowControlTransfer: true
                    );
                if (fallbackCall is null)
                {
                    return false;
                }

                fallbackCall = PrepareStackFreeTailThrow(fallbackCall, selector.StackInputs);
            }

            if (tupleTail is { } matchedTail)
            {
                var arguments = matchedTail.Call.Arguments.ToArray();
                var registers = new string[arguments.Length];
                Array.Fill(registers, VariableScopeNames.Wildcard);
                projectedFields = matchedTail.Fields.Select(field => (field.Field, Register: Array.FindIndex(arguments,
                    argument => argument is Expr.Variable variable && variable.Name == field.Name))).ToArray();
                int source = paths[0].Length == 1 ? 0 : 1;
                int overwritten = Array.FindIndex(projectedFields, field => field.Register == source);
                if (overwritten >= 0 && overwritten < projectedFields.Length - 1)
                {
                    int last = projectedFields.Length - 1;
                    int replacement = projectedFields[last].Register;
                    projectedFields[overwritten].Register = replacement;
                    projectedFields[last].Register = source;
                }

                for (int field = 0; field < projectedFields.Length; field++)
                {
                    registers[projectedFields[field].Register] = matchedTail.Fields[field].Name;
                }

                projectedCall = ReadEntryCall(matchedTail.Call, registers, selector.StackInputs);
                if (projectedCall is null)
                {
                    return false;
                }
            }

            int frame = selector.FrameSize - EmitEntrySelector(selector, producer, parameters);
            inputs = selector.Saved;
            var previous = CallLocation(producer, location);
            int selectorType = RegisterCaseSelectorType(selector.Call, IsTuple);
            if (paths.Count == 1)
            {
                int alternative = Label();
                int failure = alternative;
                if (fallbackAtom is not null)
                {
                    failure = Label();
                    owner.Op(IsTuple, LabelTag, alternative);
                    if (selectorType == 0)
                    {
                        owner.Operand(XRegisterTag, 0);
                    }
                    else
                    {
                        owner.EmitTypedRegister(0, selectorType);
                    }
                }

                int register = fallback is not null && fallbackAtom is null ? 0 : 1;
                EmitTupleProjection(paths[0], 0, register, failure, selectorType, inputs, frame,
                    shapeChecked: fallbackAtom is not null, result: tupleResult, typeSavedChecks: fallbackInput >= 0 || savedTail is not null, tail: projectedCall, previous: previous, projectedFields: projectedFields);
                if (fallbackAtom is not null)
                {
                    Mark(alternative);
                    if (!HasKnownNonTupleResult(selector.Call, fallbackAtom))
                    {
                        owner.Op(ExactEqualTest, LabelTag, failure);
                        owner.Operand(XRegisterTag, 0);
                        owner.LiteralOperand(fallbackAtom);
                    }
                    if (fallbackCall is not null)
                    {
                        if (savedTail is not null)
                        {
                            EmitSavedCallArguments(new SavedCallStep(fallbackCall, null, false, []), Enumerable.Range(0, frame).ToArray());
                        }
                        else
                        {
                            EmitEntryCallArguments(fallbackCall);
                        }
                        EmitPreparedFrameTail(fallbackCall, previous, frame);
                    }
                    else if (fallbackInput >= 0)
                    {
                        owner.MoveRegister(YRegisterTag, Array.IndexOf(inputs, fallbackInput), XRegisterTag, 0);
                        owner.EmitFrameReturn(frame);
                    }
                    else
                    {
                        owner.EmitConstant(fallback!, 0);
                        owner.EmitFrameReturn(frame);
                    }
                    Mark(failure);
                    if (decision.SourceLine != 0
                        && (savedTail is not null && fallbackCall?.Module is not null && CallLocation(savedTail, previous) != previous
                            || projectedCall?.Module is not null && CallLocation(projectedCall.Call, previous) != previous))
                    {
                        owner.EmitLine(new BeamSourceLocation(decision.PreprocessorFile ?? location.File, decision.SourceLine));
                    }

                    owner.Op(CaseEnd, XRegisterTag, 0);
                }
                else
                {
                    EmitFallback(failure);
                }
            }
            else
            {
                // Leave the original selector in X0 for any failing path.
                const int payloadRegister = 2;
                var atomOrder = Comparer<Atom>.Create((left, right) => TermOrder.Compare(left, right, exact: true));
                var ordered = paths.OrderByDescending(path => path[0].Tag!, atomOrder).ToArray();
                int[] labels = ordered.Select(_ => Label()).ToArray();
                int failure = Label();
                EmitTupleShapeTest(XRegisterTag, 0, root.Arity, failure, selectorType);
                owner.EmitTuplePart(0, 0, 1);
                bool sharedField = ordered.All(path => path[0].Field == root.Field);
                if (sharedField)
                {
                    owner.EmitTuplePart(0, root.Field, payloadRegister);
                }
                owner.EmitMembershipSelect(
                    1,
                    failure,
                    ordered.Select((path, index) => ((Term)path[0].Tag!, labels[index])).Reverse()
                );
                for (int index = 0; index < ordered.Length; index++)
                {
                    Mark(labels[index]);
                    var path = ordered[index];
                    EmitProjectionChecks(path[0], 0, failure, inputs);
                    if (path.Length == 1)
                    {
                        if (sharedField)
                        {
                            owner.MoveRegister(XRegisterTag, payloadRegister, XRegisterTag, 0);
                        }
                        else
                        {
                            owner.EmitTuplePart(0, path[0].Field, 0);
                        }

                        owner.EmitFrameReturn(frame);
                    }
                    else
                    {
                        int register = sharedField ? payloadRegister : 1;
                        if (!sharedField)
                        {
                            owner.EmitTuplePart(0, path[0].Field, register);
                        }

                        EmitTupleProjection(path.Skip(1), register, register, failure, 0, inputs, frame);
                    }
                }

                EmitFallback(failure);
            }

            return true;

            void EmitFallback(int failure)
            {
                Mark(failure);
                if (returnSelector)
                {
                    owner.EmitFrameReturn(frame);
                }
                else if (fallback is not null)
                {
                    owner.EmitConstant(fallback, 0);
                    owner.EmitFrameReturn(frame);
                }
                else
                {
                    owner.Op(CaseEnd, XRegisterTag, 0);
                }
            }
        }

        private static TupleProjectionStep[]? ReadTupleProjection(
            Pattern.Tuple root,
            string returned,
            string[] parameters,
            bool wholeTuple = false
        )
        {
            if (returned == VariableScopeNames.Wildcard || parameters.Contains(returned))
            {
                return null;
            }

            string[] bindings = Semantics.Variables(root).ToArray();
            var newBindings = bindings.Where(name => !parameters.Contains(name)).ToArray();
            if (newBindings.Distinct(StringComparer.Ordinal).Count() != newBindings.Length)
            {
                return null;
            }

            var steps = new List<TupleProjectionStep>();
            Pattern.Tuple tuple = root;
            while (true)
            {
                Atom? tag = tuple.Items.FirstOrDefault() is Pattern.Literal { Value: Atom atom } ? atom : null;
                int field = -1;
                Pattern? member = null;
                var checks = new List<(int Field, int Input)>();
                for (int index = tag is null ? 0 : 1; index < tuple.Items.Count; index++)
                {
                    Pattern item = tuple.Items[index];
                    if (item is Pattern.Variable name && name.Name != returned)
                    {
                        int input = name.Name == VariableScopeNames.Wildcard ? -1 : Array.IndexOf(parameters, name.Name);
                        if (input >= 0)
                        {
                            checks.Add((index, input));
                        }

                        continue;
                    }

                    if (item is Erlang.Pattern.Any)
                    {
                        continue;
                    }

                    if (field >= 0 || item is not (Erlang.Pattern.Tuple or Erlang.Pattern.Variable))
                    {
                        return null;
                    }

                    field = index;
                    member = item;
                }

                if (wholeTuple)
                {
                    return field < 0 ? [new TupleProjectionStep(tuple.Items.Count, tag, -1, checks.ToArray())] : null;
                }

                if (field < 0)
                {
                    return null;
                }

                steps.Add(new TupleProjectionStep(tuple.Items.Count, tag, field, checks.ToArray()));
                if (member is Erlang.Pattern.Variable)
                {
                    return steps.ToArray();
                }

                tuple = (Pattern.Tuple)member!;
            }
        }

        private void EmitTupleProjection(
            IEnumerable<TupleProjectionStep> path,
            int input,
            int scratch,
            int failure,
            int selectorType,
            int[] inputs,
            int frame,
            bool shapeChecked = false,
            CaseTuplePlan? result = null,
            bool typeSavedChecks = false,
            EntryCall? tail = null,
            BeamSourceLocation? previous = null,
            (int Field, int Register)[]? projectedFields = null
        )
        {
            TupleProjectionStep[] steps = path.ToArray();
            for (int index = 0; index < steps.Length; index++)
            {
                var step = steps[index];
                if (step.Tag is { } tag)
                {
                    if (selectorType != 0 && !shapeChecked)
                    {
                        owner.Op(IsTuple, LabelTag, failure);
                        owner.EmitTypedRegister(input, selectorType);
                    }

                    EmitDetailRecordTest(input, step.Arity, tag, failure, selectorType != 0 || shapeChecked ? TupleTypeBits : 0);
                }
                else if (shapeChecked)
                {
                    owner.Op(TestArity, LabelTag, failure);
                    owner.Operand(XRegisterTag, input);
                    owner.Operand(BeamWire.UnsignedTag, step.Arity);
                }
                else
                {
                    EmitTupleShapeTest(XRegisterTag, input, step.Arity, failure, selectorType);
                }

                EmitProjectionChecks(step, input, failure, inputs, typed: result is not null || typeSavedChecks);
                if (result is not null && index == steps.Length - 1)
                {
                    EmitCaseTuple(result with { EntryLive = input + 1 }, (step.Field, 0), extractionSource: input);
                    break;
                }
                if (projectedFields is not null && index == steps.Length - 1)
                {
                    foreach (var (field, register) in projectedFields)
                    {
                        owner.EmitTuplePart(input, field, register);
                    }
                }
                else if (step.Field >= 0)
                {
                    owner.EmitTuplePart(input, step.Field, index == steps.Length - 1 ? 0 : scratch);
                }

                input = scratch;
                selectorType = 0;
                shapeChecked = false;
            }

            if (tail is not null)
            {
                owner.EmitOrderedTailMoves(tail.Moves, tail.Constants, tail.SavedInputs);
                EmitPreparedFrameTail(tail, previous ?? location, frame);

                return;
            }

            owner.EmitFrameReturn(frame);
        }

        private void EmitProjectionChecks(
            TupleProjectionStep step,
            int input,
            int failure,
            int[] inputs,
            bool typed = false
        )
        {
            int scratch = input == 1 ? 2 : 1;
            foreach (var check in step.Checks)
            {
                owner.EmitTuplePart(input, check.Field, scratch);
                owner.Op(ExactEqualTest, LabelTag, failure);
                owner.Operand(XRegisterTag, scratch);
                int slot = Array.IndexOf(inputs, check.Input);
                int type = typed && recursiveClosure is null && parentFunction is { } function
                    ? ReadSavedComparisonEntryType(function, check.Input) : 0;
                if (type != 0)
                {
                    owner.EmitTypedRegister(slot, type, registerTag: YRegisterTag);
                }
                else
                {
                    owner.Operand(YRegisterTag, slot);
                }
            }
        }
    }
}
