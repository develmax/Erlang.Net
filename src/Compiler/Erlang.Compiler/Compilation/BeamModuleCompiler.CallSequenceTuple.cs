using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record CallSequenceTupleSource(
            Expr.Tuple Tuple,
            Expr.Fun? Closure,
            string? ClosureBinding,
            string[] Captures,
            HashSet<string> Reads,
            bool RetainedValue = false
        );

        private sealed record CallSequenceTuple(
            CaseTupleOperand[] Operands,
            Expr.Fun? Closure,
            Dictionary<string, int> SavedCaptures,
            Dictionary<string, int> CaptureOrder,
            string? ResultCapture,
            int Live,
            CallArgumentConstructions? Construction = null,
            CaseTupleOperand? Scalar = null,
            (int Slot, int Field, int Register)[]? Projections = null
        );

        private bool TryEmitSavedCallSequenceTuple(Clause clause)
        {
            if (clause.Guard is not null || !HasUnconditionalParameters(clause))
            {
                return false;
            }

            string[] parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            if (ReadSavedCallSequenceTuple(clause.Body, parameters) is not { } plan)
            {
                return false;
            }

            EmitSavedCallSequence(plan);

            return true;
        }

        private SavedCallSequence? ReadSavedCallSequenceTuple(Expr body, string[] parameters)
        {
            if (body is not (Expr.Tuple or Expr.RecordConstruction
                or Expr.Sequence { Items: [.., Expr.Tuple or Expr.RecordConstruction or Expr.Variable] }))
            {
                return null;
            }

            IReadOnlyList<Expr> items = body is Expr.Sequence sequence ? sequence.Items : [body];
            bool retainedValue = items[^1] is Expr.Variable;
            if (retainedValue && (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || items.Count < 4 || items[^2] is not Expr.Call
                || !items.SkipLast(1).OfType<Expr.Match>().Any(match => match.Value is Expr.Call)
                || !items.SkipLast(1).OfType<Expr.Call>().Any(call => call.Arguments.Any(argument => argument is Expr.Call))
                    && items is not
                    [
                        Expr.Match { Value: Expr.Call { Module: not null, DynamicRemote: false } },
                        Expr.Match { Value: Expr.Call { Module: null, DynamicRemote: false } },
                        Expr.Call { Module: not null, DynamicRemote: false },
                        Expr.Call { Module: not null, DynamicRemote: false },
                        Expr.Variable
                    ]))
            {
                return null;
            }

            var tuple = items[^1] switch
            {
                Expr.RecordConstruction record => RecordExpressions.Create(record),
                Expr.Variable retained => new Expr.Tuple([retained]),
                Expr.Tuple existing => existing,
                _ => throw new InvalidOperationException()
            };
            if (AttributeLiteral.TryNormalizeConstant(tuple, out _))
            {
                return null;
            }

            int statementCount = items.Count - 1;
            Expr.Fun? closure = null;
            string? closureBinding = null;
            if (statementCount > 0 && items[statementCount - 1] is Expr.Match
                {
                    Pattern: Pattern.Variable { Name: not VariableScopeNames.Wildcard } binding,
                    Value: Expr.Fun function
                })
            {
                closure = function;
                closureBinding = binding.Name;
                statementCount--;
            }

            var prefix = items.Take(statementCount).SelectMany(ExpandTupleCallBinding);
            var flattened = FlattenCallArguments(prefix, tuple, prepareClosures: true);
            tuple = (Expr.Tuple)flattened[^1];
            var statements = flattened.Take(flattened.Count - 1).ToArray();
            Dictionary<int, CallResultCheck>? checks = null;
            Dictionary<int, CallClosure>? argumentClosures = null;
            var calls = statements.Any(item => ReadTupleCallCheck(item) is not null)
                ? ReadCallsWithChecks(statements, parameters, out checks)
                : ReadCallsWithClosures(statements, parameters, out argumentClosures);
            if (calls is not { Length: > 0 }
                || retainedValue && (checks is not null || closure is not null
                    || argumentClosures is { Count: > 0 } && !TryPrepareRetainedScalarClosure(
                        calls, argumentClosures, ((Expr.Variable)items[^1]).Name
                    )))
            {
                return null;
            }

            var bindings = parameters.Concat(calls.Select(call => call.Result).OfType<string>())
                .Concat(ReadAdditionalCallResultBindings(checks))
                .Where(name => name != VariableScopeNames.Wildcard)
                .ToDictionary(name => name, _ => (Term)Nil.Value, StringComparer.Ordinal);
            if (closureBinding is not null && bindings.ContainsKey(closureBinding))
            {
                return null;
            }

            var reads = new HashSet<string>(StringComparer.Ordinal);
            bool usesClosure = false;
            foreach (Expr item in tuple.Items)
            {
                if (item is Expr.Variable variable && variable.Name == closureBinding)
                {
                    usesClosure = true;
                }
                else if (item is Expr.Variable input && bindings.ContainsKey(input.Name))
                {
                    reads.Add(input.Name);
                }
                else if (item is Expr.Fun inline && closure is null)
                {
                    closure = inline;
                    usesClosure = true;
                }
                else if (item is Expr.Tuple or Expr.List or Expr.RecordConstruction)
                {
                    reads.UnionWith(LocalFunctionEntries.Walk(item).OfType<Expr.Variable>()
                        .Select(value => value.Name).Where(bindings.ContainsKey));
                }
                else if (!AttributeLiteral.TryNormalizeConstant(item, out _))
                {
                    return null;
                }
            }

            if (closure is not null && (!usesClosure || parentFunction is null || closure.SelfName is not null))
            {
                return null;
            }

            string[] captures = closure is null ? [] : bindings.Keys.Intersect(ClosureCaptures.Capture(closure, bindings).Keys).ToArray();
            if (closure is not null && closure.Clauses[0].Patterns.Count + captures.Length > BeamWire.MaximumArity)
            {
                return null;
            }

            reads.UnionWith(captures);
            var source = new CallSequenceTupleSource(
                tuple,
                closure,
                closureBinding,
                captures,
                reads,
                retainedValue
            );

            return ReadSavedCallSequence(
                calls,
                parameters,
                extraction: null,
                tuple: source,
                closures: argumentClosures,
                checks: checks
            );
        }

        private static bool TryPrepareRetainedScalarClosure(
            (Expr.Call Call, string? Result)[] calls,
            Dictionary<int, CallClosure> closures,
            string retained
        )
        {
            if (closures.Count != 1)
            {
                return false;
            }

            var (index, closure) = closures.Single();
            if (index <= 0 || index + 1 >= calls.Length || calls[index - 1].Result is not { } captured
                || captured == retained || closure.Captures.Length != 2
                || !closure.Captures.Contains(captured) || !closure.Captures.Contains(retained)
                || calls[index].Result is not { } operand
                || calls[index + 1].Call.Arguments is not [Expr.Variable first, Expr.Variable second])
            {
                return false;
            }

            if (first.Name != closure.Binding || second.Name != operand)
            {
                return false;
            }

            // Core-to-SSA appends its ordered set of free variables to the
            // lifted function's declared arguments.
            string[] captures = TermOrder.Compare(
                new Atom(closure.Captures[0]),
                new Atom(closure.Captures[1]),
                exact: true
            ) <= 0 ? closure.Captures : [closure.Captures[1], closure.Captures[0]];
            closures[index] = closure with { Captures = captures };

            return true;
        }

        private static IEnumerable<Expr> ExpandTupleCallBinding(Expr expression)
        {
            if (expression is not Expr.Match { Pattern: Pattern.Tuple pattern, Value: Expr.Tuple tuple }
                || pattern.Items.Count != tuple.Items.Count
                || pattern.Items.Any(item => item is not Erlang.Pattern.Variable)
                || !DistinctBindings(pattern.Items.Cast<Pattern.Variable>().ToArray())
                || tuple.Items.Any(item => item is not Expr.Call { DynamicRemote: false }
                    || !HasSimpleDecodeArguments((Expr.Call)item)))
            {
                yield return expression;
                yield break;
            }

            var fields = pattern.Items.Cast<Pattern.Variable>().ToArray();
            var names = fields.Select(field => field.Name).ToHashSet(StringComparer.Ordinal);
            if (tuple.Items.Cast<Expr.Call>().SelectMany(call => call.Arguments).OfType<Expr.Variable>()
                .Any(argument => names.Contains(argument.Name)))
            {
                yield return expression;
                yield break;
            }

            for (int index = 0; index < fields.Length; index++)
            {
                yield return new Expr.Match(fields[index], tuple.Items[index]);
            }
        }

        private static CallSequenceTuple? ReadCallSequenceTuple(
            CallSequenceTupleSource source,
            string? result,
            Dictionary<string, int> saved,
            (int Slot, int Field, string Name)[]? projections = null
        )
        {
            int live = result is not null && source.Reads.Contains(result) ? 1 : 0;
            if (source.RetainedValue)
            {
                string returned = ((Expr.Variable)source.Tuple.Items[0]).Name;
                CaseTupleOperand? scalar = returned == result ? new(XRegisterTag, 0, null)
                    : saved.TryGetValue(returned, out int slot) ? new(YRegisterTag, slot, null) : null;

                return scalar is null ? null : new(
                    [],
                    null,
                    [],
                    [],
                    null,
                    live,
                    Scalar: scalar
                );
            }

            if (source.Closure is null)
            {
                var inputs = new Dictionary<string, int>(StringComparer.Ordinal);
                if (live != 0)
                {
                    inputs.Add(result!, 0);
                }

                var reads = new List<(int Slot, int Field, int Register)>();
                foreach (var (slot, field, name) in projections ?? [])
                {
                    int register = inputs.Count;
                    if (register >= FirstReservedXRegister)
                    {
                        return null;
                    }

                    inputs.Add(name, register);
                    reads.Add((slot, field, register));
                }

                var construction = ReadCallArgumentConstructions(
                    [source.Tuple],
                    -1,
                    saved,
                    inputs: inputs
                );

                return construction is null ? null : new CallSequenceTuple(
                    [],
                    null,
                    [],
                    [],
                    null,
                    live,
                    construction,
                    Projections: reads.Count == 0 ? null : reads.ToArray()
                );
            }

            var operands = new CaseTupleOperand[source.Tuple.Items.Count];
            for (int index = 0; index < operands.Length; index++)
            {
                Expr item = source.Tuple.Items[index];
                if (item is Expr.Fun || item is Expr.Variable binding && binding.Name == source.ClosureBinding)
                {
                    operands[index] = new CaseTupleOperand(XRegisterTag, live, null);
                }
                else if (item is Expr.Variable variable)
                {
                    if (variable.Name == result)
                    {
                        operands[index] = new CaseTupleOperand(XRegisterTag, 0, null);
                    }
                    else if (saved.TryGetValue(variable.Name, out int slot))
                    {
                        operands[index] = new CaseTupleOperand(YRegisterTag, slot, null);
                    }
                    else
                    {
                        return null;
                    }
                }
                else if (AttributeLiteral.TryNormalizeConstant(item, out Term constant))
                {
                    operands[index] = new CaseTupleOperand(0, 0, constant);
                }
                else
                {
                    return null;
                }
            }

            var captures = source.Captures.Where(name => name != result)
                .ToDictionary(name => name, name => saved[name], StringComparer.Ordinal);

            return new CallSequenceTuple(
                operands,
                source.Closure,
                captures,
                source.Captures.Select((name, index) => (name, index))
                    .ToDictionary(capture => capture.name, capture => capture.index, StringComparer.Ordinal),
                result is not null && source.Captures.Contains(result) ? result : null,
                live
            );
        }

        private void EmitCallSequenceTuple(CallSequenceTuple tuple, int frame, int? projectedField)
        {
            if (tuple.Scalar is { } scalar)
            {
                if (scalar.Tag == YRegisterTag)
                {
                    owner.MoveRegister(
                        YRegisterTag,
                        scalar.Register,
                        XRegisterTag,
                        0
                    );
                }

                owner.EmitFrameReturn(frame);

                return;
            }

            if (tuple.Construction is { } construction)
            {
                bool heapChecked = projectedField.HasValue || tuple.Projections is not null;
                if (heapChecked)
                {
                    owner.Op(TestHeap, BeamWire.UnsignedTag, construction.Constructions.Sum(item => item.HeapWords));
                    owner.Operand(BeamWire.UnsignedTag, tuple.Projections is null ? construction.Live : tuple.Live);
                    if (projectedField is int field)
                    {
                        owner.EmitTuplePart(0, field, 0);
                    }

                    // The aggregate is a Y root at the heap check. Its X fields
                    // become live only for the following construction.
                    foreach (var (slot, element, register) in tuple.Projections ?? [])
                    {
                        owner.Op(GetTupleElement, YRegisterTag, slot);
                        owner.Operand(BeamWire.UnsignedTag, element);
                        owner.Operand(XRegisterTag, register);
                    }
                }

                EmitCallArgumentConstructions(construction, -1, heapChecked: heapChecked);
                owner.EmitFrameReturn(frame);

                return;
            }

            int words = tuple.Operands.Length + 1;
            if (tuple.Closure is { } closure)
            {
                int captures = tuple.SavedCaptures.Count + (tuple.ResultCapture is null ? 0 : 1);
                owner.EmitClosureAllocation(words + captures, tuple.Live, 1);
                var previousVariables = variables;
                var previousOrder = entryCaptureOrder;
                variables = tuple.SavedCaptures;
                entryCaptureOrder = tuple.CaptureOrder;
                EmitGeneralClosure(
                    closure,
                    heapChecked: true,
                    destination: tuple.Live,
                    x0Capture: tuple.ResultCapture
                );
                variables = previousVariables;
                entryCaptureOrder = previousOrder;
            }
            else
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, words);
                owner.Operand(BeamWire.UnsignedTag, tuple.Live);
            }

            owner.EmitTupleHeader(XRegisterTag, tuple.Operands.Length);
            foreach (CaseTupleOperand operand in tuple.Operands)
            {
                EmitCaseOperand(operand);
            }

            owner.EmitFrameReturn(frame);
        }
    }
}
