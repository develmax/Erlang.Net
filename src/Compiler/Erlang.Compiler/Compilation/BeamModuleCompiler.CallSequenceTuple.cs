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
            HashSet<string> Reads
        );

        private sealed record CallSequenceTuple(
            CaseTupleOperand[] Operands,
            Expr.Fun? Closure,
            Dictionary<string, int> SavedCaptures,
            Dictionary<string, int> CaptureOrder,
            string? ResultCapture,
            int Live,
            CallArgumentConstructions? Construction = null
        );

        private bool TryEmitSavedCallSequenceTuple(Clause clause)
        {
            if (clause.Guard is not null || !HasUnconditionalParameters(clause)
                || clause.Body is not (Expr.Tuple or Expr.Sequence { Items: [.., Expr.Tuple] }))
            {
                return false;
            }

            string[] parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            IReadOnlyList<Expr> items = clause.Body is Expr.Sequence sequence ? sequence.Items : [clause.Body];
            var tuple = (Expr.Tuple)items[^1];
            if (AttributeLiteral.TryNormalizeConstant(tuple, out _))
            {
                return false;
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

            var flattened = FlattenCallArguments(items.Take(statementCount), tuple, prepareClosures: true);
            tuple = (Expr.Tuple)flattened[^1];
            var statements = flattened.Take(flattened.Count - 1).ToArray();
            Dictionary<int, CallResultCheck>? checks = null;
            Dictionary<int, CallClosure>? argumentClosures = null;
            var calls = statements.Any(item => ReadTupleCallCheck(item) is not null)
                ? ReadCallsWithChecks(statements, parameters, out checks)
                : ReadCallsWithClosures(statements, parameters, out argumentClosures);
            if (calls is not { Length: > 0 })
            {
                return false;
            }

            var bindings = parameters.Concat(calls.Select(call => call.Result).OfType<string>())
                .Concat(ReadAdditionalCallResultBindings(checks))
                .Where(name => name != VariableScopeNames.Wildcard)
                .ToDictionary(name => name, _ => (Term)Nil.Value, StringComparer.Ordinal);
            if (closureBinding is not null && bindings.ContainsKey(closureBinding))
            {
                return false;
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
                else if (item is Expr.Tuple or Expr.List)
                {
                    reads.UnionWith(LocalFunctionEntries.Walk(item).OfType<Expr.Variable>()
                        .Select(value => value.Name).Where(bindings.ContainsKey));
                }
                else if (!AttributeLiteral.TryNormalizeConstant(item, out _))
                {
                    return false;
                }
            }

            if (closure is not null && (!usesClosure || parentFunction is null || closure.SelfName is not null))
            {
                return false;
            }

            string[] captures = closure is null ? [] : bindings.Keys.Intersect(ClosureCaptures.Capture(closure, bindings).Keys).ToArray();
            if (closure is not null && closure.Clauses[0].Patterns.Count + captures.Length > BeamWire.MaximumArity)
            {
                return false;
            }

            reads.UnionWith(captures);
            var source = new CallSequenceTupleSource(
                tuple,
                closure,
                closureBinding,
                captures,
                reads
            );
            if (ReadSavedCallSequence(
                calls,
                parameters,
                extraction: null,
                tuple: source,
                closures: argumentClosures,
                checks: checks
            ) is not { } plan)
            {
                return false;
            }

            EmitSavedCallSequence(plan);

            return true;
        }

        private static CallSequenceTuple? ReadCallSequenceTuple(
            CallSequenceTupleSource source,
            string? result,
            Dictionary<string, int> saved
        )
        {
            int live = result is not null && source.Reads.Contains(result) ? 1 : 0;
            if (source.Closure is null)
            {
                var inputs = new Dictionary<string, int>(StringComparer.Ordinal);
                if (live != 0)
                {
                    inputs.Add(result!, 0);
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
                    construction
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
            if (tuple.Construction is { } construction)
            {
                if (projectedField is int field)
                {
                    owner.Op(TestHeap, BeamWire.UnsignedTag, construction.Constructions[0].HeapWords);
                    owner.Operand(BeamWire.UnsignedTag, construction.Live);
                    owner.EmitTuplePart(0, field, 0);
                }

                EmitCallArgumentConstructions(construction, -1, heapChecked: projectedField is not null);
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
